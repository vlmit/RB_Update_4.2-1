import { inject, injectable } from '@tessa/application';
import { showMessage, UIButton } from 'tessa/ui';
import { AsyncLazy, TypedJsonConverter } from '@tessa/core';
import { IWorkflowActionSettingsEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowEditorTypes';
import { WorkflowActionStorage } from 'tessa/ui/workflow/chunk/models/workflowActionStorage';
import { WorkflowSettingsEditor } from 'tessa/ui/workflow/chunk/editors/workflowSettingsEditorView';
import {
  WorkflowParametrizedTestControlActionEditorViewModel,
  WorkflowParametrizedControlTestActionStorage
} from './workflowParametrizedControlTestAction';
import { WorkflowBindingContext } from 'tessa/ui/workflow/bindings/workflowBindingContext';
import { WorkflowActionEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowActionEditorViewModel';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { WorkflowSettingsComplexTypesProvider } from 'tessa/ui/workflow/workflowHashEditor/workflowSettingsComplexTypesProvider';
import { WorkflowHashParameterComplexTypesCache } from 'tessa/ui/workflow/workflowHashParameterComplexTypesCache';
import { WorkflowHashParameterComplexTypesCache$ } from 'tessa/ui/workflow';

@injectable()
export class WorkflowParametrizedControlTestArticle extends PlaygroundArticle {
  //#region ctors

  constructor(
    @inject(WorkflowHashParameterComplexTypesCache$, { lazy: true })
    protected readonly _workflowHashParameterComplexTypesCache: AsyncLazy<WorkflowHashParameterComplexTypesCache>
  ) {
    super();
  }

  //#endregion

  //#region overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Workflow/Editors/Controls',
      description: 'Workflow editor parametrized control'
    };
  }

  override async initialize(): Promise<void> {
    await this.addStartActionEditorNode();
  }

  //#endregion

  //#region add blocks methods

  async addStartActionEditorNode(): Promise<void> {
    const complexTypedProvider = new WorkflowSettingsComplexTypesProvider(
      await this._workflowHashParameterComplexTypesCache.getValue()
    );
    this.addTestViewModelBlock(`Base`, () => {
      const action = new WorkflowActionStorage();
      const storage = new WorkflowParametrizedControlTestActionStorage(action);
      const viewModel = new WorkflowParametrizedTestControlActionEditorViewModel(storage, {
        designMode: true,
        bindingContext: new WorkflowBindingContext({}, {}, action.hash, complexTypedProvider)
      });

      return { storage: action, viewModel };
    });
  }

  //#endregion

  //#region private methods

  private addTestViewModelBlock(
    caption: string,
    viewModelFactory: () => {
      storage: WorkflowActionStorage;
      viewModel: IWorkflowActionSettingsEditorViewModel;
    }
  ): void {
    this.addBlock({
      caption: caption,
      props: async () => {
        const { storage, viewModel } = viewModelFactory();
        const actionEditorViewModel = new WorkflowActionEditorViewModel(storage, viewModel);
        await actionEditorViewModel.initialize();

        return { actionEditorViewModel, storage };
      },
      view: ({ actionEditorViewModel }) => (
        <DemoForm
          customStyles={css => css({ width: '100%', display: 'flex', flexDirection: 'column' })}
        >
          <WorkflowSettingsEditor
            viewModel={actionEditorViewModel}
            editorClassName="workflow-process-action"
          />
        </DemoForm>
      ),
      buttons: ({ storage }) => [
        UIButton.create({
          name: 'ShowStorage',
          type: 'normal',
          theme: 'control',
          caption: 'Show Storage',
          buttonAction: async () => {
            await showMessage(
              TypedJsonConverter.serialize(storage.getStorage(), { stringifySpace: '  ' })
            );
          }
        })
      ]
    });
  }

  //#endregion
}
