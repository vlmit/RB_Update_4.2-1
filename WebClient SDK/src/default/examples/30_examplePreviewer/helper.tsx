import React from 'react';
import { MenuAction } from 'tessa/ui/menuAction';
import { UIButton, UIButtonComponent } from 'tessa/ui/uiButton';

export const getInitialToolbarItems = (
  buttons: UIButton[]
): {
  items: React.ReactElement[];
  menuItems: Array<React.ReactElement | MenuAction>;
} => {
  const items: React.ReactElement[] = buttons.map(b => (
    <UIButtonComponent key={b.name} viewModel={b} />
  ));
  const menuItems: Array<React.ReactElement | MenuAction> = [];

  return { items, menuItems };
};
