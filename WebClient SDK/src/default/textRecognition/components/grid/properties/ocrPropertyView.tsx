import { observer } from 'mobx-react-lite';
import { TextFieldView } from 'ui/textField/textFieldView';
import { PropertyCommonProps } from 'tessa/ui/propertyGrid';
import { OcrProperty } from './ocrProperty';

/** The OCR property component. */
export const OcrPropertyView = observer<PropertyCommonProps<OcrProperty>>(function OcrPropertyView({
  viewModel
}) {
  return <TextFieldView viewModel={viewModel.control} />;
});
