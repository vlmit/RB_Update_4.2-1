import { inject, injectable } from '@tessa/application';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { UIButton, UIButtonComponent } from 'tessa/ui';
import { Visibility } from 'tessa/platform';
import { PropertyGridComponent } from 'tessa/ui/propertyGrid';
import { AsyncLazy } from '@tessa/core';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { WorkflowHashEditorViewModel } from 'tessa/ui/workflow/workflowHashEditor/workflowHashEditorViewModel';
import { WorkflowSettingsComplexTypesProvider } from 'tessa/ui/workflow/workflowHashEditor/workflowSettingsComplexTypesProvider';
import { WorkflowHashParameterComplexTypesCache$ } from 'tessa/ui/workflow';
import { WorkflowHashParameterComplexTypesCache } from 'tessa/ui/workflow/workflowHashParameterComplexTypesCache';

@injectable()
export class WorkflowHashEditorArticle extends PlaygroundArticle {
  //#region fields and constants

  private readonly _name = 'Workflow/HashEditor';
  private readonly _description = 'Hash editor';

  //#endregion

  //#region constructors

  constructor(
    @inject(WorkflowHashParameterComplexTypesCache$, { lazy: true })
    private readonly _workflowHashParameterComplexTypesCache: AsyncLazy<WorkflowHashParameterComplexTypesCache>
  ) {
    super();
  }

  //#endregion

  //#region overrides
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: this._name,
      description: this._description
    };
  }

  override async initialize(): Promise<void> {
    await this.addHashEditor();
  }

  //#endregion

  //#region add blocks methods

  async addHashEditor(): Promise<void> {
    const workflowItemParametersEditor = new WorkflowHashEditorViewModel(
      {},
      new WorkflowSettingsComplexTypesProvider(
        await this._workflowHashParameterComplexTypesCache.getValue()
      ),
      {
        isReadonly: () => false
      }
    );
    await workflowItemParametersEditor.initialize();

    const saveButton = UIButton.create({
      name: 'SaveButton',
      caption: '$WorkflowEngine_UI_ItemParametersEditor_SaveButton',
      type: 'normal',
      theme: 'primary',
      visibility: Visibility.Visible,
      buttonAction: async () => {
        await workflowItemParametersEditor.save();
      }
    });

    const loadButton = UIButton.create({
      name: 'LoadButton',
      caption: '$WorkflowEngine_UI_ItemParametersEditor_LoadButton',
      type: 'normal',
      theme: 'primary',
      visibility: Visibility.Visible,
      buttonAction: async () => {
        workflowItemParametersEditor.reloadParameters();
      }
    });

    this.addBlock({
      caption: this._name,
      description: this._description,
      props: async () => {
        return { workflowItemParametersEditor };
      },
      view: ({ workflowItemParametersEditor }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 10px;
            height: 900px;

            .json-editor-buttons {
              display: flex;
              justify-content: flex-end;
              gap: 10px;
              width: 100%;

              button {
                width: auto;
              }
            }
          `}
        >
          <PropertyGridComponent
            modal={false}
            viewModel={workflowItemParametersEditor.parametersEditor}
          />
          <div className="json-editor-buttons">
            <UIButtonComponent viewModel={saveButton} />
            <UIButtonComponent viewModel={loadButton} />
          </div>
        </DemoForm>
      )
    });
  }

  //#endregion
}
