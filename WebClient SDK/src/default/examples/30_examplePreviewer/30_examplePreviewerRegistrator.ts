import { ComponentsRegistry } from '@tessa/ui';
import { ExamplePreviewerViewModel } from './30_examplePreviewerViewModel';
import { ExamplePreviewerComponent } from './30_examplePreviewerComponent';

export function registerExamplePreviewerTypes(): void {
  // регистрируем вью компонент и соответствующую ему вью-модель
  ComponentsRegistry.instance.register(ExamplePreviewerViewModel, ExamplePreviewerComponent);
}
