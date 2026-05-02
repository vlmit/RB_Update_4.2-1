import { ExtensionRegistrator, IExtensionContainer$ } from '@tessa/application';
import {
  IRichModuleSettingsProvider$,
  IRichTextBoxDataSource,
  IRichTextBoxDependenciesFactory$,
  RichTextBoxDataSource,
  RichTextBoxDataSourceOptions,
  RichTextBoxViewModelArgs
} from 'ui/richTextBox';
import { GeneratorType } from './contentGenerator/contentGeneratorTypes';
import {
  IArticleRichTextBoxArgsFactory$,
  IArticleRichTextBoxDataSourceFactory$
} from './richTextBoxArticleInjects';
export { ContentGeneratorModule } from './contentGenerator/contentGeneratorModule';
export { FakeCardArticleModule } from './fakeCard/fakeCardArticleModule';

export const RichTextBoxArticleRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container.bind(IArticleRichTextBoxDataSourceFactory$).toFactory(() => {
      const generators = new Map<GeneratorType, IRichTextBoxDataSource>();
      return (type: GeneratorType, options?: RichTextBoxDataSourceOptions) => {
        let dataSource = generators.get(type);
        if (!dataSource) {
          dataSource = new RichTextBoxDataSource(options);

          if (type !== GeneratorType.None) {
            generators.set(type, dataSource);
          }
        }

        return dataSource;
      };
    });
    container.bind(IArticleRichTextBoxArgsFactory$).toFactory(({ container }) => {
      const extensionContainer = container.get(IExtensionContainer$);
      const moduleSettingsProvider = container.get(IRichModuleSettingsProvider$);
      const dependenciesFactory = container.get(IRichTextBoxDependenciesFactory$);
      const dataSourceFactory = container.get(IArticleRichTextBoxDataSourceFactory$);

      return (
        type: GeneratorType,
        options?: RichTextBoxDataSourceOptions
      ): RichTextBoxViewModelArgs => {
        return {
          extensionContainer,
          moduleSettingsProvider,
          dependenciesFactory,
          dataSource: dataSourceFactory(type, options)
        };
      };
    });
  }
};
