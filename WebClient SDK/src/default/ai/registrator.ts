import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { IAiTool$ } from 'tessa/ui/ai';
import { OutgoingWriterAiTool } from './tools/outgoingWriterAiTool';
import { CreateIncomingAiTool } from './tools/createIncomingAiTool';
import { TextEnhancementAiTool } from './tools/textEnhancementAiTool';
import { AiAssistantFileMenuExtension } from './aiAssistantFileMenuExtension';
import { ContractInfoAiTool } from './tools/contractInfoAiTool';
import { ComponentsRegistry } from '@tessa/ui';
import { AiTableWithTextViewModel } from './models/aiTableWithTextViewModel';
import { AiTableWithTextView } from './components/aiTableWithTextView';
import { AiCreateIncomingViewModel } from './models/aiCreateIncomingViewModel';
import { AiCreateIncomingView } from './components/aiCreateIncomingView';
import { AiAssistantFileUIExtension } from './aiAssistantFileUIExtension';

export const AiRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container
      .bind(IAiTool$)
      .to(OutgoingWriterAiTool)
      .inSingletonScope()
      .whenTargetNamed(OutgoingWriterAiTool.key);

    container
      .bind(IAiTool$)
      .to(CreateIncomingAiTool)
      .inSingletonScope()
      .whenTargetNamed(CreateIncomingAiTool.key);

    container
      .bind(IAiTool$)
      .to(TextEnhancementAiTool)
      .inSingletonScope()
      .whenTargetNamed(TextEnhancementAiTool.key);

    container
      .bind(IAiTool$)
      .to(ContractInfoAiTool)
      .inSingletonScope()
      .whenTargetNamed(ContractInfoAiTool.key);

    ComponentsRegistry.instance.registerFactory(AiTableWithTextViewModel, {
      componentFactory: () => AiTableWithTextView
    });

    ComponentsRegistry.instance.registerFactory(AiCreateIncomingViewModel, {
      componentFactory: () => AiCreateIncomingView
    });
  },
  async registerExtensions(container) {
    container.registerExtension({
      extension: AiAssistantFileUIExtension,
      stage: ExtensionStage.AfterPlatform,
      singleton: true
    });
    container.registerExtension({
      extension: AiAssistantFileMenuExtension,
      stage: ExtensionStage.AfterPlatform,
      singleton: true
    });
  }
};
