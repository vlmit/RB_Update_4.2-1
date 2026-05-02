import { ExtensionBundleOrder } from '@tessa/application';
import { Application } from 'tessa/application';
import { CardsRegistrator } from './cards/registrator';
import { DeskiRegistrator } from './deski/registrator';
import { DeskiMobileRegistrator } from './deskiMobile/registrator';
import { DocumentsRegistrator } from './documents/registrator';
import { ConditionsRegistrator } from './conditions/registrator';
import { EDSRegistrator } from './eds/registrator';
import { FilesRegistrator } from './files/registrator';
import { OnlyOfficeRegistrator } from './onlyOffice/registrator';
import { ForumsRegistrator } from './forums/registrator';
import { TilesRegistrator } from './tiles/registrator';
import { UIKrProcessRegistrator } from './ui/krProcess/registrator';
import { UIRegistrator } from './ui/registrator';
import { ViewsRegistrator } from './views/registrator';
import { KrPermissionsRegistrator } from './workflow/krPermissions/registrator';
import { KrProcessRegistrator } from './workflow/krProcess/registrator';
import { WFRegistrator } from './workflow/wf/registrator';
import { WorkplacesRegistrator } from './workplaces/registrator';
import { OcrRegistrator } from './textRecognition/registrator';
import { LoginRegistrator } from './login/registrator';
import { ImagingRegistrator } from './imaging/registrator';
import { CommandInterpreterRegistrator } from './workflow/krProcess/commandInterpreter/registrator';
import { TempLinksRegistrator } from './tempLinks/registrator';
import { DashboardRegistrators } from './dashboard/registrator';
import { ApiAccessTokensRegistrator } from './apiAccessTokens/registrator';
import { AbTestRegistrator } from './abTest/registrator';
import { PlaygroundRegistrator } from './playground/playgroundRegistrator';
import { AiRegistrator } from './ai/registrator';
import { KrWorkflowRegistrator } from './workflow/workflowEngine/krWorkflowRegistrator';
import { MobileClientRegistrator } from './mobileClient/registrator';

/**
 * uncomment for examples extensions
 */
// import { ExamplesRegistrator } from './examples/registrator';
// import { WorkflowExamplesRegistrator } from './examples/workflow_examples/registrator';

Application.instance.registerBundle({
  name: 'Tessa.Extensions.Default.js',
  buildTime: process.env.BUILD_TIME!,
  order: ExtensionBundleOrder.Default,
  registry: [
    CardsRegistrator,
    DeskiRegistrator,
    DeskiMobileRegistrator,
    DocumentsRegistrator,
    ConditionsRegistrator,
    EDSRegistrator,
    FilesRegistrator,
    OnlyOfficeRegistrator,
    ForumsRegistrator,
    TilesRegistrator,
    UIKrProcessRegistrator,
    UIRegistrator,
    ViewsRegistrator,
    KrPermissionsRegistrator,
    KrProcessRegistrator,
    KrWorkflowRegistrator,
    WFRegistrator,
    WorkplacesRegistrator,
    OcrRegistrator,
    LoginRegistrator,
    ImagingRegistrator,
    CommandInterpreterRegistrator,
    TempLinksRegistrator,
    ...DashboardRegistrators,
    ApiAccessTokensRegistrator,
    AbTestRegistrator,
    PlaygroundRegistrator,
    AiRegistrator,
    MobileClientRegistrator
    // ExamplesRegistrator
    // WorkflowExamplesRegistrator
  ]
});
