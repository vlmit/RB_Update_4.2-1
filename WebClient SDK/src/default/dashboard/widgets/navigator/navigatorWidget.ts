import { Guid, StringHelper } from '@tessa/core';
import {
  IFileContentSaver,
  IWorkplaceDataComponentMetadata,
  WorkplaceMetadata
} from '@tessa/platform';
import { AppPath, HttpHelper } from '@tessa/application';
import { Application } from 'tessa/application';
import { redirectOnFileLink } from 'tessa/applicationHelper';
import { showLoadingOverlay } from 'tessa/ui/loadingOverlay';
import { DashboardViewModel } from 'tessa/ui/dashboard/dashboardViewModel';
import { openViewWorkplace } from 'tessa/ui/uiHost/openViewWorkplace';
import {
  findWorkplaceMetadataByCompositionId,
  findComponentMetadataByCompositionId
} from 'tessa/ui/views/workplaces/tree';
import { IWorkplaceViewModel } from 'tessa/ui/views/workplaceViewModel';
import { isSearchQuery, isView } from 'tessa/views/workplaces/workplaceComponentMetadataHelper';
import { PageLifecycleSingleton } from 'common/pageLifecycle/pageLifecycleSingleton';
import { ButtonWidget } from '../button/buttonWidget';
import { DefaultWidgetNames } from '../widgetNames';
import { NavigatorWidgetSettings } from './navigatorWidgetSettings';

/** Виджет "Навигатор". */
export class NavigatorWidget extends ButtonWidget<NavigatorWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link NavigatorWidget}.
   * @param _fileContentSaver Сервис для загрузки контента файла.
   * @param dashboard Модель представления дашборда.
   * @param id Уникальный идентификатор виджета. Если не задан, то будет сгенерирован новый идентификатор.
   * @param settings Настройки виджета. Если не заданы, то будут созданы новые настройки со значением по умолчанию.
   */
  constructor(
    private readonly _fileContentSaver: IFileContentSaver,
    dashboard: DashboardViewModel,
    id?: string,
    settings = new NavigatorWidgetSettings()
  ) {
    super(DefaultWidgetNames.Navigator, settings, dashboard, id);
  }

  //#endregion

  //#region base overrides

  protected override async handleClickCore(): Promise<void> {
    await showLoadingOverlay(async _ => {
      const link = this.settings.link;
      if (!link) {
        return;
      } else if (!HttpHelper.isAbsoluteURL(link)) {
        await this.redirectLink(link);
      } else if (StringHelper.starts(link, AppPath.fullPath())) {
        await this.redirectLink(link.replace(AppPath.fullPath(), ''));
      } else {
        await this.openLink(link);
      }
    });
  }

  //#endregion

  //#region private methods

  private static trimStartSlashes(value: string): string {
    return value.trim().replace(/^\/+/, '');
  }

  private openLink(link: string): Promise<WindowProxy | null> {
    return Promise.resolve(window.open(link));
  }

  private redirectLink(link: string): Promise<IWorkplaceViewModel | boolean | null | void> {
    const relativeLink = NavigatorWidget.trimStartSlashes(link);

    if (StringHelper.starts(relativeLink, 'links/')) {
      return redirectOnFileLink(
        relativeLink,
        Application.instance.history,
        () => (PageLifecycleSingleton.instance.showConfirmBeforeUnload = false)
      );
    }

    if (StringHelper.starts(relativeLink, 'content/')) {
      return this._fileContentSaver.save(AppPath.fullPath(relativeLink));
    }

    if (StringHelper.starts(relativeLink, 'view/') && this.settings.openViewInSeparateTab) {
      const id = relativeLink.split('/')[1];
      if (Guid.isValid(id)) {
        const { workplace } = findWorkplaceMetadataByCompositionId(id);
        let component = workplace && findComponentMetadataByCompositionId(id, workplace);
        while (component && !isView(component) && !isSearchQuery(component)) {
          const parentCompositionId = component.parentCompositionId;
          component = findComponentMetadataByCompositionId(parentCompositionId, workplace!);
        }

        if (component) {
          const dataComponentMetadata = component as IWorkplaceDataComponentMetadata;
          const workplaceMetadata = new WorkplaceMetadata();
          workplaceMetadata.compositionId = Guid.newGuid();
          workplaceMetadata.alias = dataComponentMetadata.caption || dataComponentMetadata.alias;
          workplaceMetadata.items.push(dataComponentMetadata);
          return openViewWorkplace({
            workplaceMetadata,
            compositionId: id,
            parameters: [],
            activate: true,
            isCloseable: true,
            enabledEndUserModification: false
          });
        }
      }
    }

    return Promise.resolve(Application.instance.history.push(link));
  }

  //#endregion
}
