export interface ICardTypeSelectorNodeBase {
  readonly id: string;
  readonly caption: string;

  handleClick(e: React.MouseEvent): void;
  handleDoubleClick(e: React.MouseEvent): void;
}

export interface ICardTypeSelectorNodeGroup extends ICardTypeSelectorNodeBase {
  readonly children: ICardTypeSelectorNode[];
}

export interface ICardTypeSelectorNode extends ICardTypeSelectorNodeBase {
  readonly selected: boolean;
  readonly cardType: ICardTypeSelectorCardType;
}

export function isNodeLeafNode(node: ICardTypeSelectorNodeBase): node is ICardTypeSelectorNode {
  return 'selected' in node;
}

export function isNodeGroupNode(
  node: ICardTypeSelectorNodeBase
): node is ICardTypeSelectorNodeGroup {
  return 'children' in node;
}

export interface ICardTypeSelectorCardType {
  readonly cardTypeId: string;
  readonly cardTypeName: string;
  readonly cardTypeCaption: string;
  readonly docTypeId: string | null;
  readonly docTypeTitle: string | null;
}

export interface ICardTypeSelectorCardGroupInfo {
  readonly name: string;
  readonly caption: string;
}

export interface ICardTypeSelectorCardGroup {
  readonly groupInfo: ICardTypeSelectorCardGroupInfo;
  types: ICardTypeSelectorCardType[];
}

export interface ICardTypeSelectorTypesProvider {
  getTypes(): Promise<readonly ICardTypeSelectorCardGroup[]>;
}

export type CardTypeSelectorNodeClickEventArgs = {
  readonly node: ICardTypeSelectorNode;
};
