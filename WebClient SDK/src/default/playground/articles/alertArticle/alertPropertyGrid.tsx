import { useEffect, useState } from 'react';
import { observable, reaction } from 'mobx';
import {
  AlertPositionPoint,
  AlertSize,
  AlertType,
  AlertViewModel,
  AlertPosition,
  AlertPositionProps
} from 'tessa/ui/alerts';
import {
  PropertyGrid,
  PropertyGridBuilder,
  PropertyGridComponent,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import { AlertManager } from 'tessa/ui/alerts/alertManager';

export function AlertArticlePropertyGrid({
  alert,
  recreate
}: {
  alert: AlertViewModel;
  recreate: () => Promise<void>;
}): React.ReactElement | null {
  const [propertyGrid, setPropertyGrid] = useState<PropertyGrid>();

  useEffect(() => {
    (async () => {
      if (propertyGrid) {
        propertyGrid.dispose();
      }

      const newPropertyGrid = createAlertPropertyGrid(alert, recreate);
      newPropertyGrid.toolbarVisibility = false;
      await newPropertyGrid.initialize();
      setPropertyGrid(newPropertyGrid);
    })();

    return () => propertyGrid?.dispose();
  }, [alert]);

  if (!propertyGrid || !propertyGrid.isInitialized) {
    return null;
  }
  return <PropertyGridComponent modal={false} viewModel={propertyGrid} />;
}

function createAlertPropertyGrid(
  alert: AlertViewModel,
  recreate: () => Promise<void>
): PropertyGrid {
  const data = observable.object({
    text: alert.text,
    type: alert.type,
    icon: alert.icon,
    size: alert.size,
    position: alert.effectivePosition.point,
    appearance: alert.effectivePosition.appearsFrom,
    offsetX: alert.effectivePosition.offset.left,
    offsetY: alert.effectivePosition.offset.top,
    closeable: alert.closeable
  });
  const propertyGridData = new PropertyGridDataProvider(data);

  const propertyGrid = PropertyGridBuilder.create(propertyGridData)
    .startGroup({ caption: 'Properties', collapsible: false })
    .addTextProperty({
      data: propertyGridData,
      alias: 'text',
      caption: 'Text'
    })
    .addSelectorProperty({
      data: propertyGridData,
      alias: 'type',
      caption: 'Type',
      items: alertTypes
    })
    .addTextProperty({
      data: propertyGridData,
      alias: 'icon',
      caption: 'Icon'
    })
    .addSelectorProperty({
      data: propertyGridData,
      alias: 'size',
      caption: 'Size',
      items: alertSizes
    })
    .addSelectorProperty({
      data: propertyGridData,
      alias: 'position',
      caption: 'Position',
      items: alertPositions,
      tooltip: 'Dashed area represents the viewport.'
    })
    .addSelectorProperty({
      data: propertyGridData,
      alias: 'appearance',
      caption: 'Appearance direction',
      items: alertAppearanceDirections
    })
    .addTextProperty({
      data: propertyGridData,
      alias: 'offsetY',
      caption: 'Vertical offset',
      tooltip:
        "Top offset takes precedence over bottom. Accepts any units 'transform' accepts, e.g. 10px, -20%, 0."
    })
    .addTextProperty({
      data: propertyGridData,
      alias: 'offsetX',
      caption: 'Horizontal offset',
      tooltip:
        "Left offset takes precedence over right. Accepts any units 'transform' accepts, e.g. 10px, -20%, 0."
    })
    .addBooleanProperty({
      data: propertyGridData,
      alias: 'closeable',
      caption: 'Closeable'
    })
    .onGridInitialized(grid => {
      grid.disposeList.add(
        reaction(
          () => [
            data.text,
            data.type,
            data.size,
            data.position,
            data.offsetX,
            data.offsetY,
            data.closeable
          ],
          () => {
            // general properties reaction
            if (data.text) {
              alert.text = data.text;
            }

            if (data.type) {
              alert.type = data.type;
              data.icon = AlertManager.instance.icons[data.type];
            }

            if (data.size) {
              alert.size = data.size;
            }

            alert.position = alertPositionSettings(alert.position, {
              point: data.position,
              offset: {
                top: data.offsetY ?? '0',
                left: data.offsetX ?? '0'
              }
            });
            alert.closeable = data.closeable;
          }
        ),
        reaction(
          () => data.icon,
          icon => {
            // necessary due to the fact icon may change with type as well as alone
            if (icon) {
              alert.icon = icon;
            }
          }
        ),
        reaction(
          () => data.appearance,
          appearance => {
            // necessary because of the need to recreate the alert when this changes
            alert.position = alertPositionSettings(alert.position, {
              appearsFrom: appearance
            });
            recreate();
          }
        )
      );
    })
    .build();

  return propertyGrid;
}

const alertTypes = new Map([
  [AlertType.Success, AlertType.Success],
  [AlertType.Error, AlertType.Error],
  [AlertType.Warning, AlertType.Warning],
  [AlertType.Info, AlertType.Info]
]);

const alertSizes = new Map([
  [AlertSize.Small, AlertSize.Small],
  [AlertSize.Standard, AlertSize.Standard],
  [AlertSize.Large, AlertSize.Large]
]);

const alertPositions = new Map([
  [AlertPositionPoint.TopLeft, AlertPositionPoint.TopLeft],
  [AlertPositionPoint.TopCenter, AlertPositionPoint.TopCenter],
  [AlertPositionPoint.TopRight, AlertPositionPoint.TopRight],
  [AlertPositionPoint.CenterLeft, AlertPositionPoint.CenterLeft],
  [AlertPositionPoint.Center, AlertPositionPoint.Center],
  [AlertPositionPoint.CenterRight, AlertPositionPoint.CenterRight],
  [AlertPositionPoint.BottomLeft, AlertPositionPoint.BottomLeft],
  [AlertPositionPoint.BottomCenter, AlertPositionPoint.BottomCenter],
  [AlertPositionPoint.BottomRight, AlertPositionPoint.BottomRight]
]);

const alertAppearanceDirections = new Map([
  ['top', 'top'],
  ['right', 'right'],
  ['bottom', 'bottom'],
  ['left', 'left']
]);

const alertPositionSettings = (
  currentSettings: AlertPosition | null,
  newSettings: AlertPositionProps
): AlertPosition => {
  if (typeof currentSettings === 'function') {
    return {
      ...currentSettings(1000),
      ...newSettings
    };
  }
  return {
    point: currentSettings?.point,
    offset: currentSettings?.offset,
    appearsFrom: currentSettings?.appearsFrom,
    ...newSettings
  };
};
