import { ComponentsRegistry } from '@tessa/ui';
import { CardControlTypes } from '@tessa/platform';
import { TaskInfoDataExample } from './46_taskInfoDataExample';
import { TaskInfoRolesExample } from './46_taskInfoRolesExample';

export function registerExampleTaskInfo(): void {
  ComponentsRegistry.instance.register(CardControlTypes.TaskInfoDataType.id, TaskInfoDataExample); // регистрируем кастомный компонент TaskInfoData
  ComponentsRegistry.instance.register(CardControlTypes.TaskInfoRolesType.id, TaskInfoRolesExample); // регистрируем кастомный компонент TaskInfoRoles
}
