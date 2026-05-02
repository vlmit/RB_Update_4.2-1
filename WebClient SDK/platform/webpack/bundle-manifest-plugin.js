const fs = require('fs');
const { WebpackManifestPlugin } = require('webpack-manifest-plugin');

/**
 * Плагин генерит файл-манифест, содержащий список файлов, которые должны быть загружены для инициализации приложения.
 *
 * ```json
 * {
 *  "scripts": [
 *    "scriptPath",
 *    ...
 *  ],
 *  "assets": {
 *    "assetName": "assetPath",
 *    ...
 *  }
 * }
 * ```
 */
module.exports = class BundleManifestPlugin extends WebpackManifestPlugin {
  constructor({
    fileName = 'webpack.manifest.json',
    publicPath = '',
    chunkOrder = {},
    assetDirs = [],
    ignore = []
  } = {}) {
    super({
      fileName,
      publicPath,
      filter: file => {
        if (ignore.includes(file.name)) {
          return false;
        }
        return file.isInitial;
      },
      map: file => {
        file.order = chunkOrder[file.name] ?? 0;
        return file;
      },
      generate: (_seed, files) => {
        const scripts = files.sort((a, b) => a.order - b.order).map(x => x.path);
        const assets = {};

        for (const dirPath of assetDirs) {
          if (!fs.existsSync(dirPath)) {
            continue;
          }

          for (const asset of fs.readdirSync(dirPath)) {
            assets[asset.slice(0, asset.indexOf('.'))] = asset;
          }
        }

        return {
          scripts,
          assets
        };
      }
    });
  }
};
