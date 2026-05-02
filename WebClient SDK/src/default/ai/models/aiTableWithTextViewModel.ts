import {
  AiAssistantToolTable,
  AiAssistantViewModel,
  AiTable,
  IAiToolDataViewModel,
  TextFormat
} from 'tessa/ui/ai';
import { ValidationError, ValidationResult, ValueOrFactory } from '@tessa/core';
import { localize } from '@tessa/application';
import { InitializableViewModelBase } from '@tessa/ui';

/** Вью модель для отображения таблицы с текстом под ней. */
export class AiTableWithTextViewModel
  extends InitializableViewModelBase
  implements IAiToolDataViewModel
{
  //#region fields

  protected _tableViewModel: IAiToolDataViewModel;
  private readonly _markdownLinkRegex: RegExp = /\[([^\[\]]+)\]\(([^\(\)]+)\)/g;

  //#endregion

  //#region constructors

  constructor(
    readonly text: string,
    private readonly _table: AiTable,
    private readonly _isActive: ValueOrFactory<boolean>,
    private readonly _assistant: AiAssistantViewModel
  ) {
    super();
  }

  //#endregion

  //#region props

  get tableViewModel(): IAiToolDataViewModel {
    return this._tableViewModel;
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    this._tableViewModel = await AiAssistantToolTable.getDataTableViewModel(
      this._table,
      this._isActive,
      this._assistant
    );
  }

  //#endregion

  //#region public methods

  toText(format: TextFormat = TextFormat.Markdown): string {
    switch (format) {
      case TextFormat.Plain:
        return this.toPlainText();
      case TextFormat.HTML:
        return this.toHTML();
      case TextFormat.Markdown:
        return this.toMarkdown();
      default:
        const error = localize('$Ai_AiAgent_Validation_TextFormatNotSupported');
        throw new ValidationError(ValidationResult.fromText(error));
    }
  }

  //#endregion

  //#region private methods

  private toPlainText(): string {
    const tableText = this._table.toText(TextFormat.Plain);
    const linkCaption = this.getMarkdownLinkCaption(this.text);
    return `${tableText}\n${linkCaption}`;
  }

  private getMarkdownLinkCaption(text: string): string {
    return text.replace(this._markdownLinkRegex, '$1');
  }

  private toHTML(): string {
    const tableText = this._table.toText(TextFormat.HTML);
    const htmlLink = this.markdownLinkToHtml(this.text);
    return `${tableText}${htmlLink}`;
  }

  private markdownLinkToHtml(text: string): string {
    return text.replace(this._markdownLinkRegex, '<a href="$2">$1</a>');
  }

  private toMarkdown(): string {
    return `${this._table.toText(TextFormat.Markdown)}\n\n${this.text}`;
  }

  //#endregion
}
