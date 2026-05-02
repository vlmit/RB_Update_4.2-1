import { inject } from '@tessa/application';
import { RichTextBoxDataSourceOptions, RichTextBoxViewModelArgs } from 'ui/richTextBox';
import { PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { GeneratorType } from './contentGenerator/contentGeneratorTypes';
import { IArticleRichTextBoxArgsFactory$ } from './richTextBoxArticleInjects';

export abstract class RichTextBoxArticleBase extends PlaygroundArticle {
  //#region static field

  static readonly initializedStorages = new Set<GeneratorType>();

  //#endregion

  //#region ctor

  constructor(
    @inject(IArticleRichTextBoxArgsFactory$)
    private readonly _richTextBoxArgsFactory: (
      generatorType: GeneratorType,
      options?: RichTextBoxDataSourceOptions
    ) => RichTextBoxViewModelArgs
  ) {
    super();

    this.disposeList.add(() => RichTextBoxArticleBase.initializedStorages.clear());
  }

  //#endregion

  //#region methods

  protected getDefaultRichTextBoxParams(
    generatorType: GeneratorType,
    options?: RichTextBoxDataSourceOptions
  ): RichTextBoxViewModelArgs {
    return this._richTextBoxArgsFactory(generatorType, options);
  }

  //#endregion
}
