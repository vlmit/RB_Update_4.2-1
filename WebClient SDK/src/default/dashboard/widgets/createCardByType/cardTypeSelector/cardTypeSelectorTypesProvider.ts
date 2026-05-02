import { Flags, IStorage, StorageHelper } from '@tessa/core';
import { ISession, ISession$, inject, injectable } from '@tessa/application';
import {
  CardInstanceType,
  CardType,
  CardTypeFlags,
  ICardMetadataRepository,
  ICardMetadataRepository$,
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper,
  KrDocType
} from '@tessa/platform';
import { MetadataStorage } from 'tessa/metadataStorage';
import { WorkspaceStorage } from 'tessa/workspaceStorage';
import { RoleHelper } from 'tessa/roles';
import {
  ICardTypeSelectorCardGroup,
  ICardTypeSelectorCardType,
  ICardTypeSelectorTypesProvider
} from './cardTypeSelectorTypes';

@injectable()
export class CardTypeSelectorTypesProvider implements ICardTypeSelectorTypesProvider {
  //#region fields

  private _cache: ICardTypeSelectorCardGroup[] | null = null;

  //#endregion

  //#region ctor

  constructor(
    @inject(ICardMetadataRepository$) private readonly _cardMetadata: ICardMetadataRepository,
    @inject(ISession$) private readonly _session: ISession,
    @inject(IKrTypesCache$) private readonly _typesCache: IKrTypesCache
  ) {}

  //#endregion

  //#region methods

  async getTypes(): Promise<readonly ICardTypeSelectorCardGroup[]> {
    this._cache ??= await this.getTypesCore();

    return this._cache;
  }

  private async getTypesCore(): Promise<ICardTypeSelectorCardGroup[]> {
    const result: ICardTypeSelectorCardGroup[] = [];

    const cardMetadata = await this._cardMetadata.getCardMetadata();
    const unavailableTypes = [
      ...MetadataStorage.instance.commonMetadata.unavailableTypes,
      ...this._typesCache.unavailableTypes
    ];
    const isAdmin = this._session.user.isAdmin;

    for (const cardType of cardMetadata.cardTypes) {
      if (
        cardType.instanceType === CardInstanceType.Card &&
        Flags.hasNotFlag(cardType.flags, CardTypeFlags.Hidden) &&
        Flags.hasNotFlag(cardType.flags, CardTypeFlags.Singleton) &&
        (isAdmin || Flags.hasNotFlag(cardType.flags, CardTypeFlags.Administrative)) &&
        !unavailableTypes.some(x => x === cardType.id)
      ) {
        if (
          !this._session.user.isAdmin &&
          cardType.id === RoleHelper.personalRoleTypeId &&
          !(await KrComponentsHelper.hasBaseTypesCache(cardType.id, this._typesCache))
        ) {
          continue;
        }

        const groupName = cardType.group ?? 'other';
        const groupCaption = cardType.group
          ? this.getCardTypeGroupCaption(groupName)
          : '$UI_Tiles_Other';

        let group = result.find(g => g.groupInfo.name === groupName);
        if (!group) {
          group = {
            groupInfo: {
              name: groupName,
              caption: groupCaption
            },
            types: []
          };
          result.push(group);
        }

        const components = await KrComponentsHelper.getKrComponentsTypesCache(
          cardType.id,
          this._typesCache
        );

        if (Flags.hasFlag(components, KrComponents.DocTypes)) {
          const types = (await this._typesCache.getTypes()) as KrDocType[];
          const docTypes = this.getDocTypes(cardType, types, unavailableTypes);
          if (docTypes.length > 0) {
            group.types.push(...docTypes);
          }
        } else {
          group.types.push({
            cardTypeId: cardType.id,
            cardTypeName: cardType.name,
            cardTypeCaption: cardType.caption,
            docTypeId: null,
            docTypeTitle: null
          });
        }
      }
    }

    return result.filter(group => group.types.length > 0);
  }

  private getDocTypes(
    cardType: CardType,
    types: KrDocType[],
    unavailableTypes: string[]
  ): ICardTypeSelectorCardType[] {
    if (unavailableTypes.some(x => x === cardType.id)) {
      return [];
    }

    const docTypesForCardType = types.filter(x => x.cardTypeId === cardType.id);
    const result: ICardTypeSelectorCardType[] = [];

    for (const docType of docTypesForCardType) {
      if (unavailableTypes.some(x => x === docType.id) || docType.hideCreationButton) {
        continue;
      }

      result.push({
        cardTypeId: cardType.id,
        cardTypeName: cardType.name,
        cardTypeCaption: cardType.caption,
        docTypeId: docType.id,
        docTypeTitle: docType.name
      });
    }

    return result;
  }

  private getCardTypeGroupCaption(groupName: string): string {
    const currentWorkspace = WorkspaceStorage.instance.currentWorkspace;
    if (!currentWorkspace) {
      return '';
    }

    const tileWorkspace = currentWorkspace.tileWorkspace;

    const captions =
      StorageHelper.tryGet<IStorage<string>>(tileWorkspace.info, 'TypeGroupCaption') ?? {};

    return captions[groupName] ?? groupName;
  }

  //#endregion
}
