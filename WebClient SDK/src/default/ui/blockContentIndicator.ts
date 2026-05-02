import { FieldMapStorage } from 'tessa/cards';
import { ControlContentIndicator, ControlContentIndicatorVisitor } from './controlContentIndicator';
import { IBlockViewModel } from 'tessa/ui/cards';

export class BlockContentIndicator extends ControlContentIndicator<IBlockViewModel> {
  constructor(
    block: IBlockViewModel,
    cardFieldContainer: FieldMapStorage,
    fieldIDs: Map<string, string>
  ) {
    super([block], cardFieldContainer, fieldIDs);
  }

  protected visitControl(visitor: ControlContentIndicatorVisitor, control: IBlockViewModel): void {
    visitor.visitByBlock(control);
  }
  protected getDisplayName(control: IBlockViewModel): string {
    return control.caption;
  }
  protected setDisplayName(control: IBlockViewModel, name: string): void {
    control.caption = name;
  }
}
