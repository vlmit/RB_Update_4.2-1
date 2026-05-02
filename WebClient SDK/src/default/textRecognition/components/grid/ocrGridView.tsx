import { observer } from 'mobx-react-lite';
import { PropertyGridComponent } from 'tessa/ui/propertyGrid';
import { OcrGridViewModel } from './ocrGridViewModel';
import './ocrGridStyle.scss';

/** The OCR property grid component. */
export const OcrGridView = observer<{ viewModel: OcrGridViewModel }>(function OcrGridView({
  viewModel
}) {
  return (
    <div className="ocr-property-grid">
      <PropertyGridComponent viewModel={viewModel.control} />
    </div>
  );
});
