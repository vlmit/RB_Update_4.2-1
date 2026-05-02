import React from 'react';
import { observer } from 'mobx-react-lite';
import { AiAssistantView } from 'tessa/ui/ai';
import { AiAssistantWidget } from './aiAssistantWidget';

/** Компонент виджета {@link AiAssistantWidget}. */
export const AiAssistantWidgetComponent: React.FC<{ viewModel: AiAssistantWidget }> = observer(
  ({ viewModel: { isInitialized, aiAssistant } }) =>
    isInitialized ? <AiAssistantView viewModel={aiAssistant} /> : null
);
