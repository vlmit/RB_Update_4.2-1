import {
  PropertyGridHelper,
  PropertyHighlightMode,
  MultiplePropertyGrid,
  PropertyCounter
} from 'tessa/ui/propertyGrid';
import { CardHelpMode } from 'ui/uiEnums';
import { Button } from 'ui/button/buttonViewModel';
import { PropertyGridArticle } from './propertyGridArticle';
import { PropertyGridDemoView, PropertyGridMultipleDemoView } from './propertyGridDemoForm';

export class PropertyGridMiscellaneousArticle extends PropertyGridArticle {
  //#region base overrides

  protected override readonly name = 'Miscellaneous';
  protected override readonly description = `This article contains various examples of working with the property grid and its settings.`;
  protected override readonly keywords = new Set<string>(['misc', 'setting', 'config', 'option']);

  override async initialize(): Promise<void> {
    this.keywords.add('panel');
    this.panelsConfiguration();

    this.keywords.add('caption');
    this.captionsConfiguration();

    this.keywords.add('priority');
    this.prioritySetup();

    this.keywords.add('layout');
    this.layoutSettings();

    this.keywords.add('theme').add('style').add('custom');
    this.themeOptions();
  }

  //#endregion

  //#region private methods

  private panelsConfiguration(): void {
    this.addBlock({
      caption: 'Panels configuration',
      props: async () => {
        const propertyGrid = this.createPropertyGrid();
        await propertyGrid.initialize();

        propertyGrid.descriptionVisibility = false;
        propertyGrid.toolbarVisibility = true;
        propertyGrid.toolbar.alphabetic = true;
        propertyGrid.toolbar.categorized = false;
        propertyGrid.toolbar.search.value = 'property';
        propertyGrid.toolbar.buttons.add(
          Button.create({
            name: 'Open',
            theme: 'green',
            type: 'normal',
            icon: 'm-dialogue',
            tooltip: 'Open in modal',
            buttonAction: async btn => {
              try {
                btn.disabled = true;
                await PropertyGridHelper.showDialog(propertyGrid);
              } finally {
                btn.disabled = false;
              }
            }
          })
        );

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView
    });
  }

  private captionsConfiguration(): void {
    this.addBlock({
      caption: 'Captions configuration',
      props: async () => {
        const propertyGrid = this.createPropertyGrid();
        await propertyGrid.initialize();

        propertyGrid.getGroups(true).forEach((g, i) => {
          g.leftCaption = i % 2 === 0;
          g.collapsible = i % 2 !== 0;
          g.caption = g.leftCaption ? 'Left captions' : 'Up captions';
          g.caption += g.collapsible ? ' (collapsible)' : ' (noncollapsible)';
        });

        propertyGrid.getProperties(true).forEach((p, i) => {
          p.leftCaption = i % 2 === 0;
          p.required = i % 3 === 0;
          p.tooltip.visibility = i % 4 !== 0;
          p.help.visibility = p.tooltip.visibility ? CardHelpMode.ToolTip : CardHelpMode.None;
          p.caption.text += p.leftCaption ? ' (left caption)' : ' (up caption)';
        });

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView
    });
  }

  private prioritySetup(): void {
    this.addBlock({
      caption: 'Priority setup',
      props: async () => {
        const gridPriority = this.createPropertyGrid('Grid priority');
        gridPriority.disabled = false;
        gridPriority.leftCaption = true;
        gridPriority.highlightMode = PropertyHighlightMode.Select;

        const groupPriority = this.createPropertyGrid('Group priority');
        groupPriority.getGroups(true).forEach((g, i) => {
          g.disabled = i % 2 === 0;
          g.leftCaption = i % 2 !== 0;
          g.highlightMode = PropertyHighlightMode.Hover;
        });

        const propertyPriority = this.createPropertyGrid('Property priority');
        propertyPriority.getGroups(true).forEach((g, gi) => {
          g.disabled = gi % 2 === 0;
          g.leftCaption = gi % 2 !== 0;
          g.highlightMode = PropertyHighlightMode.Hover;

          g.getProperties(true).forEach((p, pi) => {
            p.disabled = pi % 2 !== 0;
            p.leftCaption = pi % 2 === 0;
            p.highlightMode = PropertyHighlightMode.Always;
          });
        });

        const multiplePropertyGrid = new MultiplePropertyGrid(undefined, [
          gridPriority,
          groupPriority,
          propertyPriority
        ]);
        await multiplePropertyGrid.initialize();

        multiplePropertyGrid.disabled = true;
        multiplePropertyGrid.leftCaption = false;
        multiplePropertyGrid.highlightMode = PropertyHighlightMode.None;

        return { viewModel: multiplePropertyGrid };
      },
      view: PropertyGridMultipleDemoView
    });
  }

  private layoutSettings(): void {
    this.addBlock({
      caption: 'Layout settings',
      props: async () => {
        const propertyGrid = this.createPropertyGrid();
        await propertyGrid.initialize();

        propertyGrid.autoColumnsCount = true;
        propertyGrid.getGroups(true).forEach((g, i) => {
          g.leftCaption = i % 2 !== 0;
          g.columnsCount = ((i % 3) + 1) as PropertyCounter;
        });

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView
    });
  }

  private themeOptions(): void {
    this.addBlock({
      caption: 'Theme options',
      props: async () => {
        const propertyGrid = this.createPropertyGrid();
        await propertyGrid.initialize();

        propertyGrid.themeLimitations = true;
        propertyGrid.getGroups(true).forEach((g, i) => {
          const index = i + 1;
          if (index !== 1) {
            g.themeLimitations = {
              noBorder: index % 2 === 0,
              noCorners: index % 3 === 0,
              noPadding: index % 4 === 0,
              noBackground: index % 5 === 0
            };
          }

          g.description = JSON.stringify(g.themeLimitations);
        });

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView
    });
  }

  //#endregion
}
