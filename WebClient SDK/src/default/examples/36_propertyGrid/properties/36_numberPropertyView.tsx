import { ChangeEvent, KeyboardEvent, useCallback } from 'react';
import { observer } from 'mobx-react-lite';
import ControlContainer from 'ui/controlContainer/controlContainer';
import { PropertyCommonProps } from 'tessa/ui/propertyGrid';
import { NumberProperty } from './36_numberPropertyViewModel';
import { TextField } from 'ui';

/** Компонент свойства "Число". */
export const NumberPropertyComponent = observer<PropertyCommonProps<NumberProperty>>(
  function NumberPropertyComponent({ viewModel }) {
    const handleKeyDown = useCallback((e: KeyboardEvent<HTMLInputElement>) => {
      const numberKeys = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
      const manageKeys = ['Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab'];
      if (!numberKeys.includes(e.key) && !manageKeys.includes(e.key)) {
        e.preventDefault();
      }
    }, []);

    const handleChange = useCallback(
      (e: ChangeEvent<HTMLInputElement>) => {
        const value = e.currentTarget.valueAsNumber;
        if (value >= viewModel.minValue && value <= viewModel.maxValue) {
          viewModel.value = e.currentTarget.valueAsNumber;
        }
      },
      [viewModel]
    );

    return (
      <ControlContainer isReadOnly={viewModel.disabled}>
        <TextField
          type="number"
          value={viewModel.value}
          min={viewModel.minValue}
          max={viewModel.maxValue}
          disabled={viewModel.disabled}
          onKeyDown={handleKeyDown}
          onChange={handleChange}
        />
      </ControlContainer>
    );
  }
);
