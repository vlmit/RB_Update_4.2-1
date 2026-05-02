import { extension, localize } from '@tessa/application';
import {
  ObjectHelper,
  StringHelper,
  ValidationKey,
  ValidationResultType,
  ValidationResult,
  ValidationResultBuilder
} from '@tessa/core';
import { IViewModelBase, ValidationContainer } from '@tessa/ui';
import { IAutocompleteRecord } from 'ui/autocomplete';
import { Visibility } from 'tessa/platform/visibility';
import { UIButton } from 'tessa/ui/uiButton';
import { Alert } from 'tessa/ui/alerts/alert';
import { ClipboardHelper } from 'tessa/ui/clipboard';
import { FormExtension } from 'tessa/ui/formEditor/extensions/formExtension';
import { IRuntimeBlockViewModel } from 'tessa/ui/formEditor/types';
import { RuntimeItemWithStateViewModel } from 'tessa/ui/formEditor/controls/runtimeItemWithStateViewModel';
import { IControlViewModel } from 'tessa/ui/cards';

@extension({ name: 'ApiAccessTokensBaseFormExtension' })
export abstract class ApiAccessTokensBaseFormExtension extends FormExtension {
  //#region protected methods

  protected getAutocompleteData(
    id?: unknown | null,
    name?: string | null
  ): Array<IAutocompleteRecord> {
    return id != null ? [{ id: id, name: name ?? null }] : [];
  }

  protected getControl<T extends IControlViewModel | IViewModelBase>(
    block: IRuntimeBlockViewModel,
    alias: string
  ): T | null {
    return block.getItem<RuntimeItemWithStateViewModel>(alias)?.getCurrent<T>() ?? null;
  }

  protected getDisplayedControl<T extends IControlViewModel | IViewModelBase>(
    block: IRuntimeBlockViewModel,
    alias: string
  ): T | null {
    const control = this.getControl<T>(block, alias);
    if (ObjectHelper.hasProp(control, 'visibility')) {
      control.visibility = true;
    } else if (ObjectHelper.hasProp(control, 'controlVisibility')) {
      control.controlVisibility = Visibility.Visible;
    } else if (ObjectHelper.hasProp(control, 'hidden')) {
      control.hidden = false;
    }
    return control;
  }

  protected getEnabledControl<T extends IControlViewModel | IViewModelBase>(
    block: IRuntimeBlockViewModel,
    alias: string
  ): T | null {
    const control = this.getControl<T>(block, alias);
    if (ObjectHelper.hasProp(control, 'availability')) {
      control.availability = 'enabled';
    } else if (ObjectHelper.hasProp(control, 'isReadOnly')) {
      control.isReadOnly = false;
    } else if (ObjectHelper.hasProp(control, 'disabled')) {
      control.disabled = false;
    }
    return control;
  }

  protected validateControls(block: IRuntimeBlockViewModel): ValidationResult {
    const validationResult = new ValidationResultBuilder();
    for (const { alias } of block.children) {
      const control = alias && this.getControl(block, alias);
      if (ObjectHelper.hasProp(control, 'validationContainer')) {
        const validationContainer = control.validationContainer as ValidationContainer<unknown>;
        validationContainer && validationResult.add(validationContainer.result);
      } else if (ObjectHelper.hasProp(control, 'name', 'error')) {
        const name = control.name as string;
        const error = control.error as string;
        if (!StringHelper.isNullOrWhiteSpace(error)) {
          validationResult.add(ValidationKey.unknown, ValidationResultType.Error, error, name);
        }
      }
    }
    return validationResult.build();
  }

  protected createCopyButton(value: string): UIButton {
    return UIButton.create({
      name: 'Copy',
      icon: 'm-copy',
      type: 'small',
      theme: 'control',
      buttonAction: async () => {
        if (await ClipboardHelper.copyToClipboard(value)) {
          await Alert.show({ text: localize('$UI_Misc_Message_ValueCopiedToClipboard') });
        }
      }
    });
  }

  //#endregion
}
