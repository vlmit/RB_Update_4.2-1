import { observable, runInAction } from 'mobx';
import { MenuAction, UIButton } from 'tessa/ui';
import { IListGroup, IListItem, ListItem } from 'ui/list';

export function items(): IListItem[] {
  return observable([
    { name: 'Work', caption: 'Work', icon: 'm-briefcase' },
    { name: 'School', caption: 'School', icon: 'm-prtmp' },
    { name: 'Home', caption: 'Home', icon: 'm-building' },
    { name: 'Contacts', caption: 'Contacts', icon: 'm-journal' },
    { name: 'Meeting', caption: 'Meeting', icon: 'm-roles' },
    { name: 'Product Launch', caption: 'Product Launch', icon: 'm-like' },
    { name: 'Al Cipollino', caption: 'Al Cipollino', icon: 'm-employees' },
    {
      name: 'Anne Hathaway',
      caption: 'Anne Hathaway',
      icon: 'm-employees',
      type: 'layout-b',
      description: `Anne Jacqueline Hathaway (born November 12, 1982) is an American actress. Her accolades include an Academy Award, a British Academy Film Award, a Golden Globe Award, and a Primetime Emmy Award. Her films have grossed over $6.8 billion worldwide, and she appeared on the Forbes Celebrity 100 list in 2009. She was among the world's highest-paid actresses in 2015.
Hathaway performed in several plays in high school. As a teenager, she was cast in the television series Get Real (1999–2000) and made her breakthrough by playing the lead role in the Disney comedy The Princess Diaries (2001). After starring in a string of family films, including Ella Enchanted (2004), Hathaway made a transition to mature roles with the 2005 drama Brokeback Mountain. The comedy-drama The Devil Wears Prada (2006), in which she played an assistant to a fashion magazine editor, was her biggest commercial success to that point. She played a recovering addict in the drama Rachel Getting Married (2008), which earned her a nomination for the Academy Award for Best Actress.`
    }
  ]);
}

export function groups(): IListGroup[] {
  return observable([
    {
      name: 'Documents',
      caption: 'Documents',
      children: ['Work', 'School', 'Home'],
      icon: 'm-doc'
    },
    { name: 'Events', caption: 'Events', children: ['Meeting', 'Product Launch'] },
    {
      name: 'Home',
      caption: 'Home',
      children: [{ name: 'About', caption: 'About', children: ['Contacts'] }]
    },
    {
      name: 'Movies',
      caption: 'Movies',
      children: ['Al Cipollino', 'Anne Hathaway']
    }
  ]);
}

export function types(): ListItem[] {
  return [
    {
      name: 'a',
      caption: 'Item type "layout-a"',
      description: 'This item type is used by default.',
      icon: 'm-chat',
      type: 'layout-a'
    },
    {
      name: 'b',
      caption: 'Item type "layout-b"',
      description: 'A variance of "layout-a".',
      icon: 'm-chat',
      type: 'layout-b'
    },
    {
      name: 'c',
      type: 'button',
      button: UIButton.create({
        name: 'demo-button',
        caption: 'Item type "button"',
        type: 'normal',
        theme: 'primary',
        stretch: true
      })
    }
  ];
}

export function actions(): ListItem[] {
  const menu = [
    MenuAction.create({ name: 'select', caption: 'Select', icon: 'm-check-mark' }),
    MenuAction.create({ name: 'delete', caption: 'Delete', icon: 'm-trash' })
  ];

  const buttons = [
    UIButton.create({ name: 'select', icon: 'm-check-mark', type: 'small', theme: 'control' }),
    UIButton.create({ name: 'delete', icon: 'm-trash', type: 'small', theme: 'red' })
  ];

  return [
    {
      name: 'a',
      actions: menu,
      caption: 'Item with actions',
      description:
        'For item types "layout-a" and "layout-b", actions are available through a spread button at the end.',
      type: 'layout-b'
    },
    {
      name: 'b',
      actions: buttons,
      caption: 'Item with buttons',
      description: 'If an array of UIButtons is provided — actions will be displayed as buttons.',
      type: 'layout-b'
    }
  ];
}

