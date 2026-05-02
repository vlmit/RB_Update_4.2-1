import * as React from 'react';
import { observer } from 'mobx-react';
import { ControlProps } from 'tessa/ui/cards/components/controls';
import { Slider } from 'ui/slider';
import ControlContainer from 'ui/controlContainer';
import { OcrSliderViewModel as OcrSliderViewModel } from './ocrSliderViewModel';

/** Контрол ползунка для ввода вещественных значений. */
@observer
export class OcrSlider extends React.Component<ControlProps<OcrSliderViewModel>> {
  //#region fields

  private readonly _inputRef = React.createRef<HTMLInputElement>();

  //#endregion

  //#region react

  public componentDidMount(): void {
    this.props.viewModel.bindReactComponentRef(this._inputRef);
  }

  public componentWillUnmount(): void {
    this.props.viewModel.unbindReactComponentRef();
  }

  public componentDidUpdate(prevProps: ControlProps<OcrSliderViewModel>): void {
    if (prevProps.viewModel !== this.props.viewModel) {
      this.props.viewModel.bindReactComponentRef(this._inputRef);
    }
  }

  public render(): React.ReactElement {
    const { viewModel } = this.props;

    return (
      <ControlContainer viewModel={viewModel.controlContainer}>
        <Slider
          ref={this._inputRef}
          step={viewModel.step}
          min={viewModel.minValue}
          max={viewModel.maxValue}
          value={viewModel.value}
          sliderSettings={viewModel.sliderSettings}
          displayValues={viewModel.displayValues}
          roundValues={viewModel.roundValues}
          title={viewModel.tooltip}
          onChange={this.handleChange}
          onFocus={this.handleFocus}
          onBlur={this.handleBlur}
        />
      </ControlContainer>
    );
  }

  //#endregion

  //#region methods

  /**
   * Выполняет установку фокуса на элементе.
   * @param options Параметры установки фокуса.
   */
  public focus(options?: FocusOptions): void {
    if (this._inputRef.current) {
      this._inputRef.current.focus(options);
    }
  }

  //#endregion

  //#region handlers

  private handleChange = (e: React.SyntheticEvent<HTMLInputElement>) => {
    const { viewModel } = this.props;

    const target = e.target as HTMLInputElement;
    const value = target.value;

    viewModel.value = Number.parseFloat(value);
  };

  private handleFocus = () => {
    const { viewModel } = this.props;
    viewModel.isFocused = true;
  };

  private handleBlur = () => {
    const { viewModel } = this.props;
    viewModel.isFocused = false;
  };

  //#endregion
}
