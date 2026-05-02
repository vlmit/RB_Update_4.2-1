import { TreeItemExtension } from 'tessa/ui/views/extensions';
import {
  IWorkplaceFilteringRule,
  CheckingResult,
  WorkplaceMetadataComponentSealed,
  WorkplaceFilteringContext
} from 'tessa/views/workplaces';
import { IStorage } from 'tessa/platform/storage';
import { StringHelper } from 'tessa/platform';
import { RequestParameter } from 'tessa/views/metadata';
import { extension } from '@tessa/application';

@extension()
export class RefSectionExtension extends TreeItemExtension implements IWorkplaceFilteringRule {
  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Workplaces.RefSectionExtension';
  }

  async evaluate(
    _metadata: WorkplaceMetadataComponentSealed,
    context: WorkplaceFilteringContext
  ): Promise<CheckingResult> {
    const settings = new TreeItemFilteringSettings(this.settingsStorage);

    const conditionEquals =
      this.settingsStorage &&
      settings.refSections.some(x => context.refSection?.some(y => StringHelper.equals(x, y))) &&
      RefSectionExtension.parametersEquals(context.parameters, settings.parameters);
    return conditionEquals ? CheckingResult.Positive : CheckingResult.Negative;
  }

  private static parametersEquals(
    contextParameters: RequestParameter[],
    settingsParameters: string[]
  ): boolean {
    if (contextParameters == null || contextParameters.length === 0) {
      return true;
    }

    return contextParameters
      .map(p => p.metadata.alias)
      .every(x => settingsParameters.some(y => StringHelper.equals(x, y)));
  }
}

class TreeItemFilteringSettings {
  constructor(storage: IStorage) {
    this.refSections = storage['RefSections'] || [];
    this.parameters = storage['Parameters'] || [];
  }

  public readonly refSections: string[];

  public readonly parameters: string[];
}
