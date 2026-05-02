import { ExtensionRegistrator } from '@tessa/application';
import {
  IWorkflowActionDescriptorRegistry$,
  IWorkflowActionEditorRegistry$,
  IWorkflowActionHandler$,
  IWorkflowActionSettingsRegistry$
} from 'tessa/ui/workflow/workflowInjects';
import { KrDescriptors } from './krDescriptors';
import { IKrWorkflowActionCompletionOptionsProvider$ } from './injects';
import { IViewRepository$ } from '@tessa/platform';

export const KrWorkflowRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    const chunkContainer = container.asLazy(
      () => import(/* webpackChunkName: "workflowEngine" */ './chunk')
    );

    chunkContainer
      .bind(IKrWorkflowActionCompletionOptionsProvider$)
      .to(m => m.KrWorkflowActionCompletionOptionsProvider)
      .inSingletonScope();

    chunkContainer
      .bind(IWorkflowActionHandler$)
      .to(m => m.KrApprovalActionHandler)
      .inSingletonScope()
      .whenTargetNamed(KrDescriptors.krApprovalDescriptor.id);

    chunkContainer
      .bind(IWorkflowActionHandler$)
      .to(m => m.KrSigningActionHandler)
      .inSingletonScope()
      .whenTargetNamed(KrDescriptors.krSigningDescriptor.id);

    chunkContainer
      .bind(IWorkflowActionHandler$)
      .to(m => m.KrTaskRegistrationActionHandler)
      .inSingletonScope()
      .whenTargetNamed(KrDescriptors.krTaskRegistrationDescriptor.id);
  },

  async afterRegisterTypes(container) {
    container
      .get(IWorkflowActionDescriptorRegistry$)
      .register(KrDescriptors.krRouteInitializationDescriptor)
      .register(KrDescriptors.krAmendingDescriptor)
      .register(KrDescriptors.krResolutionDescriptor)
      .register(KrDescriptors.krApprovalDescriptor)
      .register(KrDescriptors.krSigningDescriptor)
      .register(KrDescriptors.krUniversalTaskDescriptor)
      .register(KrDescriptors.krTaskRegistrationDescriptor)
      .register(KrDescriptors.krRegistrationDescriptor)
      .register(KrDescriptors.krDeregistrationDescriptor)
      .register(KrDescriptors.changeStateDescriptor)
      .register(KrDescriptors.acquaintanceDescriptor);

    container
      .get(IWorkflowActionSettingsRegistry$)
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krRouteInitializationDescriptor.id,
        m => (action, actionState) => new m.KrRouteInitializationActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krResolutionDescriptor.id,
        m => (action, actionState) => new m.KrResolutionActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krAmendingDescriptor.id,
        m => (action, actionState) => new m.KrAmendingActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krApprovalDescriptor.id,
        m => (action, actionState) => new m.KrApprovalActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krSigningDescriptor.id,
        m => (action, actionState) => new m.KrSigningActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krUniversalTaskDescriptor.id,
        m => (action, actionState) => new m.KrUniversalTaskActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krTaskRegistrationDescriptor.id,
        m => (action, actionState) => new m.KrTaskRegistrationActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.changeStateDescriptor.id,
        m => (action, actionState) => new m.KrChangeStateActionStorage(action, actionState)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.acquaintanceDescriptor.id,
        m => (action, actionState) => new m.KrAcquaintanceActionStorage(action, actionState)
      );

    container
      .get(IWorkflowActionEditorRegistry$)
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krRouteInitializationDescriptor.id,
        m => (storage, options) =>
          new m.KrRouteInitializationActionEditorViewModel(storage, options)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krResolutionDescriptor.id,
        m => (storage, options) => new m.KrResolutionActionEditorViewModel(storage, options)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krAmendingDescriptor.id,
        m => (storage, options) => new m.KrAmendingActionEditorViewModel(storage, options)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krApprovalDescriptor.id,
        m => (storage, options) =>
          new m.KrApprovalActionEditorViewModel(storage, options, container.get(IViewRepository$))
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krSigningDescriptor.id,
        m => (storage, options) => new m.KrSigningActionEditorViewModel(storage, options)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krUniversalTaskDescriptor.id,
        m => (storage, options) => new m.KrUniversalTaskActionEditorViewModel(storage, options)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.krTaskRegistrationDescriptor.id,
        m => (storage, options) => new m.KrTaskRegistrationActionEditorViewModel(storage, options)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.changeStateDescriptor.id,
        m => (storage, options) => new m.KrChangeStateActionEditorViewModel(storage, options)
      )
      .registerLazy(
        () => import('./chunk'),
        KrDescriptors.acquaintanceDescriptor.id,
        m => (storage, options) => new m.KrAcquaintanceActionEditorViewModel(storage, options)
      );
  }
};
