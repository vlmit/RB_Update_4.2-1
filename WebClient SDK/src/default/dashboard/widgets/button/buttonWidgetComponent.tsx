import { useCallback, useMemo } from 'react';
import { observer } from 'mobx-react-lite';
import styled from 'styled-components';
import { useLocalize } from '@tessa/ui';
import { ColorPickerHelper } from 'ui/colorPicker/colorPickerHelper';
import { Icon } from 'ui/icon/icon';
import { ButtonWidget } from './buttonWidget';
import { ButtonWidgetSettings } from './buttonWidgetSettings';
import './buttonStyle.scss';

type ButtonWidgetComponentProps = {
  viewModel: ButtonWidget<ButtonWidgetSettings>;
};

export const StyledButton = styled.div<{ mainColor?: string; captionColor?: string }>`
  background: ${props => props.mainColor};
  color: ${props => props.captionColor};
  &:hover {
    background: ${props =>
      `hsl(from ${props.mainColor ?? 'var(--dashboard-widget-button-background)'} calc(h + 10) s l / alpha)`};
  }
`;

export const ButtonWidgetComponent = observer<ButtonWidgetComponentProps>(
  function ButtonWidgetComponent({ viewModel }) {
    const color = useMemo(() => {
      return viewModel.color
        ? ColorPickerHelper.toRGBAhexFromARGBhex(
            ColorPickerHelper.toARGBhexFromDecimal(viewModel.color)
          )
        : undefined;
    }, [viewModel.color]);
    const captionColor = useMemo(() => {
      return viewModel.captionColor
        ? ColorPickerHelper.toRGBAhexFromARGBhex(
            ColorPickerHelper.toARGBhexFromDecimal(viewModel.captionColor)
          )
        : undefined;
    }, [viewModel.captionColor]);

    const handleClick = useCallback(() => {
      viewModel.handleClick();
    }, [viewModel]);

    const caption = useLocalize(viewModel.caption);

    return (
      <StyledButton
        className="button-widget-container"
        mainColor={color}
        captionColor={captionColor}
        onClick={handleClick}
        title={viewModel.captionHidden ? (viewModel.caption ?? undefined) : undefined}
      >
        {viewModel.icon && <Icon icon={viewModel.icon} size="l" />}
        {viewModel.caption && !viewModel.captionHidden && <span title={caption}>{caption}</span>}
      </StyledButton>
    );
  }
);
