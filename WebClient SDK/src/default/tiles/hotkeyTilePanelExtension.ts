import { extension } from '@tessa/application';
import { Application } from 'tessa';
import { ApplicationExtension } from 'tessa/applicationExtension';
import { CommandHotkey } from 'tessa/ui';
import { TilePanelManager } from 'tessa/ui/sidePanel/tilePanelManager';
import { WorkspaceStorage } from 'tessa/workspaceStorage';
import LayerManager from 'ui/internal/layerManager';

@extension()
export class HotkeyTilePanelExtension extends ApplicationExtension {
  async initialize(): Promise<void> {
    const command = () => {
      if (LayerManager.isAnyDialogsExists()) {
        return;
      }

      const tilePanel = TilePanelManager.instance;

      if (tilePanel.isOpen) {
        tilePanel.closePanel();

        return;
      }
      tilePanel.openPanel('simple');

      const workspace = WorkspaceStorage.instance.currentWorkspace;
      if (workspace) {
        const panel = workspace.tileWorkspace.leftPanel;
        panel.search.focus();
      }
    };

    Application.instance.hotkeyStorage.addCommandHotkey(
      new CommandHotkey('Backquote', command, { ctrl: true })
    );
  }
}
