import { injectable } from '@tessa/application';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { IconSelector, IconSelector$, IIconManager, IIconManager$ } from 'tessa/ui/iconSelector';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { Button } from 'ui/button/button';
import { IconSelectorArticle } from './iconSelectorArticle';
import { IconArticleViewModel } from './iconsArticleViewModel';
import './iconsArticle.scss';

@injectable()
export class IconsArticle extends PlaygroundArticle {
  constructor(
    @IIconManager$() private readonly _iconManger: IIconManager,
    @IconSelector$() private readonly _iconSelector: IconSelector
  ) {
    super();
  }

  override getSettings(): PlaygroundArticleSettings {
    return { name: 'Icons' };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: '',
      props: async () => {
        const iconsArticleViewModel = new IconArticleViewModel(this._iconManger);
        await iconsArticleViewModel.initialize({ useSearchBox: true });

        return { iconsArticleViewModel };
      },
      view: ({ iconsArticleViewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <IconSelectorArticle viewModel={iconsArticleViewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Icons Dialog',
      props: async () => {
        const iconSelectorBtn = ButtonViewModel.create({
          name: 'iconSelectorBtn',
          caption: 'Show Icon Dialog',
          icon: 'icon-thin-334',
          type: 'normal',
          theme: 'primary',
          buttonAction: async () => {
            const result = await this._iconSelector({
              useSearchBox: true,
              multiSelect: true,
              showIconCaption: true
            });

            console.log(result);
          }
        });

        return { iconSelectorBtn };
      },
      view: ({ iconSelectorBtn }) => (
        <DemoForm>
          <Button viewModel={iconSelectorBtn} />
        </DemoForm>
      )
    });
  }
}
