import { AvatarContentKind } from '@tessa/platform';
import { StorageAccessor, IStorage } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { IWorkplaceViewComponent, StandardViewComponentContentItemFactory } from 'tessa/ui/views';
import { TableGridViewModelBase } from 'tessa/ui/views/content';
import { IAvatarViewModelFactory$ } from 'ui/avatar/avatarInjects';
import { AvatarShape, AvatarSize, IAvatarViewModelFactory } from 'ui/avatar/avatarTypes';
import { UserAvatarTableCellViewModel } from './userAvatarTableCellViewModel';

@extension({ name: 'UserAvatarInRowViewExtension' })
export class UserAvatarInRowViewExtension extends WorkplaceViewComponentExtension {
  constructor(
    @inject(IAvatarViewModelFactory$) private readonly _avatarFactory: IAvatarViewModelFactory
  ) {
    super();
  }

  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.Avatars.UserAvatarInRowViewExtension';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    const settings = new UserAvatarInRowViewExtensionSettings(this.settingsStorage);
    if (!settings.destColumn || !settings.idColumn) {
      return;
    }

    const tableFactory = model.contentFactories.get(StandardViewComponentContentItemFactory.Table);
    if (!tableFactory) {
      return;
    }

    model.contentFactories.set(StandardViewComponentContentItemFactory.Table, c => {
      const vm = tableFactory(c);
      if (vm instanceof TableGridViewModelBase) {
        const initAction = vm.createCellAction;
        vm.createCellAction = options => {
          if (
            options.column.columnName === settings.destColumn &&
            (!options.value || typeof options.value === 'string')
          ) {
            const content = options.value;
            const userId = options.row.data.get(settings.idColumn!);
            if (userId && typeof userId === 'string') {
              const avatar = this._avatarFactory({
                avatarInfo: { id: userId, kind: settings.avatarContentKind },
                size: settings.avatarSize,
                shape: settings.avatarShape
              });
              return new UserAvatarTableCellViewModel({
                ...options,
                avatar: avatar,
                content: content
              });
            }
          }

          return initAction(options);
        };
      }

      return vm;
    });
  }
}

//#region UserAvatarInRowViewExtensionSettings

class UserAvatarInRowViewExtensionSettings {
  constructor(storage: IStorage) {
    const accessor = new StorageAccessor(storage);

    this.destColumn = accessor.tryGetString('DestinationColumn');
    this.idColumn = accessor.tryGetString('IDColumn');

    const size = accessor.tryGetStringOrDefault('AvatarSize');
    switch (size) {
      case 'Small':
        this.avatarSize = 'sm';
        break;
      case 'Medium':
        this.avatarSize = 'md';
        break;
      case 'Large':
        this.avatarSize = 'lg';
        break;
    }

    const shape = accessor.tryGetStringOrDefault('AvatarShape');
    switch (shape) {
      case 'Circle':
        this.avatarShape = 'circle';
        break;
      case 'Square':
        this.avatarShape = 'square';
        break;
    }

    this.avatarContentKind =
      accessor.tryGetEnumFromString('AvatarContentKind', AvatarContentKind) ??
      AvatarContentKind.Avatar;
  }

  public readonly destColumn: string | null;
  public readonly idColumn: string | null;
  public readonly avatarSize?: AvatarSize;
  public readonly avatarShape?: AvatarShape;
  public readonly avatarContentKind?: AvatarContentKind;
}

//#endregion
