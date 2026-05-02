import { ComponentsRegistry } from '@tessa/ui';
import { ExampleHeader } from './31_exampleHeader';
import { ExampleHeaderViewModel } from './31_exampleHeaderViewModel';

export function registerExampleHeaderTypes(): void {
  // регистрируем компонент для хэдера и соответствующую ему вью-модель
  ComponentsRegistry.instance.register(ExampleHeaderViewModel, ExampleHeader);
}
