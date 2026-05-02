import { ExtensionBundleOrder } from '@tessa/application';
import { Application } from 'tessa/application';
import { ThemesRegistrator } from './themes/themesRegistrator';

Application.instance.registerBundle({
  name: 'Tessa.Extensions.Solution.js',
  buildTime: process.env.BUILD_TIME!,
  order: ExtensionBundleOrder.Solution,
  registry: [
    ThemesRegistrator
    // add registrators here
  ]
});
