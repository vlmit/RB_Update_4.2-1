import { observer } from 'mobx-react-lite';
import {
  PropertyGridHelper,
  PropertyGridComponent,
  MultiplePropertyGrid,
  PropertyGridMultipleComponent
} from 'tessa/ui/propertyGrid';
import { Button as ButtonView } from 'ui/button/button';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { PropertyGridArticle } from './propertyGridArticle';
import { PropertyGridDemoForm } from './propertyGridDemoForm';

export class PropertyGridDisplayArticle extends PropertyGridArticle {
  //#region base overrides

  protected override readonly name = 'Display';
  protected override readonly description = `This article shows any ways to display property grid.`;
  protected override readonly keywords = new Set(['basic', 'display']);

  override async initialize(): Promise<void> {
    this.keywords.add('embedded');
    this.displaySingleNonModal();

    this.keywords.add('modal');
    this.displaySingleModal();

    this.keywords.add('multiple');
    this.displayMultipleNonModal();
    this.displayMultipleModal();
  }

  //#endregion

  //#region private methods

  private displaySingleNonModal(): void {
    this.addBlock({
      caption: 'Display single non modal',
      description: `Displaying property grid in place through explicit use of an embedded non modal component.`,
      props: async () => {
        const propertyGrid = this.createPropertyGrid();
        await propertyGrid.initialize();

        return { viewModel: propertyGrid };
      },
      view: ({ viewModel }) => (
        <PropertyGridDemoForm>
          <PropertyGridComponent viewModel={viewModel} />
        </PropertyGridDemoForm>
      ),
      code: `
~~~jsx
// 1. Create and initialize property grid
const propertyGrid = new PropertyGrid(...);
await propertyGrid.initialize();

// 2. Display property grid
<PropertyGridComponent viewModel={propertyGrid} />
~~~`
    });
  }

  private displaySingleModal(): void {
    this.addBlock({
      caption: 'Display single modal',
      description: `Displaying property grid in modal dialog window through embedded modal component and helper function.`,
      props: async () => {
        const button1 = this.createOpenDialogButton('Open modal property grid');
        const button2 = this.createOpenDialogButton('Open modal embedded property grid');

        const propertyGrid = this.createPropertyGrid();
        propertyGrid.title = 'Modal property grid';
        propertyGrid.onClose.add(() => {
          button1.active = button2.active = false;
        });
        await propertyGrid.initialize();

        button1.buttonAction = async button => {
          button.active = true;
          await PropertyGridHelper.showDialog(propertyGrid, { autoSizeHeight: true });
        };

        return { button1, button2, viewModel: propertyGrid };
      },
      view: observer(({ button1, button2, viewModel }) => (
        <>
          <PropertyGridDemoForm>
            <ButtonView viewModel={button1} />
            <ButtonView viewModel={button2} />
          </PropertyGridDemoForm>
          {button2.active && <PropertyGridComponent viewModel={viewModel} modal autoSizeHeight />}
        </>
      )),
      code: `
~~~jsx
// 1. Create and initialize property grid
const propertyGrid = new PropertyGrid(...);
await propertyGrid.initialize();

// 2. Display modal property grid
await PropertyGridHelper.showDialog(propertyGrid, { autoSizeHeight: true });

// 2. Display modal embedded property grid
<PropertyGridComponent viewModel={propertyGrid} modal autoSizeHeight autoSizeWidth />
// <PropertyGridDialog viewModel={propertyGrid} autoSizeHeight autoSizeWidth />
~~~`
    });
  }

  private displayMultipleNonModal(): void {
    this.addBlock({
      caption: 'Display multiple non modal',
      description: `Displaying multiple property grid in place through explicit use of an embedded non modal component.`,
      props: async () => {
        const multiplePropertyGrid = new MultiplePropertyGrid();
        multiplePropertyGrid.addGrid(this.createPropertyGrid('First Property Grid'), true);
        multiplePropertyGrid.addGrid(this.createPropertyGrid('Second Property Grid'), false);
        await multiplePropertyGrid.initialize();

        return { viewModel: multiplePropertyGrid };
      },
      view: ({ viewModel }) => (
        <PropertyGridDemoForm>
          <PropertyGridMultipleComponent viewModel={viewModel} />
        </PropertyGridDemoForm>
      ),
      code: `
~~~jsx
// 1. Create and initialize multiple property grid
const multiplePropertyGrid = new MultiplePropertyGrid(...);
await multiplePropertyGrid.initialize();

// 2. Display property multiple grid
<PropertyGridMultipleComponent viewModel={multiplePropertyGrid} />
~~~`
    });
  }

  private displayMultipleModal(): void {
    this.addBlock({
      caption: 'Display multiple modal',
      description: `Displaying multiple property grid in modal dialog window through embedded modal component and helper function.`,
      props: async () => {
        const button1 = this.createOpenDialogButton('Open modal property grid');
        const button2 = this.createOpenDialogButton('Open modal embedded property grid');

        const multiplePropertyGrid = new MultiplePropertyGrid({ caption: 'Modal property grid' });
        multiplePropertyGrid.addGrid(this.createPropertyGrid('First Property Grid'), true);
        multiplePropertyGrid.addGrid(this.createPropertyGrid('Second Property Grid'), false);
        multiplePropertyGrid.initialize();
        multiplePropertyGrid.onClose.add(() => {
          button1.active = button2.active = false;
        });
        await multiplePropertyGrid.initialize();

        button1.buttonAction = async button => {
          button.active = true;
          await PropertyGridHelper.showMultipleDialog(multiplePropertyGrid, {
            autoSizeHeight: true
          });
        };

        return { button1, button2, viewModel: multiplePropertyGrid };
      },
      view: observer(({ button1, button2, viewModel }) => (
        <>
          <PropertyGridDemoForm>
            <ButtonView viewModel={button1} />
            <ButtonView viewModel={button2} />
          </PropertyGridDemoForm>
          {button2.active && (
            <PropertyGridMultipleComponent viewModel={viewModel} modal autoSizeHeight />
          )}
        </>
      )),
      code: `
~~~jsx
// 1. Create and initialize multiple property grid
const multiplePropertyGrid = new MultiplePropertyGrid(...);
await multiplePropertyGrid.initialize();

// 2. Display modal multiple property grid
await PropertyGridHelper.showMultipleDialog(multiplePropertyGrid, { autoSizeHeight: true });

// 2. Display modal embedded multiple property grid
<PropertyGridMultipleComponent viewModel={multiplePropertyGrid} modal autoSizeHeight autoSizeWidth />
// <PropertyGridMultipleDialog viewModel={multiplePropertyGrid} autoSizeHeight autoSizeWidth />
~~~`
    });
  }

  private createOpenDialogButton(caption = 'Open property grid'): ButtonViewModel {
    return ButtonViewModel.create({
      caption,
      type: 'normal',
      theme: 'primary',
      buttonAction: btn => {
        btn.active = true;
      }
    });
  }

  //#endregion
}
