const { Operation } = require('./operation');

/** @typedef {import("webpack/lib/Compiler")} Compiler */

const PLUGIN_NAME = 'modify-source-plugin';

/**
 * https://github.com/artembatura/modify-source-webpack-plugin/blob/main/src
 *
 * modify-source-webpack-plugin кучу какого-то подозрительного мусора.
 * форкаем только то что нужно.
 */

/**
const validationSchema = {
  type: 'object',
  additionalProperties: false,
  properties: {
    rules: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          test: {
            anyOf: [{ instanceof: 'Function' }, { instanceof: 'RegExp' }]
          },
          operations: {
            type: 'array',
            items: {
              type: 'object'
            }
          }
        }
      }
    },
    constants: {
      type: 'object'
    },
    debug: {
      type: 'boolean'
    }
  }
};
*/

class ModifySourcePlugin {
  constructor(options) {
    this.options = options;
  }

  /**
   * Apply the plugin
   * @param {Compiler} compiler the compiler instance
   * @returns {void}
   */
  apply(compiler) {
    const { rules, debug, constants = {} } = this.options;

    compiler.hooks.compilation.tap(PLUGIN_NAME, compilation => {
      const modifiedModules = [];

      const tapCallback = (_, normalModule) => {
        const userRequest = normalModule.userRequest || '';

        const startIndex =
          userRequest.lastIndexOf('!') === -1 ? 0 : userRequest.lastIndexOf('!') + 1;

        const moduleRequest = userRequest.substring(startIndex).replace(/\\/g, '/');

        if (modifiedModules.includes(moduleRequest)) {
          return;
        }

        rules.forEach(ruleOptions => {
          const test = ruleOptions.test;
          const isMatched = (() => {
            if (typeof test === 'function' && test(normalModule)) {
              return true;
            }

            return test instanceof RegExp && test.test(moduleRequest);
          })();

          if (isMatched) {
            const serializableOperations = ruleOptions.operations?.map(op =>
              Operation.makeSerializable(op)
            );

            normalModule.loaders.push({
              loader: require.resolve('./loader.js'),
              options: {
                moduleRequest,
                operations: serializableOperations,
                constants
              }
            });

            modifiedModules.push(moduleRequest);

            if (debug) {
              console.log(`\n[${PLUGIN_NAME}] Use loader for "${moduleRequest}".`);
            }
          }
        });
      };

      const NormalModule = compiler.webpack?.NormalModule;
      const isNormalModuleAvailable =
        Boolean(NormalModule) && Boolean(NormalModule.getCompilationHooks);

      if (isNormalModuleAvailable) {
        NormalModule.getCompilationHooks(compilation).beforeLoaders.tap(PLUGIN_NAME, tapCallback);
      } else {
        compilation.hooks.normalModuleLoader.tap(PLUGIN_NAME, tapCallback);
      }
    });
  }
}

module.exports = ModifySourcePlugin;
