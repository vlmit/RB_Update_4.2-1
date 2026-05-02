import React from 'react';
import { DynamicComponent, useDefaultViewModel } from '@tessa/ui';
import { AiTableWithTextViewModel } from '../models/aiTableWithTextViewModel';
import { renderMarkdown } from 'tessa/ui/ai/assistant/messages/useAiAssistantMessageTextContent';

/** Пропсы для компонента отображения таблицы с текстом под ней. */
type AiTableWithTextViewProps = {
  /** Вью модель для отображения таблицы с текстом под ней. */
  viewModel: AiTableWithTextViewModel;
};

/** Компонент для отображения таблицы с текстом под ней. */
export const AiTableWithTextView: React.FC<AiTableWithTextViewProps> = props => {
  const { mainRef } = useDefaultViewModel<HTMLDivElement>(props.viewModel);

  if (!props.viewModel.tableViewModel) {
    return null;
  }

  return (
    <div ref={mainRef}>
      <DynamicComponent viewModel={props.viewModel.tableViewModel} />
      {renderMarkdown(props.viewModel.text)}
    </div>
  );
};
