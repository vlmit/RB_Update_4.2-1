const packageJSON = require('./package.json');

module.exports = function () {
  return {
    minify: false,
    sourceMaps: true,
    module: {
      type: 'es6',
      strictMode: true,
      noInterop: true
    },
    jsc: {
      externalHelpers: true,
      // target: 'es2021',
      parser: {
        syntax: 'typescript',
        tsx: true,
        decorators: true,
        dynamicImport: true
      },
      transform: {
        legacyDecorator: true,
        decoratorMetadata: false,
        useDefineForClassFields: false,
        react: {
          runtime: 'automatic',
          throwIfNamespace: false,
          useBuiltins: false
        }
      },
      keepClassNames: true,
      baseUrl: __dirname
    },
    env: {
      targets: packageJSON.browserslist,
      mode: 'entry',
      coreJs: '3.47',
      include: ['transform-async-to-generator']
    }
  };
};
