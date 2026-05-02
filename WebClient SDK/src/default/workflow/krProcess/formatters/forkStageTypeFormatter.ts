import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';
import { ForkStageTypeFormatterBase } from './forkStageTypeFormatterBase';

@extension({ name: 'ForkStageTypeFormatter' })
export class ForkStageTypeFormatter extends ForkStageTypeFormatterBase {
  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.forkDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    const builder = { text: '' };

    this.appendSecondaryProcessesNames(builder, context);

    context.displaySettings = builder.text;
  }

  //#endregion
}
