import { CardTableViewControlViewModel } from './cardTableViewControlViewModel';
import { CardTableViewInitializationStrategy } from './cardTableViewInitializationStrategy';
import { CardTypeControl, CardTypeTableControl } from 'tessa/cards/types';
import { Guid, unseal } from 'tessa/platform';
import { tryGetFromSettings } from 'tessa/ui';
import { ICardModel } from 'tessa/ui/cards';
import { IStorage } from 'tessa/platform/storage';
import { CardTask } from '@tessa/platform';

export async function initializeViewTable(
  settings: IStorage,
  model: ICardModel,
  cardTask: CardTask | null
): Promise<void> {
  const viewControlAlias = tryGetFromSettings<string>(settings, 'ViewControlAlias');
  if (!viewControlAlias) {
    return;
  }

  const tableSettings = tryGetFromSettings<IStorage>(settings, 'TableSettings');
  if (tableSettings == null) {
    return;
  }

  const tableControl = deserializeTable(tableSettings);
  if (Guid.equals(tableControl.sectionId, Guid.empty)) {
    return;
  }

  if (!cardTask) {
    model.controlCreationOverrides.push(async (control, _block, _form, _parentControl, model) => {
      if (control.name === viewControlAlias) {
        const viewModel = new CardTableViewControlViewModel(
          unseal<CardTypeControl>(control),
          model,
          tableControl,
          settings
        );
        viewModel.initializeStrategy(new CardTableViewInitializationStrategy(), true);
        return viewModel;
      }

      return null;
    });
  } else {
    model.taskInitializers.push(async taskCardModel => {
      if (taskCardModel.cardTask === cardTask) {
        taskCardModel.controlCreationOverrides.push(
          async (control, _block, _form, _parentControl, model) => {
            if (control.name === viewControlAlias) {
              const viewModel = new CardTableViewControlViewModel(
                unseal<CardTypeControl>(control),
                model,
                tableControl,
                settings
              );
              viewModel.initializeStrategy(new CardTableViewInitializationStrategy(), true);
              return viewModel;
            }

            return null;
          }
        );
      }
    });
  }
}

function deserializeTable(settings: IStorage): CardTypeTableControl {
  return new CardTypeTableControl().deserializeFromStorage(settings);
}
