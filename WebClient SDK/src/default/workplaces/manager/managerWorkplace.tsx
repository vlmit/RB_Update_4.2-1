import { observer } from 'mobx-react-lite';
import { computed, reaction, action, observable, runInAction } from 'mobx';
import styled from 'styled-components';
import { ManagerWorkplaceSettings } from './managerWorkplaceSettings';
import { ManagerWorkplaceTileViewModel, ManagerWorkplaceTile } from './managerWorkplaceTile';
import { BaseContentItem, ContentPlaceArea, ContentPlaceOrder } from 'tessa/ui/views/content';
import { IWorkplaceViewComponent } from 'tessa/ui/views';
import { Guid } from 'tessa/platform';

export class ManagerWorkplaceViewModel extends BaseContentItem {
  //#region ctor

  constructor(
    settings: ManagerWorkplaceSettings,
    viewComponent: IWorkplaceViewComponent,
    area: ContentPlaceArea = ContentPlaceArea.ContentPanel,
    order: number = ContentPlaceOrder.BeforeAll
  ) {
    super(viewComponent, area, order);
    this._settings = settings;
    this.models = observable.array([], { deep: false });
  }

  //#endregion

  //#region fields

  private _settings: ManagerWorkplaceSettings;

  private _loadingGuard: string | null = null;

  //#endregion

  //#region props

  public readonly models: ManagerWorkplaceTileViewModel[];

  @computed
  public get isLoading(): boolean {
    return this.viewComponent.isDataLoading;
  }

  //#endregion

  //#region methods

  public initialize(): void {
    super.initialize();
    this.disposeList.add(
      reaction(
        () => this.viewComponent.data,
        () => this.refresh()
      )
    );
  }

  public dispose(): void {
    super.dispose();
  }

  @action.bound
  public async refresh(): Promise<void> {
    this.models.length = 0;
    const data = this.viewComponent.data;
    if (!data) {
      return;
    }

    const tiles: ManagerWorkplaceTileViewModel[] = [];
    const guard = (this._loadingGuard = Guid.newGuid());
    for (const row of data) {
      tiles.push(
        await ManagerWorkplaceTileViewModel.create(this.viewComponent, row, this._settings)
      );
    }

    runInAction(() => {
      if (this._loadingGuard === guard) {
        this.models.push(...tiles);
        this._loadingGuard = null;
      }
    });
  }

  //#endregion
}

const StyledContainer = styled.div`
  display: flex;
  flex-wrap: wrap;
`;

export interface ManagerWorkplaceProps {
  viewModel: ManagerWorkplaceViewModel;
}

export const ManagerWorkplace = observer<ManagerWorkplaceProps>(function ManagerWorkplace(props) {
  const { viewModel } = props;
  return (
    <StyledContainer>
      {viewModel.models.map((x, i) => {
        return <ManagerWorkplaceTile key={i} viewModel={x} />;
      })}
    </StyledContainer>
  );
});
