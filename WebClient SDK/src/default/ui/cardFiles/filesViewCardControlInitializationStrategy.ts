import { FilesViewMetadata } from './filesViewMetadata';
import { CardFilesDataProvider } from './cardFilesDataProvider';
import {
  ViewControlInitializationStrategy,
  ViewControlInitializationContext,
  FileListViewModel
} from 'tessa/ui/cards/controls';
import { tryGetFromInfo } from 'tessa/ui';
import { IViewMetadata, ViewMetadataSealed } from 'tessa/views/metadata';

export class FilesViewCardControlInitializationStrategy extends ViewControlInitializationStrategy {
  initializeTable(context: ViewControlInitializationContext): void {
    super.initializeTable(context);

    context.controlViewModel.firstRowSelection = false;
  }

  initializeMetadata(context: ViewControlInitializationContext): void {
    context.controlViewModel.viewMetadata = this.createViewMetadata(context);
  }

  initializeDataProvider(context: ViewControlInitializationContext): void {
    if (!context.model) {
      return;
    }

    const fileControl = tryGetFromInfo<FileListViewModel | null>(
      context.model.info,
      context.controlViewModel.name ?? '',
      null
    );
    if (fileControl) {
      context.controlViewModel.dataProvider = this.createDataProvider(
        context,
        context.controlViewModel.viewMetadata!,
        fileControl
      );
    }
  }

  initializePaging(_context: ViewControlInitializationContext): void {
    // Не запускать базовую реализацию.
    // Представление генерируемое, настройки пагинации берутся из настроек создания файлового контрола,
    // которые заполняются в FilesViewGeneratorBaseUIExtension на основании настроек, установленных для расширения.
  }

  /**
   * Create view metadata for files view.
   * @param context initialization context
   * @returns view metadata
   */
  protected createViewMetadata(_context: ViewControlInitializationContext): IViewMetadata {
    return FilesViewMetadata.create();
  }

  /**
   * Create data provider for files view.
   * @param context initialization context
   * @param viewMetadata metadata of source view
   * @param fileControl target view model
   * @returns data provider
   */
  protected createDataProvider(
    _context: ViewControlInitializationContext,
    viewMetadata: ViewMetadataSealed,
    fileControl: FileListViewModel
  ): CardFilesDataProvider {
    return new CardFilesDataProvider(viewMetadata, fileControl);
  }
}
