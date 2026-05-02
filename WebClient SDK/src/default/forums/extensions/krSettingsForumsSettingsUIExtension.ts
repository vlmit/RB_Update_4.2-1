import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { GridRowAction, GridViewModel } from 'tessa/ui/cards/controls';
import { ForumHelper } from 'tessa/forums';
import { WfUiHelper } from '../../workflow/wf/wfUiHelper';
import { extension } from '@tessa/application';
import { DisposeList, StorageHelper, TypedField } from '@tessa/core';
import { CardRow, CardRowsListener, WorkflowCardTypes } from '@tessa/platform';
import { Visibility } from 'tessa/platform';

@extension()
export class KrSettingsForumsSettingsUIExtension extends CardUIExtension {
  cardRowsListener: CardRowsListener | null;
  readonly disposeList = new DisposeList();

  public initialized(context: ICardUIExtensionContext): void {
    const { model } = context;
    if (model.inSpecialMode) {
      return;
    }

    const hasForumLicense =
      StorageHelper.tryGet<boolean>(model.card.tryGetInfo(), ForumHelper.LicenseWarningFlag) !==
      true;

    const cardTypeId = model.card.typeId;
    if (cardTypeId === WorkflowCardTypes.KrSettingsTypeID) {
      const sections = model.card.tryGetSections();

      const cardTypesSection = sections?.tryGet('KrSettingsCardTypes');
      const cardTypesControl = model.controls.get('CardTypeControl');

      if (cardTypesSection && cardTypesControl) {
        const cardTypesGrid = cardTypesControl as GridViewModel;

        cardTypesGrid.rowInvoked.add(e => {
          if (e.action === GridRowAction.Inserted || e.action === GridRowAction.Opening) {
            const forumsBlock = e.rowModel?.blocks.get('UseForumBlock');

            if (forumsBlock) {
              const useForumFirst = e.row.get<boolean>(ForumHelper.UseForumField) ?? false;

              WfUiHelper.setControlVisibility(
                forumsBlock,
                ForumHelper.UseForumSuffix,
                useForumFirst
              );

              const dispose = e.row.fieldChanged.add((e, s) => {
                if (e.fieldName === ForumHelper.UseForumField) {
                  const useForum = e.fieldValue as boolean;
                  s.set(
                    ForumHelper.UseDefaultDiscussionTabField,
                    TypedField.createBoolean(useForum)
                  );
                  WfUiHelper.setControlVisibility(
                    forumsBlock,
                    ForumHelper.UseForumSuffix,
                    useForum
                  );
                }
              });

              if (dispose) {
                forumsBlock.form.closed.addOnce(() => {
                  dispose();
                });
              }
            }

            if (!hasForumLicense) {
              const warningLabel = e.rowModel?.controls.get(ForumHelper.LicenseWarningControlAlias);
              if (warningLabel) {
                warningLabel.controlVisibility = Visibility.Visible;
              }
            }
          }
        });

        for (const row of cardTypesSection.rows) {
          this.attachHandlersToCardTypeRow(row);
        }

        this.cardRowsListener = new CardRowsListener();
        this.cardRowsListener.rowInserted.add(({ row }) => this.attachHandlersToCardTypeRow(row));
        this.cardRowsListener.start(cardTypesSection.rows);
      }
    } else if (cardTypeId === WorkflowCardTypes.KrDocTypeTypeID) {
      const sections = model.card.tryGetSections();
      if (sections) {
        const docTypeSection = sections.tryGet('KrDocType');
        const useForumBlock = model.blocks.get('UseForumBlock');
        if (docTypeSection && useForumBlock) {
          const useForumFirst =
            docTypeSection.fields.get<boolean>(ForumHelper.UseForumField) ?? false;
          WfUiHelper.setControlVisibility(useForumBlock, ForumHelper.UseForumSuffix, useForumFirst);

          const dispose = docTypeSection.fields.fieldChanged.add((e, s) => {
            if (e.fieldName === ForumHelper.UseForumField) {
              const useForum = e.fieldValue as boolean;
              s.set(ForumHelper.UseDefaultDiscussionTabField, TypedField.createBoolean(useForum));
              WfUiHelper.setControlVisibility(useForumBlock, ForumHelper.UseForumSuffix, useForum);
            }
          });

          if (dispose) {
            this.disposeList.add(dispose);
          }
        }
      }

      if (!hasForumLicense) {
        const warningLabel = model.controls.get(ForumHelper.LicenseWarningControlAlias);
        if (warningLabel) {
          warningLabel.controlVisibility = Visibility.Visible;
        }
      }
    }
  }

  public finalized(): void {
    if (this.cardRowsListener) {
      this.cardRowsListener.stop();
      this.cardRowsListener = null;
    }

    this.disposeList.dispose();
  }

  private attachHandlersToCardTypeRow(cardTypeRow: CardRow): void {
    const dispose = cardTypeRow.fieldChanged.add((e, s) => {
      if (e.fieldName === ForumHelper.UseForumField && !(e.fieldValue as boolean)) {
        s.set(
          ForumHelper.UseDefaultDiscussionTabField,
          TypedField.createBoolean(e.fieldValue as boolean)
        );
      }
    });

    if (dispose) {
      this.disposeList.add(dispose);
    }
  }
}
