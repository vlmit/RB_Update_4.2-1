import { CardUIExtension, ICardModel, ICardUIExtensionContext } from 'tessa/ui/cards';
import { showError, tryGetFromSettings } from 'tessa/ui';
import { ViewControlViewModel } from 'tessa/ui/cards/controls';
import { DoubleClickInfo } from 'tessa/ui/views';
import { DefaultFormTabWithTasksViewModel } from 'tessa/ui/cards/forms';
import { LocalizationManager } from 'tessa/localization';
import { DefaultCardTypeExtensionTypes } from '../cards/defaultCardTypeExtensionTypes';
import { IStorage } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardHelper, ICardTypeExtensionContext } from '@tessa/platform';
import { CardControlHelper } from 'tessa/ui/cards/controls/cardControlHelper';
import { ReferenceOpenMode } from 'tessa/ui/cards/controls/referenceOpenMode';

/**
 * @author Раткевич С.С. Syntellect (C) 2021
 * @description
 * UI расширение, реализующее функционал - открыть карточку из представления.
 */
@extension()
export class OpenCardInViewUIExtension extends CardUIExtension {
  public async initialized(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      DefaultCardTypeExtensionTypes.openCardInView,
      context.card,
      context.model.generalMetadata,
      this.executeInitializedActionAsync,
      context
    );

    context.validationResult.add(result);
  }

  private executeInitializedActionAsync = async (
    context: ICardTypeExtensionContext
  ): Promise<void> => {
    const extensionContext = context.externalContext as ICardUIExtensionContext;
    const settings = context.settings;
    if (!context.cardTask) {
      await this.attachDoubleClickHandlerAsync(extensionContext.model, settings);
    } else {
      const model = extensionContext.model;
      const tasks = (model.mainForm as DefaultFormTabWithTasksViewModel).tasks;
      if (!tasks) {
        return;
      }
      const task = tasks.find(x => x.taskModel.cardTask === context.cardTask);
      if (task) {
        task.modifyWorkspace(async () => {
          await this.attachDoubleClickHandlerAsync(task.taskModel, settings);
        });
      }
    }
  };

  private async attachDoubleClickHandlerAsync(cardModel: ICardModel, settings: IStorage | null) {
    if (settings === null) {
      return;
    }

    const viewControlName = tryGetFromSettings<string>(settings, 'ViewControlAlias', '');
    if (!viewControlName) {
      return;
    }

    const viewModel = cardModel.controls.get(viewControlName) as ViewControlViewModel;
    if (!viewModel) {
      return;
    }

    const prefixReference = tryGetFromSettings<string>(settings, 'ViewReferencePrefix', '')?.trim();
    const mapping = CardControlHelper.tryGetCardReferenceMapping(viewModel, prefixReference);
    const referenceOpenMode = CardControlHelper.getReferenceMode(settings, true);
    const dialogName = tryGetFromSettings<string>(settings, 'CardDialogName', '');
    viewModel.doubleClickAction = async (info: DoubleClickInfo) => {
      if (referenceOpenMode === ReferenceOpenMode.None) {
        return;
      }

      if (prefixReference && !mapping) {
        showError(
          LocalizationManager.instance.format(
            '$UI_Cards_TypesEditor_Exception_RefSectionInView',
            prefixReference,
            viewModel.cardTypeControl.caption,
            viewModel.block.cardTypeBlock.caption,
            viewModel.block.form.cardTypeForm['tabCaption'],
            viewModel.cardModel.cardType.caption
          )
        );
      }
      await CardControlHelper.doubleClickHandlerAsync(
        info,
        mapping,
        referenceOpenMode,
        null,
        dialogName
      );
    };
  }
}
