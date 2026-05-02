import { injectable } from '@tessa/application';
import ColorPicker, { ColorPaletteViewModel, ForegroundPaletteAlias } from 'ui/colorPicker';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class ColorPickerArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Color picker',
      order: 1
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Basic',
      props: async () => {
        const colorPalette = new ColorPaletteViewModel(ForegroundPaletteAlias);

        return {
          colorPalette
        };
      },
      view: ({ colorPalette }) => (
        <DemoForm>
          <ColorPicker palette={colorPalette} />
        </DemoForm>
      )
    });
  }
}
