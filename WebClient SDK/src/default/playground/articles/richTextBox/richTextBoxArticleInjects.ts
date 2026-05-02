import { createInjectToken } from '@tessa/application';
import {
  IRichTextBoxDataSource,
  RichTextBoxDataSourceOptions,
  RichTextBoxViewModelArgs
} from 'ui/richTextBox';
import { GeneratorType } from './contentGenerator/contentGeneratorTypes';

/** @category injects */
export const IArticleRichTextBoxDataSourceFactory$ = createInjectToken<
  (generatorType: GeneratorType, options?: RichTextBoxDataSourceOptions) => IRichTextBoxDataSource
>('IArticleRichTextBoxDataSourceFactory');

/** @category injects */
export const IArticleRichTextBoxArgsFactory$ = createInjectToken<
  (generatorType: GeneratorType, options?: RichTextBoxDataSourceOptions) => RichTextBoxViewModelArgs
>('IArticleRichTextBoxArgsFactory');
