import React, { useEffect, useMemo, useRef } from 'react';
import { observer } from 'mobx-react-lite';
import { localize } from '@tessa/application';
import { getVisibilityStyle } from 'tessa/ui';
import { Visibility } from 'tessa/platform';
import {
  ControlProps,
  ControlCaption,
  createStyledControl
} from 'tessa/ui/cards/components/controls';
import { SliderViewModel } from './29_sliderViewModel';

const StyledInput = createStyledControl<React.AllHTMLAttributes<HTMLInputElement>>('input');

export const SliderControl = observer<ControlProps<SliderViewModel>>(function SliderControl({
  viewModel
}) {
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    viewModel.bindReactComponentRef(inputRef);
    return () => viewModel.unbindReactComponentRef();
  }, [viewModel]);

  const caption = useMemo(() => localize(viewModel.caption), [viewModel.caption]);

  const controlVisibility = viewModel.controlVisibility;
  if (controlVisibility === Visibility.Collapsed) {
    return null;
  }

  const captionVisibility =
    controlVisibility === Visibility.Hidden ? Visibility.Hidden : viewModel.captionVisibility;

  const handleChange = (e: React.SyntheticEvent<HTMLInputElement>) => {
    const target = e.target as HTMLInputElement;
    const value = target.value;

    viewModel.value = Number.parseInt(value);
  };

  // получаем фокус
  const handleFocus = () => {
    viewModel.isFocused = true;
  };

  // теряем фокус
  const handleBlur = () => {
    viewModel.isFocused = false;
  };

  return (
    <>
      <ControlCaption
        captionStyle={viewModel.captionStyle.result}
        style={getVisibilityStyle({}, captionVisibility)}
      >
        {caption}
      </ControlCaption>
      <div style={getVisibilityStyle({}, controlVisibility)}>
        <StyledInput
          ref={inputRef}
          type="range"
          step={viewModel.step}
          min={viewModel.minValue}
          max={viewModel.maxValue}
          value={viewModel.value}
          controlStyle={viewModel.controlStyle.result}
          onChange={handleChange}
          onFocus={handleFocus}
          onBlur={handleBlur}
          title={viewModel.error || viewModel.tooltip}
        />
      </div>
    </>
  );
});
