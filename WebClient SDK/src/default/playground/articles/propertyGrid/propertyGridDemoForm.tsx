import {
  PropertyGrid,
  PropertyGridComponent,
  MultiplePropertyGrid,
  PropertyGridMultipleComponent
} from 'tessa/ui/propertyGrid';
import { DemoForm } from 'tessa/ui/playground/chunk';

export const PropertyGridDemoForm: React.FC = ({ children }) => {
  return (
    <DemoForm
      customStyles={css => css`
        display: flex;
        flex-direction: column;
        gap: 10px;
        max-height: 500px;

        > * {
          flex: 1;
        }
      `}
    >
      {children}
    </DemoForm>
  );
};

export const PropertyGridDemoView: React.FC<{
  readonly viewModel: PropertyGrid;
  readonly modal?: boolean;
}> = ({ viewModel, modal = false }) => {
  return (
    <PropertyGridDemoForm>
      <PropertyGridComponent viewModel={viewModel} modal={modal} />
    </PropertyGridDemoForm>
  );
};

export const PropertyGridMultipleDemoView: React.FC<{
  readonly viewModel: MultiplePropertyGrid;
  readonly modal?: boolean;
}> = ({ viewModel, modal = false }) => {
  return (
    <PropertyGridDemoForm>
      <PropertyGridMultipleComponent viewModel={viewModel} modal={modal} />
    </PropertyGridDemoForm>
  );
};
