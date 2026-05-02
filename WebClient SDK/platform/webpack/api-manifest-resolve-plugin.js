const fs = require('fs/promises');
const path = require('path');
const VirtualModulesPlugin = require('webpack-virtual-modules');

/** @typedef {import("webpack/lib/Compiler")} Compiler */

const PLUGIN_NAME = 'api-manifest-resolve-plugin';
const manifests = new Map();

class ApiManifestResolvePlugin {
  constructor({
    manifestsPath,
    contextPath,
    ignore,
    writeModuleRequest,
    noPromise,
    logAsyncModules,
    extensions
  }) {
    if (!manifestsPath) {
      throw new Error('Path to api manifests is undefined.');
    }
    if (!contextPath) {
      throw new Error('Context path is undefined.');
    }

    this.manifestsPath = manifestsPath;
    if (!contextPath.endsWith(path.sep)) {
      contextPath = contextPath + path.sep;
    }
    this.contextPath = contextPath;
    this.ignore = ignore;
    this.writeModuleRequest = writeModuleRequest ?? false;
    this.noPromise = noPromise ?? false;
    this.logAsyncModules = logAsyncModules ?? false;

    this.extensions = [''];
    if (extensions) {
      for (const ext of extensions) {
        this.extensions.push(ext);
        this.extensions.push(`/index${ext}`);
      }
    }
  }

  /**
   * Apply the plugin
   * @param {Compiler} compiler the compiler instance
   * @returns {void}
   */
  apply(compiler) {
    const virtualModules = new VirtualModulesPlugin();

    virtualModules.apply(compiler);

    compiler.hooks.beforeCompile.tapAsync(PLUGIN_NAME, async (_context, callback) => {
      try {
        let manifestRuntimeModule = '';

        const files = await fs.readdir(this.manifestsPath);
        for (const file of files) {
          const filePath = path.join(this.manifestsPath, file);
          const fileName = path.parse(filePath).name;
          const fileContent = await fs.readFile(filePath);
          const manifest = JSON.parse(fileContent.toString('utf8'));

          const data = new Map();
          for (const key in manifest) {
            data.set(key, manifest[key]);
          }

          manifests.set(fileName, data);

          const chunk_meta = data.get('__chunk_meta__');
          const chunkIdValue = getSerializeableChunkId(chunk_meta.id);
          const chunksValue = chunk_meta.chunks.map(x => getSerializeableChunkId(x)).join(',');
          manifestRuntimeModule += `globalThis.__api_manifest__.addChunkInfo(${chunkIdValue}, [${chunksValue}]);`;
        }

        virtualModules.writeModule(
          'node_modules/api-manifest-runtime/index.js',
          manifestRuntimeModule
        );
        virtualModules.writeModule(
          'node_modules/api-manifest-runtime/package.json',
          JSON.stringify({
            name: 'api-manifest-runtime',
            version: '1.0.0',
            main: 'index.js'
          })
        );
      } catch (err) {
        return callback(err);
      }

      callback();
    });
  }

  getEntries(entries) {
    if (Array.isArray(entries)) {
      return ['api-manifest-runtime/index.js', ...entries];
    }

    return { 'api-manifest-runtime': 'api-manifest-runtime/index.js', ...entries };
  }

  getExternalResolver() {
    const { contextPath, ignore, writeModuleRequest, noPromise, logAsyncModules, extensions } =
      this;

    const emptyResolve = [null, null];
    const resolveFromManifest = request => {
      if (!request) {
        return emptyResolve;
      }

      if (ignore && ignore(request)) {
        return emptyResolve;
      }

      for (const [fileName, data] of manifests) {
        const module = data.get(request);
        if (module != null) {
          const chunk_meta = data.get('__chunk_meta__');
          if (!chunk_meta) {
            return [new Error(`Chunk meta info is missing in manifest file ${fileName}.`), null];
          }

          const loadArgs = [];
          loadArgs.push(module);
          loadArgs.push(getSerializeableChunkId(chunk_meta.id));
          if (writeModuleRequest) {
            loadArgs.push(`'${request}'`);
          }

          if (!noPromise && chunk_meta.async) {
            if (logAsyncModules) {
              console.log(`async request: ${request}`);
            }

            return [null, `promise __api_manifest__.load(${loadArgs.join(',')})`];
          }

          return [null, `var __api_manifest__.load(${loadArgs.join(',')})`];

          // const chunkId = Number(chunkName);
          // // если это именнованный чанк (например app), то просто резолвим модуль
          // if (isNaN(chunkId)) {
          //   return `var __api_manifest__.load(${module}, '${request}')`;
          // }

          // const chunks = data.get('__chunks__') ?? [];
          // // если чанк называется по его id, то сначала нужно загрузить все необходимые чанки
          // return `var __api_manifest__.loadAsync(${module}, [${chunks.join()}], '${request}')`;
        }
      }

      return emptyResolve;
    };

    return function (ctx, callback) {
      const { context, request, getResolve } = ctx;

      // относительные пути сразу точно не external
      if (request.startsWith('./') || request.startsWith('../')) {
        return callback();
      }

      const resolver = getResolve();
      resolver(context, request, (err, result) => {
        // если не смогли отрезолвить, то пытаемся найти в manifests
        if (err) {
          if (request) {
            for (let i = 0; i < extensions.length; i++) {
              const extension = extensions[i];
              const requestPlusExt = request + extension;

              const [resolveError, resolvedPath] = resolveFromManifest(requestPlusExt);
              if (resolveError) {
                return callback(resolveError);
              } else if (resolvedPath) {
                return callback(null, resolvedPath);
              }
            }
          }

          // ошибку не прокидываем, т.к. путь может быть отрезовлен другими плагинами
          return callback();
        }

        // если смогли отрезолвить (например node_modules), то нужно проверить, что такого модуля нет в манифесте.
        // если есть, то нужно использовать его
        const actualPath = result.replace(contextPath, '').replaceAll(path.sep, '/');
        const [resolveError, resolvedPath] = resolveFromManifest(actualPath);
        if (resolveError) {
          return callback(resolveError);
        } else if (resolvedPath) {
          return callback(null, resolvedPath);
        }

        return callback();
      });
    };
  }
}

module.exports = ApiManifestResolvePlugin;

function getSerializeableChunkId(id) {
  return typeof id === 'number' ? id : `"${id}"`;
}
