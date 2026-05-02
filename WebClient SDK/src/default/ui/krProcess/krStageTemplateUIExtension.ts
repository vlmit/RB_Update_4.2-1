import { observe, Lambda } from 'mobx';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { GridViewModel, LabelViewModel, AutoCompleteTableViewModel } from 'tessa/ui/cards/controls';
import { Card, CardRow, CardRowState } from 'tessa/cards';
import { Visibility, Guid } from 'tessa/platform';
import { extension } from '@tessa/application';
import { FieldType } from '@tessa/core';
import { sqlApproverRoleId, sqlApproverRoleName } from '../../workflow/krProcess/krUIHelper';

/**
 * UI расширение, обрабатывающее логику работы со ссылкой "Вычисляемые исполнители" в параметрах этапов.
 */
@extension({ name: 'KrStageTemplateUIExtension' })
export class KrStageTemplateUIExtension extends CardUIExtension {
  //#region const

  private static readonly krStageTemplateTypeId = '2fa85bb3-bba4-4ab6-ba97-652106db96de';

  private static readonly krSecondaryProcessTypeId = '61420fa1-cc1f-47cb-b0bb-4ea8ee77f51a';

  private static readonly krApprovalStageTypeSettingsTypeId =
    '4a377758-2366-47e9-98ac-c5f553974236';

  //#endregion

  //#region base overrides

  public initialized(context: ICardUIExtensionContext): void {
    const cardModel = context.model;

    if (
      !Guid.equals(cardModel.card.typeId, KrStageTemplateUIExtension.krStageTemplateTypeId) &&
      !Guid.equals(cardModel.card.typeId, KrStageTemplateUIExtension.krSecondaryProcessTypeId)
    ) {
      return;
    }

    const grid = cardModel.controls.get('ApprovalStagesTable') as GridViewModel;
    if (!grid) {
      return;
    }

    let approversControlDisposer: Lambda | null = null;
    grid.rowInitializing.add(e => {
      const hyperlink = e.rowModel!.controls.get('AddComputedRoleLink') as LabelViewModel;
      if (!hyperlink) {
        return;
      }

      hyperlink.controlVisibility = KrStageTemplateUIExtension.hasComputedRole(
        e.cardModel.card,
        e.row.rowId
      )
        ? Visibility.Collapsed
        : Visibility.Visible;

      const approversControl = e.rowModel!.controls.get(
        'MultiplePerformersTableAC'
      ) as AutoCompleteTableViewModel;
      if (approversControl) {
        const approversControlChanged = () => {
          hyperlink.controlVisibility = KrStageTemplateUIExtension.hasComputedRole(
            e.cardModel.card,
            e.row.rowId
          )
            ? Visibility.Collapsed
            : Visibility.Visible;
        };

        approversControlDisposer = observe(approversControl, 'items', approversControlChanged);
      }

      const card = e.cardModel.card;
      const rows = card.sections.get('KrPerformersVirtual_Synthetic').rows;
      const rowId = e.row.rowId;

      hyperlink.onClick = () => {
        if (!KrStageTemplateUIExtension.hasComputedRole(card, rowId)) {
          const row = new CardRow();
          row.rowId = Guid.newGuid();
          row.state = CardRowState.Inserted;

          row.set('PerformerID', sqlApproverRoleId, FieldType.Guid);
          row.set('PerformerName', sqlApproverRoleName, FieldType.String);
          row.set('StageRowID', rowId, FieldType.Guid);
          const order =
            rows.length !== 0 ? Math.max(...rows.map(x => x.get<number>('Order')!), -1) + 1 : 0;
          row.set('Order', order, FieldType.Int);
          rows.push(row);
        }
      };
    });

    grid.rowEditorClosed.add(_ => {
      if (approversControlDisposer) {
        approversControlDisposer();
      }
    });

    grid.rowValidating.add(e => {
      const multiplePerformerControl = e.rowModel!.controls.get('MultiplePerformersTableAC');
      if (
        !multiplePerformerControl ||
        multiplePerformerControl.controlVisibility !== Visibility.Visible
      ) {
        return;
      }
    });
  }

  //#endregion

  //#region private methods

  private static hasComputedRole(card: Card, stageRowId: string): boolean {
    const sectionAlias = Guid.equals(
      card.typeId,
      KrStageTemplateUIExtension.krApprovalStageTypeSettingsTypeId
    )
      ? 'KrPerformersVirtual'
      : 'KrPerformersVirtual_Synthetic';

    const section = card.sections.tryGet(sectionAlias);
    if (!section) {
      return false;
    }

    return section.rows.some(row =>
      KrStageTemplateUIExtension.hasComputedRoleInRow(row, stageRowId)
    );
  }

  private static hasComputedRoleInRow(row: CardRow, stageRowId: string): boolean {
    const srid = row.get<string>('StageRowID');
    if (!Guid.equals(srid, stageRowId)) {
      return false;
    }

    const approverId = row.get<string>('PerformerID');
    if (!Guid.equals(approverId, sqlApproverRoleId)) {
      return false;
    }

    return row.state !== CardRowState.Deleted;
  }

  //#endregion
}
