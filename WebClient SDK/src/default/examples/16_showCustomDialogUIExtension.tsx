import { useRef, useCallback } from 'react';
import { observable, computed, action } from 'mobx';
import { observer } from 'mobx-react-lite';
import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ButtonViewModel } from 'tessa/ui/cards/controls';
import { Dialog, DialogContainer, DialogContent } from 'ui';
import { showViewModelDialog } from 'tessa/ui';
import { Button } from 'ui/button/button';

/**
 * При клике на контрол кнопки показываем кастомный диалог.
 *
 * Результат работы расширения:
 * При клике на кнопку "Показать диалог" показываем кастомный диалог,
 * содержащий текстовое поле, а также кнопки "ОК" и "Закрыть". При нажатии на кнопку "OK"
 * в консоли отображается содержимое текстового поля.
 */
@extension()
export class ShowCustomDialogUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся получить контрол "Показать диалог"
    const button = context.model.controls.get('ShowDialogTypeForm') as ButtonViewModel;
    if (!button) {
      return;
    }

    // будем открывать кастомный диалог
    button.onClick = ShowCustomDialogUIExtension.showCustomDialog;
  }

  private static async showCustomDialog() {
    // делаем viewModel для нашего диалога
    const viewModel = new CustomDialogViewModel('Hello World!');
    // вызываем диалоговое окно
    await showViewModelDialog(viewModel, CustomDialog);
    // все изменения сохранены во viewModel
    console.log(viewModel.text);
  }
}

class CustomDialogViewModel {
  constructor(text: string) {
    this._text = text || '';
  }

  @observable
  private _text: string;

  @computed
  public get text(): string {
    return this._text;
  }

  @action.bound
  public changeText(newText: string): void {
    this._text = newText || '';
  }
}

interface CustomDialogProps {
  viewModel: CustomDialogViewModel;
  onClose: () => void;
}

const CustomDialog = observer<CustomDialogProps>(function CustomDialog({ viewModel, onClose }) {
  const inputRef = useRef<HTMLInputElement>(null);

  const handleChange = useCallback(() => {
    if (inputRef && inputRef.current) {
      const name = inputRef.current.value;
      viewModel.changeText(name.trim());
    }
    onClose();
  }, [viewModel, onClose]);

  const handleCloseForm = useCallback(() => {
    onClose();
  }, [onClose]);

  return (
    <Dialog
      isOpened={true}
      noPortal={true}
      autoSizeWidth={true}
      autoSizeHeight={true}
      onCloseRequest={handleCloseForm}
      style={{
        minHeight: 'auto',
        minWidth: '300px',
        borderRadius: '4px',
        outline: 'none',
        padding: '0.5rem',
        border: '1px solid #ccc',
        background: '#fff'
      }}
      showChrome={true}
    >
      <DialogContainer>
        <DialogContent>
          <div>
            <input
              type="text"
              className="form-control"
              ref={inputRef}
              defaultValue={viewModel.text}
            />
          </div>
          <div>
            <Button caption="$UI_Common_OK" onClick={handleChange} type="normal" theme="primary" />
            <Button
              caption="$UI_Common_Cancel"
              onClick={handleCloseForm}
              type="normal"
              theme="secondary"
            />
          </div>
        </DialogContent>
      </DialogContainer>
    </Dialog>
  );
});
