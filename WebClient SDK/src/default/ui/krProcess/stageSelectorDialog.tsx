import * as React from 'react';
import { observer } from 'mobx-react';
import { StageGroup } from './stageGroup';
import { StageType } from './stageType';
import { StageSelectorViewModel } from './stageSelectorViewModel';
import { localize } from 'tessa/localization';
import { Dialog, DialogContainer, DialogContent, DialogFooter } from 'ui';
import { Grid } from 'components/cardElements';
import { StageDialogRowAdapter } from './stageDialogRowAdapter';
import {
  defaultLayout,
  GridColumnDisplayType,
  IGridColumnViewModel,
  IGridLayout
} from 'components/cardElements/grid';
import { Visibility } from 'tessa/platform';
import { computed } from 'mobx';
import { Button } from 'ui/button/button';
import { IDialogChromeSettings } from 'ui/dialog/definitions';

export const stageSelectorColumnName = 'name';

export interface StageSelectorDialogProps {
  viewModel: StageSelectorViewModel;
  onClose: (args: { cancel: boolean; group: StageGroup | null; type: StageType | null }) => void;
}

@observer
export class StageSelectorDialog extends React.Component<StageSelectorDialogProps> {
  //#region ctor

  constructor(props: StageSelectorDialogProps) {
    super(props);
    this._layouts = [{ name: defaultLayout.name, start: 0 }];

    this._groupColumns = [
      {
        id: stageSelectorColumnName,
        caption: localize('$Views_KrStageTemplates_StageGroup'),
        visibility: Visibility.Visible,
        displayType: GridColumnDisplayType.normal
      }
    ];

    this._typeColumns = [
      {
        id: stageSelectorColumnName,
        caption: localize('$Views_KrStageTemplates_Types'),
        visibility: Visibility.Visible,
        displayType: GridColumnDisplayType.normal
      }
    ];
  }

  //#endregion

  //#region fields

  private _layouts: IGridLayout[];
  private _groupColumns: IGridColumnViewModel[];
  private _typeColumns: IGridColumnViewModel[];

  //#endregion

  //#region props

  @computed
  private get groupRows() {
    const { viewModel } = this.props;
    return viewModel.groups.map(g => new StageDialogRowAdapter(g, viewModel, g.id));
  }

  @computed
  private get typeRows() {
    const { viewModel } = this.props;
    return viewModel.types.map(
      g => new StageDialogRowAdapter(g, viewModel, g.id, this.handleCloseFormWithResult)
    );
  }

  //#endregion

  //#region react

  public render(): React.ReactElement {
    const chromeSettings: IDialogChromeSettings = {
      title: localize('$UI_Cards_SelectGroupAndType'),
      titlePosition: 'left'
    };

    return (
      <Dialog
        isOpened={true}
        noPortal={true}
        autoSizeWidth={true}
        autoSizeHeight={true}
        onCloseRequest={this.handleCloseForm}
        className="kr-stages-modal table-modal-form"
        type="controls"
        showChrome={true}
        chromeSettings={chromeSettings}
      >
        <DialogContainer>
          <DialogContent>
            <Grid
              columns={this._groupColumns}
              rows={this.groupRows}
              canSelectMultipleItems={false}
              layouts={this._layouts}
            />
            <Grid
              columns={this._typeColumns}
              rows={this.typeRows}
              canSelectMultipleItems={false}
              noDataText={localize('$UI_Error_NoAvailableStages')}
              layouts={this._layouts}
            />
          </DialogContent>
          <DialogFooter className="default-footer">
            <Button
              caption="$UI_Common_OK"
              onClick={this.handleCloseFormWithResult}
              type="normal"
              theme="primary"
            />
            <Button
              caption="$UI_Common_Cancel"
              onClick={this.handleCloseForm}
              type="normal"
              theme="secondary"
            />
          </DialogFooter>
        </DialogContainer>
      </Dialog>
    );
  }

  //#endregion

  //#region handlers

  private handleCloseForm = () => {
    this.props.onClose({
      cancel: true,
      group: null,
      type: null
    });
  };

  private handleCloseFormWithResult = () => {
    const { viewModel } = this.props;

    this.props.onClose({
      cancel: false,
      group: viewModel.selectedGroup,
      type: viewModel.selectedType
    });
  };

  //#endregion
}
