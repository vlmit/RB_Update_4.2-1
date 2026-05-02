import moment from 'moment';
import isSubnet from 'is-subnet';
import { ValidationContainerConstants } from '@tessa/ui';
import { extension, inject, localize } from '@tessa/application';
import { Guid, StringHelper } from '@tessa/core';
import {
  ApiAccessTokenData,
  ApiAccessTokenGetRequest,
  ApiAccessTokenNewRequest,
  ApiAccessTokenNewResponse,
  IAccessTokenInfo,
  IApiAccessTokenService,
  IApiAccessTokenService$,
  PlatformResourceTypes
} from '@tessa/platform';
import { showLoadingOverlay } from 'tessa/ui/loadingOverlay';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { DatePickerViewModel } from 'ui/datePicker/datePickerViewModel';
import { AdvancedCardDialogManager } from 'tessa/ui/cards/advancedCardDialogManager';
import { IFormExtensionContext } from 'tessa/ui/formEditor/extensions/formExtension';
import { IFormDialogManager, IRuntimeBlockViewModel } from 'tessa/ui/formEditor/types';
import { IDataProvider } from 'tessa/ui/formEditor/data/definitions';
import { IFormDialogManager$ } from 'tessa/ui/formEditor/injects';
import { FlexRuntimeBlockViewModel } from 'tessa/ui/formEditor/blocks/flex/viewModels/flexRuntimeBlockVIewModel';
import { Button } from 'ui/button/buttonViewModel';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { AutocompleteMode, AutocompleteViewModel, IAutocompleteRecord } from 'ui/autocomplete';
import { ApiAccessTokensScopeAutocompleteDataSource } from './apiAccessTokensScopeAutocompleteDataSource';
import { ApiAccessTokensBaseFormExtension } from './apiAccessTokensBaseFormExtension';
import { ApiAccessTokensHelper } from './apiAccessTokensHelper';

@extension({ name: 'ApiAccessTokensInfoLayoutExtension' })
export class ApiAccessTokensInfoLayoutExtension extends ApiAccessTokensBaseFormExtension {
  //#region constructors

