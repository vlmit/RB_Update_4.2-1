//import { ODGetCertinfo } from './GetCertInfoFromProfile';
import { ODTestButton } from './CertificateButton';

import { ChangeFieldOrRowUIExtension } from './ODchangeFieldOrRowUIExtension';
import { TableSectionChangedUIExtension } from './ODtableSectionChangedUIExtension';
import { AdditionalTableButtonUIExtension } from './ODadditionalTableButtonUIExtension';
import { TableControlDoubleClickUIExtension } from './ODtableControlDoubleClickUIExtension';
import { ODControlTaskCustomButtonUIExtension } from './ui/ODControlTaskCustomButtonUIExtension';
//import { ODTaskTreeUIExtension } from './ui/ODTaskTreeUIExtension';
//import { ODFormExampleUIExtension } from './ui/ODFormExampleUIExtension';
//import { ODFormImportPFXUIExtension } from './ui/ODFormImportPFXUIExtension';
import { ODSignAllFileExtension } from './ui/ODSignAllFileExtension';
import { ODSigningUIApprovalExtension } from './ui/ODSigningUIApprovalExtension';
import { ODSigningUISignExtension } from './ui/ODSigningUISignExtension';
import { ODFileControlUIExtension } from './ui/ODFileControlUIExtension';
import { ODFileControlSecondPreviewUIExtension } from './ui/ODFileControlSecondPreviewUIExtension';
import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
//mobile
//import { ODCloseCardUIExtension } from './mobile_ui/ODCloseCardUIExtension';
//import { ODMobApprovalUIExtension } from './mobile_ui/ODMobApprovalUIExtension';
//import { ODMobSigningUIExtension } from './mobile_ui/ODMobSigningUIExtension';
//ExtensionContainer.instance.registerExtension({
//  extension: ODGetCertinfo,
//  stage: ExtensionStage.AfterPlatform
//});

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: ODTestButton,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ChangeFieldOrRowUIExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: TableSectionChangedUIExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: AdditionalTableButtonUIExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: TableControlDoubleClickUIExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODControlTaskCustomButtonUIExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODSignAllFileExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODSigningUIApprovalExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODSigningUISignExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODFileControlUIExtension,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODFileControlSecondPreviewUIExtension,
      stage: ExtensionStage.AfterPlatform
    });
  }
}

//ExtensionContainer.instance.registerExtension({
//  extension: ODTaskTreeUIExtension,
//  stage: ExtensionStage.AfterPlatform
//});
//ExtensionContainer.instance.registerExtension({
//  extension: ODFormExampleUIExtension,
//  stage: ExtensionStage.AfterPlatform
//});

//ExtensionContainer.instance.registerExtension({
//  extension: ODFormImportPFXUIExtension,
//  stage: ExtensionStage.AfterPlatform
//});
//ExtensionContainer.instance.registerExtension({
//  extension: showImportPFXDialogUIExtension,
//  stage: ExtensionStage.AfterPlatform
//});
//ExtensionContainer.instance.registerExtension({
// extension: ODCloseCardUIExtension,
// stage: ExtensionStage.AfterPlatform
//});
// ExtensionContainer.instance.registerExtension({
// extension: ODMobApprovalUIExtension,
// stage: ExtensionStage.AfterPlatform
// });
// ExtensionContainer.instance.registerExtension({
// extension: ODMobSigningUIExtension,
// stage: ExtensionStage.AfterPlatform
// });
