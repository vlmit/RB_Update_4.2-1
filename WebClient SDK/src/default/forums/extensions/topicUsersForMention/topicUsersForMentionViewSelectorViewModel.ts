import { observable, runInAction } from 'mobx';
import { ViewCriteriaOperators, ViewRequestParameterBuilder } from '@tessa/platform';
import { ClassNameList } from '@tessa/ui';
import { MenuAction } from 'tessa/ui';
import { IWorkplaceViewComponent } from 'tessa/ui/views';
import { BaseContentItem, ContentPlaceArea, ContentPlaceOrder } from 'tessa/ui/views/content';

export type TopicUsersForMentionViewType = 'all-users' | 'only-participants';

export class TopicUsersForMentionViewSelectorViewModel extends BaseContentItem {
  //#region private static fields

  private static readonly showAllUsersParameterAlias = 'ShowAllUsers';

  //#endregion

  //#region ctor

  constructor(
    viewComponent: IWorkplaceViewComponent,
    area = ContentPlaceArea.ContextPanel,
    order = ContentPlaceOrder.AfterAll
  ) {
    super(viewComponent, area, order);
  }

  //#endregion

  //#region fields

  @observable.ref
  private _viewType: TopicUsersForMentionViewType = 'only-participants';

  //#endregion

  //#region props

  get viewType(): TopicUsersForMentionViewType {
    return this._viewType;
  }
  set viewType(value: TopicUsersForMentionViewType) {
    runInAction(() => (this._viewType = value));
  }

  //#endregion

  //#region public methods

  public readonly className = new ClassNameList();

  public getMenuActions(): MenuAction[] {
    return [
      MenuAction.create({
        name: 'only-participants',
        caption: '$Forum_TopicUsersForMention_ParticipantsFilterCaption',
        action: async () => await this.applySelectionFilter('only-participants')
      }),
      MenuAction.create({
        name: 'all-users',
        caption: '$Forum_TopicUsersForMention_AllUsersFilterCaption',
        action: async () => await this.applySelectionFilter('all-users')
      })
    ];
  }

  //#endregion

  //#region private methods

  private async applySelectionFilter(type: TopicUsersForMentionViewType): Promise<void> {
    if (this.viewType === type) {
      return;
    }

    const parameterMetadata = this.viewComponent.viewMetadata?.parameters.tryGet(
      TopicUsersForMentionViewSelectorViewModel.showAllUsersParameterAlias
    );
    if (!parameterMetadata) {
      return;
    }

    const existedParameter = this.viewComponent.parameters.parameters.find(
      p => p.metadata.alias === TopicUsersForMentionViewSelectorViewModel.showAllUsersParameterAlias
    );

    if (type === 'all-users' && !existedParameter) {
      this.viewComponent.parameters.addParameters(
        new ViewRequestParameterBuilder()
          .withMetadata(parameterMetadata)
          .addCriteria(ViewCriteriaOperators.IsTrue)
          .asRequestParameter()
      );
    } else if (type === 'only-participants' && existedParameter) {
      this.viewComponent.parameters.removeParameters(existedParameter);
    }

    this.viewType = type;
  }

  //#endregion
}