  constructor(
    @inject(IFormDialogManager$) private readonly _dialogManager: IFormDialogManager,
    @inject(IApiAccessTokenService$) private readonly _apiAccessTokenService: IApiAccessTokenService
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initializing(context: IFormExtensionContext): Promise<void> {
    if (context.validationResult.isSuccessful) {
      context.itemDataSourceResolvers?.set('Scope', (_, __, ___, rootDataProvider) => {
        return new ApiAccessTokensScopeAutocompleteDataSource('Scope', rootDataProvider);
      });
    }
  }

  override async initialized(context: IFormExtensionContext): Promise<void> {
    const block = context.runtimeView?.children![0] as IRuntimeBlockViewModel;
    const data = context.runtimeView?.rootDataProvider;
    const hash = ApiAccessTokensHelper.getHash(context.runtimeView?.info);
    if (!block || !data || !context.validationResult.isSuccessful) {
      return;
    }

    if (!hash) {
      this.initializeFields(data, null);
      this.initializeControls(data, block);
      this.initializeEditableControls(data, block);
      return;
    }

    const response = await showLoadingOverlay(() =>
      this._apiAccessTokenService.get(new ApiAccessTokenGetRequest(hash))
    );
    const result = response.validationResult.build();
    context.validationResult.add(result);
    if (result.isSuccessful && response.tokenData) {
      this.initializeFields(data, response.tokenData);
      this.initializeControls(data, block);
      this.initializeReadableControls(data, block);
    }
  }

  //#endregion

  //#region private methods

  private initializeFields(data: IDataProvider, token: ApiAccessTokenData | null = null): void {
    const setter = token ? data.rawSetValue.bind(data) : data.setValue.bind(data);
    const scopes = token?.scope.split(' ') ?? [ApiAccessTokensHelper.ApiReadScope];
    setter('UseReadScope', this.hasInScope(scopes, ApiAccessTokensHelper.ApiReadScope));
    setter('UseWriteScope', this.hasInScope(scopes, ApiAccessTokensHelper.ApiWriteScope));
    setter('Scope', scopes.map(scope => ({ id: scope, name: scope })) ?? []);
    setter('CreatedBy', this.getAutocompleteData(token?.createdById, token?.createdByName));
    setter('Language', this.getAutocompleteData(token?.languageId, token?.languageName));
    setter('TimeZone', this.getAutocompleteData(token?.timeZoneId, token?.timeZoneName));
    setter('User', this.getAutocompleteData(token?.userId, token?.userName));
    setter('Description', token?.description ?? PlatformResourceTypes.api);
    setter('LastActivity', token?.lastActivity ?? null);
    setter('Created', token?.created ?? null);
    setter('Expires', token?.expires ?? null);
    setter('Subnet', token?.subnet ?? null);
    setter('Hash', token?.hash ?? null);
    setter('TokenID', token?.id ?? null);
  }

  private initializeControls(_data: IDataProvider, block: IRuntimeBlockViewModel): void {
    for (const alias of ['Scope', 'Language', 'TimeZone']) {
      const control = this.getControl<AutocompleteViewModel>(block, alias);
      if (control) {
        control.menu.canOpenRecord = false;
      }
    }

    for (const alias of ['CreatedBy', 'User']) {
      const control = this.getControl<AutocompleteViewModel>(block, alias);
      if (control) {
        control.menu.openAction.action = async () => {
          const cardId = control.selectedRecord?.model.id as string;
          if (Guid.isValid(cardId)) {
            if (!control.menu.openAction.autoclose) {
              control.menu.close();
            }
            await showLoadingOverlay(() => AdvancedCardDialogManager.instance.openCard({ cardId }));
          }
        };
      }
    }
  }

  private initializeReadableControls(_data: IDataProvider, block: IRuntimeBlockViewModel): void {
    this.getDisplayedControl(block, 'LastActivity');
    this.getDisplayedControl(block, 'Created');
    this.getDisplayedControl(block, 'CreatedBy');

    for (const alias of ['TokenID', 'Hash']) {
      const control = this.getDisplayedControl<TextFieldViewModel>(block, alias);
      if (control) {
        control.toolbar.buttons.availability = 'enabled';
        control.toolbar.buttons.add(this.createCopyButton(control.text));
      }
    }
  }

  private initializeEditableControls(data: IDataProvider, block: IRuntimeBlockViewModel): void {
    this.getEnabledControl(block, 'User');
    this.getEnabledControl(block, 'Language');
    this.getEnabledControl(block, 'TimeZone');

    const description = this.getEnabledControl<TextFieldViewModel>(block, 'Description');
    if (description) {
      description.validationContainer.add(
        context => {
          if (description.required && !context.value) {
            context.addError({
              fieldName: description.alias || '',
              message: localize('$CardTypes_Validators_ApiAccessToken_DescriptionRequired')
            });
          }
        },
        {
          id: ValidationContainerConstants.requiredValidator,
          order: ValidationContainerConstants.requiredValidatorOrder
        }
      );
    }

    const scope = this.getEnabledControl<AutocompleteViewModel>(block, 'Scope');
    if (scope) {
      scope.mode = 0 as AutocompleteMode;
      scope.manualInput = true;
      scope.onAddedRecord.add(({ record }) => {
        if (this.hasInScope(record.name, ApiAccessTokensHelper.ApiReadScope)) {
          data.setValue('UseReadScope', true);
        } else if (this.hasInScope(record.name, ApiAccessTokensHelper.ApiWriteScope)) {
          data.setValue('UseWriteScope', true);
        }
      });
      scope.onRemovedRecord.add(
        ({ record }) => {
          if (
            this.hasInScope(record.name, ApiAccessTokensHelper.ApiReadScope) &&
            !scope.records.some(({ model }) =>
              this.hasInScope(model.name, ApiAccessTokensHelper.ApiReadScope)
            )
          ) {
            data.setValue('UseReadScope', false);
          } else if (
            this.hasInScope(record.name, ApiAccessTokensHelper.ApiWriteScope) &&
            !scope.records.some(({ model }) =>
              this.hasInScope(model.name, ApiAccessTokensHelper.ApiWriteScope)
            )
          ) {
            data.setValue('UseWriteScope', false);
          }
        },
        { order: Number.MAX_SAFE_INTEGER }
      );
      scope.validationContainer.add(
        context => {
          if (scope.required && !context.value && !scope.records.length) {
            context.addError({
              fieldName: scope.alias || '',
              message: localize('$CardTypes_Validators_ApiAccessToken_ScopeRequired')
            });
          }
        },
        {
          id: ValidationContainerConstants.requiredValidator,
          order: ValidationContainerConstants.requiredValidatorOrder
        }
      );
    }

    const useReadScope = this.getEnabledControl<CheckboxViewModel>(block, 'UseReadScope');
    if (useReadScope) {
      useReadScope.onChange.add((_, vm) => {
        vm.checked
          ? scope?.addRecord({ record: ApiAccessTokensHelper.ApiReadScopeRecord })
          : scope?.removeRecord({ record: ApiAccessTokensHelper.ApiReadScopeRecord });
      });
    }

    const useWriteScope = this.getEnabledControl<CheckboxViewModel>(block, 'UseWriteScope');
    if (useWriteScope) {
      useWriteScope.onChange.add((_, vm) => {
        vm.checked
          ? scope?.addRecord({ record: ApiAccessTokensHelper.ApiWriteScopeRecord })
          : scope?.removeRecord({ record: ApiAccessTokensHelper.ApiWriteScopeRecord });
        useReadScope && (useReadScope.disabled = vm.checked);
      });
    }

    const subnet = this.getEnabledControl<TextFieldViewModel>(block, 'Subnet');
    if (subnet) {
      subnet.validationContainer.add(
        context => {
          if (context.value && !isSubnet(context.value)) {
            context.addError({
              fieldName: subnet.alias || '',
              message: localize('$CardTypes_Validators_ApiAccessToken_SubnetInvalid')
            });
          }
        },
        {
          id: ValidationContainerConstants.requiredValidator,
          order: ValidationContainerConstants.requiredValidatorOrder
        }
      );
    }

    const expires = this.getEnabledControl<DatePickerViewModel>(block, 'Expires');
    if (expires) {
      expires.minDate = moment().utc().startOf('d');
    }

    const buttonsBlock = block.getItem<FlexRuntimeBlockViewModel>('ButtonsBlock');
    if (buttonsBlock) {
      buttonsBlock.hidden = false;

      const create = this.getDisplayedControl<Button>(buttonsBlock, 'Create');
      if (create) {
        create.buttonAction = async () => {
          let validationResult = this.validateControls(block);
          if (validationResult.isSuccessful) {
            const response = await this.createToken(data);
            validationResult = response.validationResult.build();
            if (validationResult.isSuccessful && response.tokenInfo) {
              // не выполняется ожидание асинхронного вызова чтобы не держать диалог
              this.showTokenInfo(response.tokenInfo);
              this._dialogManager.closeDialog(true);
            }
          } else {
            await showNotEmpty(validationResult);
          }
        };
      }

      const cancel = this.getDisplayedControl<Button>(buttonsBlock, 'Cancel');
      if (cancel) {
        cancel.buttonAction = async () => {
          await this._dialogManager.closeDialog(false);
        };
      }
    }
  }

  private createToken(data: IDataProvider): Promise<ApiAccessTokenNewResponse> {
    const newRequest = new ApiAccessTokenNewRequest();
    newRequest.description = data.getValue<string | null>('Description') ?? '';
    newRequest.expires = data.getValue<string | null>('Expires');
    newRequest.subnet = data.getValue<string | null>('Subnet');
    const user = data.getValue<IAutocompleteRecord[]>('User')[0];
    newRequest.userId = (user?.id as string) ?? null;
    newRequest.userName = user?.name ?? null;
    const timeZone = data.getValue<IAutocompleteRecord[]>('TimeZone')[0];
    newRequest.timeZoneId = (timeZone?.id as number) ?? null;
    newRequest.timeZoneName = timeZone?.name ?? null;
    const language = data.getValue<IAutocompleteRecord[]>('Language')[0];
    newRequest.languageId = (language?.id as number) ?? null;
    newRequest.languageName = language?.name ?? null;
    newRequest.scope = data
      .getValue<IAutocompleteRecord[]>('Scope')
      .map(r => r.name!.toLocaleLowerCase())
      .sort((a, b) => a.localeCompare(b))
      .reduce((scopes: string[], scope: string) => {
        if (scopes.length === 0 || scopes[scopes.length - 1] !== scope) {
          scopes.push(scope);
        }
        return scopes;
      }, [])
      .join(' ');

    return this._apiAccessTokenService.create(newRequest);
  }

  private showTokenInfo(tokenInfo: IAccessTokenInfo): Promise<void> {
    return ApiAccessTokensHelper.openLayoutDialog(
      this._dialogManager,
      ApiAccessTokensHelper.ApiAccessTokensTokenLayout,
      undefined,
      { token: tokenInfo.token }
    );
  }

  //#endregion

  //#region helpers

  private hasInScope(scope?: string | string[] | null, value?: string | null): boolean {
    if (!scope) {
      return false;
    } else if (typeof scope === 'string') {
      return StringHelper.equals(scope, value);
    } else {
      return scope.some(s => StringHelper.equals(s, value));
    }
  }

  //#endregion
}
