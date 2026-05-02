import React from 'react';
import { DynamicComponent, useDefaultViewModel } from '@tessa/ui';
import { AiCreateIncomingViewModel } from '../models/aiCreateIncomingViewModel';

/** Пропсы компонента инструмента ИИ для создания входящих договоров. */
type AiCreateIncomingViewProps = {
  /** Вью модель инструмента ИИ создания входящих договоров. */
  viewModel: AiCreateIncomingViewModel;
};

/** Компонент для отображения данных вью модели инструмента ИИ для создания входящих договоров.  */
export const AiCreateIncomingView: React.FC<AiCreateIncomingViewProps> = props => {
  const { mainRef } = useDefaultViewModel<HTMLDivElement>(props.viewModel);

  if (!props.viewModel.propertyGrid.isInitialized) {
    return null;
  }

  return (
    <div ref={mainRef}>
      <DynamicComponent viewModel={props.viewModel.propertyGrid} />
    </div>
  );
};
