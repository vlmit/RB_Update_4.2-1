import { extension } from '@tessa/application';
import { IWorkplaceViewComponent, StandardViewComponentContentItemFactory } from 'tessa/ui/views';
import {
  ContentPlaceArea,
  ContentPlaceOrder,
  ViewButtonViewModel,
  QuickSearchBlockViewModel,
  QuickSearchViewModel,
  OpenFilterDialogButtonViewModel,
  RefreshButtonViewModel
} from 'tessa/ui/views/content';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { MenuAction } from 'tessa/ui';
import { ViewSelectViewModel } from 'tessa/ui/views/content/viewSelectViewModel';
import { reaction, runInAction } from 'mobx';

/** Пример группировки контролов, управления порядком групп и контролов внутри групп в тулбаре в заголовке представления. */
@extension({ name: 'GroupedViewToolbarExtension' })
export class GroupedViewToolbarExtension extends WorkplaceViewComponentExtension {
  getExtensionName(): string {
    return 'GroupedViewToolbarExtension';
  }

  override shouldExecute(model: IWorkplaceViewComponent): boolean {
    // Выполняется в представлении "Автомобили"
    return model.dataNodeMetadata.compositionId === '193496fb-e9a1-49a1-b9f4-8dbf73d56bd4';
  }

  override initialize(model: IWorkplaceViewComponent): void {
    // Удаляем элементы тулбара, добавленные другими расширениями
    model.contentFactories.delete('CreateCardExtension');
    model.contentFactories.delete(StandardViewComponentContentItemFactory.QuickSearch);
    model.contentFactories.delete(StandardViewComponentContentItemFactory.FilterButton);
    model.contentFactories.delete('AddTagButtonViewExtension');
    model.contentFactories.delete(StandardViewComponentContentItemFactory.RefreshButton);

    // Создаем группы
    model.groupContainer.createGroup('monthSwitcher', {
      isPermanent: true,
      dropdownWrapperType: 'normal'
    });
    model.groupContainer.createGroup('calendar', { isPermanent: true });
    model.groupContainer.createGroup('btns', { dropdownWrapperType: 'normal' });
    model.groupContainer.createGroup('quickSearch', { order: -1 });

    // Создаем контролы, указывая их принадлежность к группе.
    // Если группа не указана, для контрола будет создана своя отдельная группа
    model.contentFactories.set('arrow-left', c => {
      const vm = new ViewButtonViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.icon = 'icon-grid-left';
      vm.type = 'small';
      vm.theme = 'control';
      vm.setGroupName('monthSwitcher');

      return vm;
    });

    model.contentFactories.set('months', c => {
      const vm = new ViewButtonViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.caption = 'июнь - июль';
      vm.type = 'small';
      vm.theme = 'transparent';
      vm.setGroupName('monthSwitcher');

      return vm;
    });

    model.contentFactories.set('arrow-right', c => {
      const vm = new ViewButtonViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.icon = 'icon-grid-right';
      vm.type = 'small';
      vm.theme = 'control';
      vm.setGroupName('monthSwitcher');

      return vm;
    });

    model.contentFactories.set('today', c => {
      const vm = new ViewButtonViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.caption = 'Сегодня';
      vm.type = 'small';
      vm.theme = 'control';
      vm.setGroupName('calendar');

      return vm;
    });

    model.contentFactories.set('calendar btn', c => {
      const vm = new ViewButtonViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.icon = 'm-date';
      vm.type = 'small';
      vm.theme = 'control';
      vm.setGroupName('calendar');

      return vm;
    });

    model.contentFactories.set('select', c => {
      const vm = new ViewSelectViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.selectProps.values = [
        MenuAction.create({
          name: 'Мои события',
          caption: 'Мои события',
          action: () => {
            runInAction(() => (vm.selectProps.value = 'Мои события'));
          }
        }),
        MenuAction.create({
          name: 'test2',
          caption: 'test2',
          action: () => {
            runInAction(() => (vm.selectProps.value = 'test2'));
          }
        }),
        MenuAction.create({
          name: 'test3',
          caption: 'test3',
          action: () => {
            runInAction(() => (vm.selectProps.value = 'test3'));
          }
        })
      ];
      vm.selectProps.value = vm.selectProps.values[0].caption!;
      vm.selectProps.selectType = 'compact';
      vm.selectProps.widthType = 'static';
      vm.setGroupName('btns');

      vm.disposeList.add(
        reaction(
          () => vm.selectProps.value,
          () => vm.closeParentContainer()
        )
      );

      return vm;
    });

    model.contentFactories.set('QuickSearchBlockViewModel', c => {
      if (c.viewMetadata?.quickSearchParam) {
        const vm = new QuickSearchBlockViewModel(
          c,
          new QuickSearchViewModel(c),
          new OpenFilterDialogButtonViewModel(c),
          ContentPlaceArea.ToolBarPanel
        );

        vm.setGroupName('quickSearch');

        vm.quickSearchViewModel.onSearch.add(() => vm.closeParentContainer());

        return vm;
      }
      return null;
    });

    model.contentFactories.set('calendar btn2', c => {
      const vm = new ViewButtonViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.icon = 'm-date';
      vm.type = 'small';
      vm.theme = 'control';
      vm.setGroupName('btns');

      return vm;
    });

    model.contentFactories.set('filter', c => {
      const vm = new OpenFilterDialogButtonViewModel(
        c,
        ContentPlaceArea.ToolBarPanel,
        ContentPlaceOrder.AfterAll
      );

      vm.setGroupName('btns');

      return vm;
    });

    model.contentFactories.set('add event', c => {
      const vm = new ViewButtonViewModel(
        c,
        ContentPlaceArea.ToolBarPanel,
        ContentPlaceOrder.AfterAll
      );

      vm.caption = 'Добавить событие';
      vm.icon = 'm-plus';
      vm.captionPosition = 'after';
      vm.type = 'small';
      vm.theme = 'control';
      vm.closeParentContainerOnClick = false;
      vm.setGroupName('btns');
      vm.onClick = () => console.log(vm.groupName);

      return vm;
    });

    model.contentFactories.set('refresh', c => {
      const vm = new RefreshButtonViewModel(c, ContentPlaceArea.ToolBarPanel);
      vm.setGroupName('btns');

      return vm;
    });
  }

  override initialized(model: IWorkplaceViewComponent): void {
    const group = model.groupContainer.getGroupByContent('refresh');
    if (group) {
      // Изменяем порядок группы, к которой принадлежит контрол с ключом 'refresh'
      group.order = -10000;
    }

    // Переносим контрол с ключом 'refresh' в позицию 0 в пределах своей группы.
    model.groupContainer.moveContentToIndex('refresh', 0);

    // Переносим контрол с ключом 'filter' в позицию 0 в заведомо существующую группу с ключом 'calendar'.
    model.groupContainer.moveContentToGroup('filter', 'calendar', 0);

    // Переносим контрол с ключом 'add event' в позицию 0 в заведомо несуществующую группу
    // с ключом 'new nonexistent group', применяя для новой группы указанные настройки.
    model.groupContainer.moveContentToGroup('add event', 'new nonexistent group', 0, {
      isPermanent: true,
      order: 10000
    });
  }
}
