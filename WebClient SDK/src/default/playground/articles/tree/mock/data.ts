import { FieldType } from '@tessa/core';
import { OrderableTreeDataSource, TreeDataSource } from 'ui';
import { GridViewModel } from 'ui/grid';

export const nodes = [
  { name: 'Documents', caption: 'Documents', parent: null, icon: 'icon-Int787' },
  { name: 'Work', caption: 'Work', parent: 'Documents', icon: 'm-briefcase' },
  { name: 'School', caption: 'School', parent: 'Documents', icon: 'icon-Int962' },
  { name: 'Home', caption: 'Home', parent: 'Documents', icon: 'icon-Int5' },
  { name: 'About', caption: 'About', parent: 'Home', icon: 'icon-thin-228' },
  { name: 'Contacts', caption: 'Contacts', parent: 'About', icon: 'icon-thin-327' },
  { name: 'Events', caption: 'Events', parent: null, icon: 'm-date' },
  { name: 'Meeting', caption: 'Meeting', parent: 'Events', icon: 'icon-Int967' },
  {
    name: 'Product Launch',
    caption: 'Product Launch',
    parent: 'Events',
    icon: 'icon-thin-273'
  },
  { name: 'Movies', caption: 'Movies', parent: null, icon: 'icon-thin-374' },
  {
    name: 'Al Chipalino',
    caption: 'Al Chipalino',
    parent: 'Movies',
    icon: 'icon-thin-201'
  },
  {
    name: 'Anne Hathaway',
    caption: 'Anne Hathaway',
    parent: 'Movies',
    icon: 'icon-thin-201'
  }
];

export const dataSource = new TreeDataSource();
export const orderableDataSource = new OrderableTreeDataSource();

dataSource.addItems(...nodes);
orderableDataSource.addItems(...nodes);

export async function createTable(): Promise<GridViewModel> {
  const gridViewModel = new GridViewModel();
  gridViewModel.columnsMetadata.push(
    {
      id: 'CarID',
      caption: 'ID',
      dataSourceKey: 'CarID',
      dataType: FieldType.Int
    },
    {
      id: 'CarName',
      caption: 'Name',
      dataSourceKey: 'CarName',
      dataType: FieldType.String
    }
  );

  gridViewModel.extensionContainer.addHooks({
    rowInitialized: context => {
      context.row.draggable = true;
      context.row.handlersContainer.onDragStart.add(event => {
        event.dataTransfer.setData('data', context.row.id);
      });
    }
  });

  await gridViewModel.initialize();

  [
    { CarID: 1, CarName: 'Lada' },
    { CarID: 2, CarName: 'Cherry' },
    { CarID: 3, CarName: 'Geely' },
    { CarID: 4, CarName: 'Haval' },
    { CarID: 5, CarName: 'Li xiang' }
  ].forEach(x => gridViewModel.addRow(x));

  return gridViewModel;
}
