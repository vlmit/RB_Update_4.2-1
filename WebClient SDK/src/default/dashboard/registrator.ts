import {
  DiContainer,
  ExtensionRegistrator,
  ExtensionStage,
  IExtensionContainer
} from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';
import {
  IDashboardWidgetTilesProvider$,
  IDashboardWidgetType$
} from 'tessa/ui/dashboard/dashboardInjects';
import { AiAssistantWidget } from './widgets/aiAssistant/aiAssistantWidget';
import { AiAssistantWidgetType } from './widgets/aiAssistant/aiAssistantWidgetType';
import { AiAssistantWidgetComponent } from './widgets/aiAssistant/aiAssistantWidgetComponent';
import { BirthdayWidget } from './widgets/birthday/birthdayWidget';
import { BirthdayWidgetComponent } from './widgets/birthday/birthdayWidgetComponent';
import { BirthdayWidgetType } from './widgets/birthday/birthdayWidgetType';
import { ViewBirthdayInfoProvider } from './widgets/birthday/viewBirthdayInfoProvider';
import { ButtonWidgetComponent } from './widgets/button/buttonWidgetComponent';
import { CreateCardByTemplateWidget } from './widgets/createCardByTemplate/createCardByTemplateWidget';
import { CreateCardByTemplateWidgetType } from './widgets/createCardByTemplate/createCardByTemplateWidgetType';
import { CardTypeSelectorTypesProvider } from './widgets/createCardByType/cardTypeSelector/cardTypeSelectorTypesProvider';
import { CreateCardByTypeWidget } from './widgets/createCardByType/createCardByTypeWidget';
import { CreateCardByTypeWidgetType } from './widgets/createCardByType/createCardByTypeWidgetType';
import { NavigatorWidget } from './widgets/navigator/navigatorWidget';
import { NavigatorWidgetType } from './widgets/navigator/navigatorWidgetType';
import { NotesWidget } from './widgets/notes/notesWidget';
import { NotesWidgetComponent } from './widgets/notes/notesWidgetComponent';
import { NotesWidgetType } from './widgets/notes/notesWidgetType';
import { SharedNotesWidgetType } from './widgets/notes/sharedNotesWidgetType';
import { SearchQueryWidget } from './widgets/searchQuery/searchQueryWidget';
import { SearchQueryWidgetType } from './widgets/searchQuery/searchQueryWidgetType';
import { TagDataManager } from './widgets/tag/tagDataManager';
import { TagWidget } from './widgets/tag/tagWidget';
import { TagWidgetComponent } from './widgets/tag/tagWidgetComponent';
import { TagWidgetType } from './widgets/tag/tagWidgetType';
import { TasksWidgetHeader } from './widgets/tasks/header/tasksWidgetHeader';
import { TasksWidgetHeaderComponent } from './widgets/tasks/header/tasksWidgetHeaderComponent';
import { TasksWidget } from './widgets/tasks/tasksWidget';
import { TasksWidgetType } from './widgets/tasks/tasksWidgetType';
import { TasksWidgetViewParameterManager } from './widgets/tasks/view/tasksWidgetViewParameterManager';
import { SearchQueryTilesProvider } from './widgets/tiles/searchQuery/searchQueryTilesProvider';
import { ViewWidgetTilesProvider } from './widgets/tiles/viewTile/viewWidgetTilesProvider';
import { UsersWidget } from './widgets/users/usersWidget';
import { UsersWidgetComponent } from './widgets/users/usersWidgetComponent';
import { UsersWidgetType } from './widgets/users/usersWidgetType';
import { ViewUserInfoProvider } from './widgets/users/viewUserInfoProvider';
import { ClearClipboardDashboardIUExtension } from './widgets/view/clearClipboardDashboardIUExtension';
import { CopyViewSettingsToClipboardViewExtension } from './widgets/view/copyViewSettingsToClipboardViewExtension';
import { ViewWidget } from './widgets/view/viewWidget';
import { ViewWidgetComponent } from './widgets/view/viewWidgetComponent';
import { ViewWidgetDashboardClipboard } from './widgets/view/viewWidgetDashboardClipboard';
import { ViewWidgetType } from './widgets/view/viewWidgetType';
import {
  IBirthdayInfoProvider$,
  ICardTypeSelectorTypesProvider$,
  ITagDataManager$,
  ITasksWidgetViewParameterManager$,
  IUserInfoProvider$,
  IViewWidgetDashboardClipboard$
} from './widgets/widgetInjects';
import { AiAssistantTilesProvider } from './widgets/tiles/aiAssistant/aiAssistantTilesProvider';

