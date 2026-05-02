import { extension } from '@tessa/application';
import { CompiledCardTypes, sendCompileRequest } from '../../workflow/krProcess/krUIHelper';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ButtonViewModel } from 'tessa/ui/cards/controls';

@extension()
export class KrStageSourceUIExtension extends CardUIExtension {
  public initialized(context: ICardUIExtensionContext): void {
    if (!CompiledCardTypes.some(x => x === context.card.typeId)) {
      return;
    }

    const cardModel = context.model;

    const compileControl = cardModel.controls.get('CompileButton') as ButtonViewModel;
    if (compileControl) {
      compileControl.onClick = () => sendCompileRequest('.CompileWithValidationResult');
    }

    const compileAllControl = cardModel.controls.get('CompileAllButton') as ButtonViewModel;
    if (compileAllControl) {
      compileAllControl.onClick = () => sendCompileRequest('.CompileAllWithValidationResult');
    }
  }
}
