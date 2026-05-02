import {
  TagInfo,
  TagClickMode,
  IViewMetadata,
  IViewRepository,
  IViewRepository$,
  ViewRequest,
  ViewRequestParameter,
  ViewRequestParameterBuilder,
  ViewCriteriaOperators
} from '@tessa/platform';
import { inject, injectable, localize } from '@tessa/application';
import { Guid, ValidationResult, ValidationResultType } from '@tessa/core';
import { showView } from 'tessa/ui/uiHost/showView';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { ITagData, ITagDataManager } from './tagTypes';
import { NamedReference } from '../../common/namedReference';

/** Менеджер для получения и отображения данных о теге. */
@injectable()
export class TagDataManager implements ITagDataManager {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TagDataManager}.
   * @param _viewRepository Предоставляет доступ к представлениям доступным в системе.
   */
  constructor(@inject(IViewRepository$) private readonly _viewRepository: IViewRepository) {}

  //#endregion

  //#region ITagDataManager

  async getTagData(tag: NamedReference, types?: NamedReference[]): Promise<ITagData | null> {
    if (!Guid.isValid(tag.id)) {
      return null;
    }

    const tagInfo = await this.getTagInfo(tag);
    if (!tagInfo) {
      return null;
    }

    const recordsCount = await this.getRecordsCount(tag, types);

    return { tagInfo, recordsCount };
  }

  async showTagData(tag: NamedReference, types?: NamedReference[]): Promise<void> {
    if (!Guid.isValid(tag.id)) {
      return;
    }

    const view = await this._viewRepository.getByName('TagCards');
    if (!view) {
      const message = '$Tags_OpenTagCards_NoAccessToView_Error';
      await showNotEmpty(ValidationResult.fromText(message, ValidationResultType.Error));
      return;
    }

    const metadata = await view.getMetadata();

    const parameters = [this.createParameter('Tag', tag, metadata)];
    if (types?.length) {
      parameters.push(this.createParameter('Type', types, metadata));
    }

    await showView({
      viewAlias: 'TagCards',
      displayValue: localize('$Tags_OpenTagCards_Workspace_Template', tag.name),
      parameters
    });
  }

  //#endregion

  //#region private methods

  private async getTagInfo(tag: NamedReference): Promise<TagInfo | null> {
    const view = await this._viewRepository.getByName('Tags');
    if (!view) {
      return null;
    }

    const metadata = await view.getMetadata();
    const request = new ViewRequest(metadata);
    request.addParameter(builder => this.createParameter('ID', tag, metadata, builder));

    const result = await view.getData(request);
    const row = result.getRowsAsMap()?.[0];
    if (!row) {
      return null;
    }

    const tagInfo = new TagInfo();
    tagInfo.id = row.getString('TagID')!;
    tagInfo.name = row.getString('TagName')!;
    tagInfo.background = row.getNumber('TagBackground')!;
    tagInfo.isCommon = row.getBoolean('TagIsCommon')!;
    tagInfo.clickMode = TagClickMode.No;
    tagInfo.icon = '';

    return tagInfo;
  }

  private async getRecordsCount(tag: NamedReference, types?: NamedReference[]): Promise<number> {
    const view = await this._viewRepository.getByName('TagCards');
    if (!view) {
      return 0;
    }

    const metadata = await view.getMetadata();
    const request = new ViewRequest(metadata);
    request.subsetName = 'Count';

    request.addParameter(builder => this.createParameter('Tag', tag, metadata, builder));
    if (types?.length) {
      request.addParameter(builder => this.createParameter('Type', types, metadata, builder));
    }

    const result = await view.getData(request);
    return (result.rows[0]?.[0] as number) ?? 0;
  }

  private createParameter(
    alias: string,
    value: NamedReference | NamedReference[],
    metadata: IViewMetadata,
    builder: ViewRequestParameterBuilder = new ViewRequestParameterBuilder()
  ): ViewRequestParameter {
    builder.withMetadata(metadata.parameters.get(alias)!);

    if (Array.isArray(value)) {
      for (const { id, name } of value) {
        builder.addCriteria(ViewCriteriaOperators.EqualsTo, name || id, id);
      }
    } else {
      builder.addCriteria(ViewCriteriaOperators.EqualsTo, value.name || value.id, value.id);
    }

    return builder.asRequestParameter();
  }

  //#endregion
}