export const DashboardRegistrators: ExtensionRegistrator[] = [
  {
    async registerTypes(container: DiContainer): Promise<void> {
      container
        .bind(IDashboardWidgetTilesProvider$)
        .to(AiAssistantTilesProvider)
        .inSingletonScope();

      container.bind(IDashboardWidgetType$).to(AiAssistantWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(AiAssistantWidget, AiAssistantWidgetComponent);

      container.bind(IDashboardWidgetType$).to(BirthdayWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(BirthdayWidget, BirthdayWidgetComponent);

      container.bind(IDashboardWidgetType$).to(UsersWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(UsersWidget, UsersWidgetComponent);

      container.bind(IDashboardWidgetType$).to(NotesWidgetType).inSingletonScope();
      container.bind(IDashboardWidgetType$).to(SharedNotesWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(NotesWidget, NotesWidgetComponent);

      container.bind(IDashboardWidgetType$).to(ViewWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(ViewWidget, ViewWidgetComponent);

      container.bind(IBirthdayInfoProvider$).to(ViewBirthdayInfoProvider).inSingletonScope();

      container.bind(IUserInfoProvider$).to(ViewUserInfoProvider).inSingletonScope();

      container.bind(ITagDataManager$).to(TagDataManager).inSingletonScope();

      container.bind(IDashboardWidgetType$).to(CreateCardByTemplateWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(CreateCardByTemplateWidget, ButtonWidgetComponent);

      container.bind(IDashboardWidgetType$).to(CreateCardByTypeWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(CreateCardByTypeWidget, ButtonWidgetComponent);

      container
        .bind(IDashboardWidgetTilesProvider$)
        .to(SearchQueryTilesProvider)
        .inSingletonScope();

      container.bind(IDashboardWidgetType$).to(SearchQueryWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(SearchQueryWidget, ViewWidgetComponent);

      container.bind(IDashboardWidgetType$).to(TagWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(TagWidget, TagWidgetComponent);

      container.bind(IDashboardWidgetType$).to(NavigatorWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(NavigatorWidget, ButtonWidgetComponent);

      container.bind(IDashboardWidgetType$).to(TasksWidgetType).inSingletonScope();
      ComponentsRegistry.instance.register(TasksWidget, ViewWidgetComponent);
      ComponentsRegistry.instance.register(TasksWidgetHeader, TasksWidgetHeaderComponent);

      container
        .bind(ITasksWidgetViewParameterManager$)
        .to(TasksWidgetViewParameterManager)
        .inSingletonScope();

      container
        .bind(IViewWidgetDashboardClipboard$)
        .to(ViewWidgetDashboardClipboard)
        .inSingletonScope();

      container.bind(IDashboardWidgetTilesProvider$).to(ViewWidgetTilesProvider).inSingletonScope();

      container
        .bind(ICardTypeSelectorTypesProvider$)
        .to(CardTypeSelectorTypesProvider)
        .inSingletonScope();
    },
    async registerExtensions(container: IExtensionContainer): Promise<void> {
      container
        .registerExtension({
          extension: CopyViewSettingsToClipboardViewExtension,
          stage: ExtensionStage.AfterPlatform
        })
        .registerExtension({
          extension: ClearClipboardDashboardIUExtension,
          stage: ExtensionStage.Platform,
          singleton: true
        });
    }
  }
];
