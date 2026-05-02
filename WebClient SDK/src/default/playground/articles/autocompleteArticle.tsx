import cloneDeep from 'lodash/cloneDeep';
import { Guid, StringHelper, ValidationResult, ValidationResultType } from '@tessa/core';
import { injectable } from '@tessa/application';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { Autocomplete } from 'ui/autocomplete/autocomplete';
import {
  AutocompleteViewModel,
  AutocompleteDataSource,
  IAutocompleteItem,
  IAutocompleteItemLayout
} from 'ui/autocomplete';

@injectable()
export class AutocompleteArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Autocomplete',
      description: 'Autocomplete lets users enter text with popup and advice.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Base with single column',
      description: 'Single value with single dropdown column',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(false, true, false, false, 0, 0);

        const autocomplete2 = await this.autocompleteFactory(false, true, false, false, 0, 0);
        autocomplete2.dialog.caption = 'Base with single column';
        autocomplete2.dialogAllowed = true;

        return { autocomplete1, autocomplete2 };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Base with multiple columns',
      description: 'Single value with multiple stricted dropdown columns',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(true, true, false, false, 5, 0);
        autocomplete1.toolbar.buttons.availability = 'disabled';

        const autocomplete2 = await this.autocompleteFactory(true, true, false, false, 5, 0);
        autocomplete2.toolbar.buttons.availability = 'disabled';
        autocomplete2.dialog.caption = 'Base with multiple columns';
        autocomplete2.dialogAllowed = true;

        return { autocomplete1, autocomplete2 };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Multiple non unique',
      description:
        'Multiple non unique values with multiple dropdown columns, flags and initial values',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(true, true, true, false, 0, 2);
        autocomplete1.dropdown.useCheckbox = true;

        const autocomplete2 = await this.autocompleteFactory(true, true, true, false, 0, 2);
        autocomplete2.dialog.caption = 'Multiple non unique';
        autocomplete2.dropdown.useCheckbox = true;
        autocomplete2.dialogAllowed = true;

        return { autocomplete1, autocomplete2 };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Multiple non unique multiline',
      description:
        'Multiple non unique values with multiple dropdown columns, flags and initial values and multiline',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(true, true, true, false, 0, 2);
        autocomplete1.dropdown.useCheckbox = true;
        autocomplete1.minRows = 2;
        autocomplete1.maxRows = 4;

        const autocomplete2 = await this.autocompleteFactory(true, true, true, false, 0, 2);
        autocomplete2.dialog.caption = 'Multiple non unique multiline';
        autocomplete2.dropdown.useCheckbox = true;
        autocomplete2.dialogAllowed = true;
        autocomplete2.minRows = 2;
        autocomplete2.maxRows = 4;

        return { autocomplete1, autocomplete2 };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Multiple unique',
      description: 'Multiple unique values with single dropdown column, flags and initial values',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(false, true, true, true, 0, 4);
        autocomplete1.dropdown.useCheckbox = true;

        const autocomplete2 = await this.autocompleteFactory(false, true, true, true, 0, 4);
        autocomplete2.dialog.caption = 'Multiple unique';
        autocomplete2.dropdown.useCheckbox = true;
        autocomplete2.dialogAllowed = true;

        return { autocomplete1, autocomplete2 };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Manual single',
      description:
        'Single value with multiple stricted dropdown columns, initial values and manual input',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(true, true, false, false, 5, 1);
        autocomplete1.toolbar.buttons.availability = 'disabled';
        autocomplete1.manualInput = true;

        const autocomplete2 = await this.autocompleteFactory(true, true, false, false, 5, 1);
        autocomplete2.toolbar.buttons.availability = 'disabled';
        autocomplete2.dialog.caption = 'Manual single';
        autocomplete2.manualInput = true;
        autocomplete2.dialogAllowed = true;

        return { autocomplete1, autocomplete2 };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Manual multiple',
      description:
        'Multiple values with multiple stricted dropdown columns, flags, initial values and manual input',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(true, true, true, false, 5, 2);
        autocomplete1.dropdown.useCheckbox = true;
        autocomplete1.manualInput = true;

        const autocomplete2 = await this.autocompleteFactory(true, true, true, false, 5, 2);
        autocomplete2.dialog.caption = 'Manual multiple';
        autocomplete2.dropdown.useCheckbox = true;
        autocomplete2.manualInput = true;
        autocomplete2.dialogAllowed = true;

        return { autocomplete1, autocomplete2 };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Editable single',
      description: 'Editable value with single dropdown column',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(false, true, false, false, 0, 0);
        autocomplete1.recordEditable = true;

        const autocomplete2 = await this.autocompleteFactory(false, true, false, false, 0, 0);
        autocomplete2.recordEditable = true;
        autocomplete2.dialogAllowed = true;
        autocomplete2.dialog.caption = 'Editable with single column';

        return {
          autocomplete1,
          autocomplete2
        };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Editable manual single',
      description: 'Editable value with single dropdown column and manual input',
      props: async () => {
        const autocomplete1 = await this.autocompleteFactory(false, true, false, false, 0, 0);
        autocomplete1.recordEditable = true;
        autocomplete1.manualInput = true;

        const autocomplete2 = await this.autocompleteFactory(false, true, false, false, 0, 0);
        autocomplete2.recordEditable = true;
        autocomplete1.manualInput = true;
        autocomplete2.dialogAllowed = true;
        autocomplete2.dialog.caption = 'Editable manual single';

        return {
          autocomplete1,
          autocomplete2
        };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Custom validation',
      description: 'Single value with custom validation through validation container',
      props: async () => {
        const addValidation = (autocomplete: AutocompleteViewModel): void => {
          autocomplete.validationContainer.add(context => {
            if (!autocomplete.records.length) {
              context.addResult(
                ValidationResult.fromText('$UI_Cards_ErrorText', ValidationResultType.Error)
              );
            } else {
              context.addResult(ValidationResult.empty);
            }
          });
        };

        const autocomplete1 = await this.autocompleteFactory(false, true, false, false, 0, 0);
        addValidation(autocomplete1);

        const autocomplete2 = await this.autocompleteFactory(false, true, false, false, 0, 0);
        autocomplete2.dialogAllowed = true;
        addValidation(autocomplete2);

        return {
          autocomplete1,
          autocomplete2
        };
      },
      view: ({ autocomplete1, autocomplete2 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <Autocomplete viewModel={autocomplete1} />
          <Autocomplete viewModel={autocomplete2} />
        </DemoForm>
      )
    });
  }

  private generateItems(useCount = true, useOrder = true): IAutocompleteItem[] {
    const numeral = ['One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine'];
    return numeral.map((name, id) => {
      const data = useCount ? (useOrder ? { Order: id, Count: name } : { Count: name }) : undefined;
      return { id: Guid.fromNumber(id), name, data };
    });
  }

  private generateLayout(useCount = true, useOrder = true): IAutocompleteItemLayout[] {
    const layout: IAutocompleteItemLayout[] = [];
    if (useOrder) {
      layout.push({ alias: 'Order', caption: '#' });
    }
    if (useCount) {
      layout.push({ alias: 'Count', caption: 'Counter' });
    }
    return layout;
  }

  private filterItems(items: IAutocompleteItem[], filter?: string | null): IAutocompleteItem[] {
    return items.filter(item => StringHelper.contains(item.name, filter));
  }

  private limitItems(items: IAutocompleteItem[], limit = 0): IAutocompleteItem[] {
    return limit > 0 ? items.slice(0, limit) : items;
  }

  private async autocompleteFactory(
    useOrder = true,
    useCount = true,
    multiple = false,
    unique = false,
    limit = 0,
    init = 0
  ): Promise<AutocompleteViewModel> {
    const items = this.generateItems(useCount, useOrder);
    const layout = this.generateLayout(useCount, useOrder);
    const dataSource = new AutocompleteDataSource(
      {
        unique,
        multiple,
        layoutFactory: async () => layout,
        itemsFactory: async (_, f) => this.limitItems(f ? this.filterItems(items, f) : items, limit)
      },
      undefined,
      init > 0 ? cloneDeep(items.slice(0, init)) : undefined
    );

    const autocomplete = new AutocompleteViewModel(dataSource);

    await autocomplete.initialize();
    autocomplete.placeholder = multiple
      ? unique
        ? 'Enter multiple unique values (one, two, three...)'
        : 'Enter multiple values (one, two, three...)'
      : 'Enter single value (one, two, three...)';

    if (limit > 0 && limit < 9) {
      autocomplete.dropdown.itemsLabel = 'Max displayed items is 5';
    }

    return autocomplete;
  }
}
