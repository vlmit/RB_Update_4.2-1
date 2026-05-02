import { extension } from '@tessa/application';
import { IStorage, StorageHelper } from '@tessa/core';
import { ThemeFragment } from '@tessa/ui';
import { ApplicationExtension } from 'tessa/applicationExtension';
import { IApplicationExtensionMetadataContext } from 'tessa/applicationExtensionContext';
import { Themes } from 'tessa/ui/themes/themesHandler';
import Platform from 'common/platform';

@extension({ name: 'ThemesInitializationApplicationExtension' })
export class ThemesInitializationApplicationExtension extends ApplicationExtension {
  async afterMetadataReceived(context: IApplicationExtensionMetadataContext): Promise<void> {
    // логика только для дев режима
    if (process.env.NODE_ENV === 'production') {
      return;
    }

    const info = context.response?.tryGetInfo() ?? {};
    const themeFragments = StorageHelper.tryGet<IStorage | null>(info, 'Themes') ?? null;
    if (!themeFragments) {
      return;
    }

    // находим фрагменты для проектных решений и удаляем их, так как в дев режиме
    // они должны подгружаться с дев сервера
    const solutionFragmentsNames = Object.keys(themeFragments).filter(name =>
      name.endsWith('.Solution')
    );

    for (const name of solutionFragmentsNames) {
      delete themeFragments[name];
    }

    const fragments = await this.importSolutionFragments();

    for (const fragment of fragments) {
      delete fragment['Name'];
      Themes.addFragment(fragment);
    }
  }

  private async importSolutionFragments(): Promise<ThemeFragment[]> {
    const Base = Object.assign({}, (await import('./Base.Solution.json')).default);
    const Cold = Object.assign({}, (await import('./Cold.Solution.json')).default);
    const Dark = Object.assign({}, (await import('./Dark.Solution.json')).default);
    const Warm = Object.assign({}, (await import('./Warm.Solution.json')).default);
    const Light = Object.assign({}, (await import('./Light.Solution.json')).default);
    const Condensed = Object.assign({}, (await import('./Condensed.Solution.json')).default);
    const Compact = Object.assign({}, (await import('./Compact.Solution.json')).default);

    const fragments: ThemeFragment[] = [Base, Cold, Dark, Warm, Light, Condensed, Compact];

    if (Platform.isMobile()) {
      const Mobile = Object.assign({}, (await import('./Mobile.Solution.json')).default);
      fragments.push(Mobile);
    }
    // здесь можно импортировать кастомные фрагменты

    return fragments;
  }
}
