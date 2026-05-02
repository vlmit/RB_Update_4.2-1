const path = require('path');
const VirtualModulesPlugin = require('webpack-virtual-modules');
const ModifySourcePlugin = require('./modify-source-plugin');
const { ConcatOperation } = require('./operation');

/** @typedef {import("webpack/lib/Compiler")} Compiler */

const PLUGIN_NAME = 'bundle-bootstrap-plugin';

class BundleBootstrapPlugin {
  constructor({ entries, noPromise }) {
    if (!entries) {
      throw new Error('Entries is undefined.');
    }

    this.entries = entries;
    this.noPromise = noPromise;
  }

  /**
   * Apply the plugin
   * @param {Compiler} compiler the compiler instance
   * @returns {void}
   */
  apply(compiler) {
    if (this.noPromise) {
      return;
    }

    // bundle loading scripts
    const virtualModules = new VirtualModulesPlugin();

    virtualModules.apply(compiler);

    function writeVirtualModule(bundleName, count) {
      virtualModules.writeModule(
        `node_modules/${bundleName}/index.js`,
        `window.dispatchEvent(new CustomEvent("bundleloading", { detail: ${count} }));`
      );
      virtualModules.writeModule(
        `node_modules/${bundleName}/package.json`,
        JSON.stringify({
          name: bundleName,
          version: '1.0.0',
          main: 'index.js'
        })
      );
    }

    compiler.hooks.beforeCompile.tapAsync(PLUGIN_NAME, async (_context, callback) => {
      if (Array.isArray(this.entries)) {
        writeVirtualModule('bundle-bootstrap-extensions', this.entries.length);
      } else {
        for (const key of Object.keys(this.entries)) {
          const bundleName = `bundle-bootstrap-${key}`;
          writeVirtualModule(bundleName, 1);
        }
      }

      callback();
    });

    // bundle loaded scripts
    const rootPath = compiler.options.context;
    const entries = (
      Array.isArray(this.entries) ? this.entries : Array.from(Object.values(this.entries))
    )
      .map(x => (Array.isArray(x) ? x[x.length - 1] : x))
      .map(x => path.resolve(rootPath, x));

    new ModifySourcePlugin({
      rules: [
        {
          test: m => {
            return entries.includes(m.resource);
          },
          operations: [
            new ConcatOperation(
              'end',
              '\n\nsetTimeout(() => window.dispatchEvent(new CustomEvent("bundleloaded")), 0);'
            )
          ]
        }
      ]
    }).apply(compiler);
  }

  getEntries() {
    if (this.noPromise) {
      return Array.isArray(this.entries) ? [...this.entries] : { ...this.entries };
    }

    if (Array.isArray(this.entries)) {
      return ['bundle-bootstrap-extensions/index.js', ...this.entries];
    }

    const resultEntries = { ...this.entries };

    for (const key of Object.keys(resultEntries)) {
      const originalEntry = Array.isArray(resultEntries[key])
        ? resultEntries[key]
        : [resultEntries[key]];
      resultEntries[key] = [`bundle-bootstrap-${key}/index.js`, ...originalEntry];
    }

    return resultEntries;
  }
}

module.exports = BundleBootstrapPlugin;
