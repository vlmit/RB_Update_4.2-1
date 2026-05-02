import { WorkflowActionDescriptor } from 'tessa/ui/workflow/workflowActionDescriptor';

/**
 * Стандартные дескрипторы действий в WorkflowEngine.
 */
export namespace KrDescriptors {
  /**
   * Описание действия "Инициализация маршрута".
   */
  export const krRouteInitializationDescriptor = new WorkflowActionDescriptor({
    id: '25ca876a-50b2-4c27-b847-56d4fc597934',
    caption: '$KrActions_RouteInitialization',
    name: 'KrRouteInitializationAction',
    icon: 'm-flag',
    group: '$KrActions_RoutesGroup'
  });

  /**
   * Описание действия "Типовая задача".
   */
  export const krResolutionDescriptor = new WorkflowActionDescriptor({
    id: '235e42ea-7ad8-4321-9a3a-91b752985ef0',
    caption: '$KrActions_Resolution',
    name: 'KrResolutionAction',
    icon: 'm-task',
    group: '$KrActions_RoutesGroup'
  });

  /**
   * Описание действия "Доработка".
   */
  export const krAmendingDescriptor = new WorkflowActionDescriptor({
    id: '9c530e93-ec3a-48ba-b09c-ee9eceb2173e',
    caption: '$KrActions_Amending',
    name: 'KrAmendingAction',
    icon: 'm-pen',
    group: '$KrActions_RoutesGroup'
  });

  /**
   * Описание действия "Согласование".
   */
  export const krApprovalDescriptor = new WorkflowActionDescriptor({
    id: '70762c81-bd23-4580-a3fb-c452604f6e78',
    caption: '$KrActions_Approval',
    name: 'KrApprovalAction',
    icon: 'm-sogl',
    group: '$KrActions_RoutesGroup',
    version: 2
  });

  /**
   * Описание действия "Подписание".
   */
  export const krSigningDescriptor = new WorkflowActionDescriptor({
    id: '01762690-a192-4e8e-9b5e-0110666fd977',
    caption: '$KrActions_Signing',
    name: 'KrSigningAction',
    icon: 'm-annotation',
    group: '$KrActions_RoutesGroup',
    version: 2
  });

  /**
   * Описание действия "Настраиваемое задание".
   */
  export const krUniversalTaskDescriptor = new WorkflowActionDescriptor({
    id: '231eea47-db41-4ad4-8846-164da4ef4048',
    caption: '$KrActions_UniversalTask',
    name: 'KrUniversalTaskAction',
    icon: 'm-task-manage',
    group: '$KrActions_RoutesGroup',
    version: 2
  });

  /**
   * Описание действия "Задание регистрации".
   */
  export const krTaskRegistrationDescriptor = new WorkflowActionDescriptor({
    id: '2d6cbf60-1c5a-40fd-a091-fa42bd4441bc',
    caption: '$KrActions_TaskRegistration',
    name: 'KrTaskRegistrationAction',
    icon: 'm-task-reg',
    group: '$KrActions_RoutesGroup'
  });

  /**
   * Описание действия "Регистрация".
   */
  export const krRegistrationDescriptor = new WorkflowActionDescriptor({
    id: 'bf4641ad-f4dc-4a75-83f4-534cba8bf225',
    caption: '$KrActions_Registration',
    name: 'KrRegistrationAction',
    icon: 'm-reg',
    group: '$KrActions_StandardSolutionGroup'
  });

  /**
   * Описание действия "Отмена регистрации".
   */
  export const krDeregistrationDescriptor = new WorkflowActionDescriptor({
    id: '94e91c8c-1336-4c04-87c5-11ceb9839de3',
    caption: '$KrActions_Deregistration',
    name: 'KrDeregistrationAction',
    icon: 'm-reg-cancel',
    group: '$KrActions_StandardSolutionGroup'
  });
  /**
   * Описание действия "Смена состояния".
   */
  export const changeStateDescriptor = new WorkflowActionDescriptor({
    id: '4f07209c-ab6b-44f6-9460-f594c3bdf8a3',
    caption: '$KrActions_ChangeState',
    name: 'ChangeStateAction',
    group: '$KrActions_StandardSolutionGroup',
    icon: 'm-change',
    order: 99
  });

  /**
   * Описание действия "Ознакомление".
   */
  export const acquaintanceDescriptor = new WorkflowActionDescriptor({
    id: '956a34eb-8318-4d35-92a2-c0df118c01ea',
    caption: '$KrActions_Acquaintance',
    name: 'KrAcquaintanceAction',
    group: '$KrActions_StandardSolutionGroup',
    icon: 'm-pres'
  });
}
