import { BasePoint, Element, Text, Transforms } from 'slate';
import { ReactEditor } from 'slate-react';
import { CheckBoxElementHelper } from 'ui/richTextBox/modules/controls';
import { StylesModuleToken } from 'ui/richTextBox/modules/tokens';
import {
  IRichNodesProvider,
  RichEditorHelper,
  RichNode,
  RichNodeVisitor,
  RichTreeNode,
  SlateEditor
} from 'ui/richTextBox';

export class CheckboxStrikethroughVisitor extends RichNodeVisitor {
  //#region ctor

  constructor(
    private _node: RichNode,
    provider: IRichNodesProvider,
    editor: SlateEditor
  ) {
    super(provider, editor);
  }

  //#endregion

  //#region fields

  private _range: { start: BasePoint; end: BasePoint } | null;

  //#endregion

  //#region base overrides

  visit(): void {
    const stylesEditor = this.editor.extendedEditor;
    if (!RichEditorHelper.isEditor(stylesEditor, StylesModuleToken)) {
      throw new Error('Styles editor is not initialized');
    }

    super.visit();

    if (!this._range) {
      return;
    }

    Transforms.select(this.editor, {
      anchor: this._range.start,
      focus: this._range.end
    });

    const element = this._node.tryGetNode();
    if (!element || !CheckBoxElementHelper.isCheckboxElement(element)) {
      return;
    }

    stylesEditor.toggleMark('strikethrough', element.checked);
    Transforms.deselect(this.editor);
  }

  protected visitElement(
    parent: RichTreeNode,
    treeNode: RichTreeNode,
    slateNode: Element | null
  ): boolean {
    if (treeNode.node?.slateId !== this._node.slateId) {
      return true;
    }

    if (!slateNode) {
      return false;
    }

    const parentNode = parent.node?.tryGetNode();
    if (!slateNode || !parentNode) {
      return false;
    }

    const index = parentNode.children.findIndex(c => c === slateNode);
    if (index === -1) {
      return false;
    }

    let after = parentNode.children.slice(index + 1);
    const checkBoxNode = after.findIndex(a => CheckBoxElementHelper.isCheckboxElement(a));
    if (checkBoxNode !== -1) {
      after = after.slice(0, checkBoxNode);
    }

    const left = after[0];
    if (!RichEditorHelper.isText(left)) {
      return false;
    }

    let right: Text | null = null;
    for (let i = after.length - 1; i >= 0; i--) {
      const text = after[i];
      if (RichEditorHelper.isText(text)) {
        right = text;
        break;
      }
    }

    if (!right) {
      return false;
    }

    const pathLeft = ReactEditor.findPath(this.editor, left);
    const pathRight = ReactEditor.findPath(this.editor, right);
    const offset = right.text ? right.text.length : 0;
    if (pathLeft !== pathRight || offset) {
      const start = {
        path: pathLeft,
        offset: 0
      };
      const end = {
        path: pathRight,
        offset: offset
      };

      this._range = { start, end };
    }

    return false;
  }

  //#endregion
}
