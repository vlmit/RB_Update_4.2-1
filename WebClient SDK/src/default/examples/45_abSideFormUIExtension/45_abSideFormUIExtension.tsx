import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { SideFormViewModel } from 'tessa/ui/cards/forms/sideFormViewModel';
import { Button } from 'ui/button/button';
import { UIButton } from 'tessa/ui';
import './styles.scss';

/**
 * An example of using a card side form.
 */
@extension({ name: 'AbSideFormUIExtension' })
export class AbSideFormUIExtension extends CardUIExtension {
  override async initializing(context: ICardUIExtensionContext): Promise<void> {
    // Create new side form view model.
    const sideForm = new SideFormViewModel();

    // Add your content.
    // It can be a mix of react components, UIButton and viewModels that are
    // registered via ComponentRegistry.
    sideForm.content.push(
      <Button
        icon="m-save"
        type="toolbar"
        theme="control"
        onClick={() => context.model.saveAsync()}
      />,
      UIButton.create({
        icon: 'm-refresh',
        type: 'toolbar',
        theme: 'control',
        buttonAction: () => context.uiContext.cardEditor?.refreshCard()
      })
    );

    // You can provide custom classes.
    sideForm.classNames.add('side-form-example');

    // The side from can be stylized via theme.
    sideForm.theme.appendFragment({
      card: {
        'side-form': {
          background: '$dustyBlue'
        }
      }
    });

    // The side form can be positioned automatically or at a fixed position.
    sideForm.position = 'auto';

    // Or can be hidden by setting the position to "hidden".
    // sideForm.position = 'hidden';

    // Dimensions can be controlled via *height and *width properties.
    // Properties in "horizontal" refer to the configuration where the side form
    // is moved above the card. This happens when there is not enough space for
    // the form to stay in its default position, or when the position is set to "top".
    // Properties in "vertical" refer to the configuration where the side form
    // is on the left side of the card.
    sideForm.horizontal.maxHeight = '300';
    sideForm.horizontal.maxWidth = '50%';

    // Each orientation can be assigned individual classes.
    sideForm.horizontal.classNames.add('horizontal');

    // Assign your side form.
    context.model.sideForm = sideForm;
  }
}