export function selection(): ListItem[] {
  const e: ListItem = observable({
    name: 'e',
    caption: 'Custom selection logic',
    description: 'This item uses a button provided via action property to control selection state.',
    selectionArea: 'none',
    isSelected: false,
    type: 'layout-b'
  });

  e.actions = [
    UIButton.create({
      name: 'selection',
      caption: 'select',
      type: 'small',
      theme: 'yellow',
      buttonAction: (b, event) => {
        event?.preventDefault();
        runInAction(() => {
          e.isSelected = !e.isSelected;
          b.caption = e.isSelected ? 'deselect' : 'select';
        });
      }
    })
  ];

  return observable([
    {
      name: 'a',
      caption: 'Non-selectable item',
      selectionArea: 'none',
      type: 'layout-b'
    },
    {
      name: 'b',
      caption: 'Checkbox selection',
      description: 'This item can only be selected via checkbox.',
      selectionArea: 'checkbox',
      type: 'layout-b'
    },
    {
      name: 'c',
      caption: 'Body click selection',
      description: 'This item can be selected via click on any part of it.',
      selectionArea: 'body',
      type: 'layout-b'
    },
    {
      name: 'd',
      caption: 'Combination of checkbox and body',
      description: 'This item combines checkbox and body selection.',
      selectionArea: 'combined',
      type: 'layout-b'
    },
    e
  ]);
}

export function drag(): ListItem[] {
  return observable([
    {
      name: 'a',
      caption: 'Non-draggable item',
      drag: 'none',
      type: 'layout-b'
    },
    {
      name: 'b',
      caption: 'Body',
      description: 'This item can be dragged by any part.',
      drag: 'body',
      type: 'layout-b'
    },
    {
      name: 'c',
      caption: 'Handle',
      description: 'This item can be dragged only by handle on the right side.',
      drag: 'handle',
      type: 'layout-b'
    }
  ]);
}

export function groupsMix(): IListGroup[] {
  return observable([
    {
      name: 'Documents',
      caption: 'Documents',
      children: ['Work', 'School', 'Home'],
      type: 'virtual',
      icon: 'm-doc'
    },
    { name: 'Events', caption: 'Events', children: ['Meeting', 'Product Launch'] },
    {
      name: 'Home',
      caption: 'Home',
      children: [{ name: 'About', caption: 'About', children: ['Contacts'] }]
    },
    {
      name: 'Movies',
      caption: 'Movies',
      children: ['Al Cipollino', 'Anne Hathaway'],
      type: 'virtual'
    },
    {
      name: 'Everything',
      caption: 'Everything',
      isExpanded: false,
      children: [
        'Work',
        'School',
        'Home',
        'Contacts',
        'Meeting',
        'Product Launch',
        'Al Cipollino',
        'Anne Hathaway'
      ]
    }
  ]);
}

export function groupsDirection(): IListGroup[] {
  return observable([
    {
      name: 'h1',
      caption: 'Horizontal group',
      children: ['Work', 'School', 'Home'],
      orientation: 'horizontal'
    },
    {
      name: 'h2',
      caption: 'Horizontal group with nested vertical groups',
      orientation: 'horizontal',
      children: [
        {
          name: 'v1',
          children: ['Work', 'School', 'Home'],
          ignoreDepth: true,
          type: 'virtual'
        },
        {
          name: 'v2',
          children: ['Contacts', 'Meeting', 'Product Launch'],
          ignoreDepth: true,
          type: 'virtual',
          stretch: true
        },
        {
          name: 'v3',
          children: ['Work', 'School', 'Home'],
          ignoreDepth: true,
          type: 'virtual'
        }
      ]
    }
  ]);
}

export function groupsPresets(): IListGroup[] {
  return observable([
    {
      name: 'g1',
      caption: 'Global presets',
      children: ['Work', 'School', 'Home']
    },
    {
      name: 'g2',
      caption: 'Drag preset override',
      children: ['Contacts', 'Meeting', 'Product Launch'],
      drop: 'block',
      presets: { item: { drag: 'none', drop: 'block' } }
    },
    {
      name: 'g3',
      caption: 'Selection preset override',
      children: ['Al Cipollino', 'Anne Hathaway'],
      presets: { item: { selectionArea: 'none' } }
    }
  ]);
}

export function treeItems(): ListItem[] {
  const mockItems = items();
  mockItems.forEach(i => {
    i.type = 'tree-item';
    i.actions = [
      MenuAction.create({ name: 'select', caption: 'Select', icon: 'm-check-mark' }),
      MenuAction.create({ name: 'delete', caption: 'Delete', icon: 'm-trash' })
    ];
  });
  mockItems.push({ name: 'image-item', type: 'image' });
  return mockItems;
}

export function treeGroups(): IListGroup[] {
  const mockGroups = groups();
  mockGroups.push(
    observable({
      name: 'image-group',
      children: ['image-item'],
      caption: 'Images',
      isExpanded: false
    })
  );
  function setTypeRecursively(group: IListGroup) {
    group.type = 'tree-group';
    group.children.forEach(c => typeof c !== 'string' && setTypeRecursively(c));
  }
  mockGroups.forEach(g => setTypeRecursively(g));
  return mockGroups;
}
