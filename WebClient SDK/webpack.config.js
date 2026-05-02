const path = require('path');
const webpack = require('webpack');
const TerserPlugin = require('terser-webpack-plugin');
const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');
const WebpackBar = require('webpackbar');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const ApiManifestResolvePlugin = require('./platform/webpack/api-manifest-resolve-plugin');
const BundleManifestPlugin = require('./platform/webpack/bundle-manifest-plugin');
const BundleBootstrapPlugin = require('./platform/webpack/bundle-bootstrap-plugin');
const swcConfig = require('./swc.config');

const outputPath = path.join(__dirname, 'wwwroot/extensions');
const outputThemePath = path.join(__dirname, 'wwwroot/themes');
const NoPromiseExternals = false; // включить если есть проблемы с импортом асинхронных чанков платформы

const entries = {
  default: './src/default/index.ts',
  solution: './src/solution/index.ts'
};

module.exports = function (env, argv) {
  const isServeMode = env ? env['WEBPACK_SERVE'] : false;
  const mode = process.env.WEBPACK_ENV ?? argv.mode ?? 'production';
  const e2e = env.E2E ?? false;
  const asModule = env.MODULE ?? false;
  const isMobileBuild = process.env.PLATFORM === 'mobile';

  // Если собираемся как стендэлон модуль, то не собираем дефолтные бандлы
  if (asModule) {
    delete entries['default'];
    delete entries['solution'];
  }

  const params = isServeMode
    ? {
        mode: 'development',
        devServer: {
          headers: {
            'Access-Control-Allow-Origin': '*'
          },
          port: 3000,
          hot: false,
          liveReload: true,
          client: {
            overlay: {
              runtimeErrors: error => {
                if (
                  error?.message === 'ResizeObserver loop completed with undelivered notifications.'
                ) {
                  console.error(error);
                  return false;
                }
                return true;
              }
            }
          },
          static: {
            directory: outputPath,
            publicPath: '/extensions'
          }
        },
        output: {
          path: outputPath,
          filename: 'extensions.bundle.js',
          clean: true,
          publicPath: 'auto'
        }
      }
    : {
        mode,
        output: {
          path: outputPath,
          filename: '[name].[contenthash].js',
          chunkFilename: 'chunk_[name].[contenthash].js',
          clean: true,
          publicPath: 'auto'
        }
      };

  const apiManifestPlugin = new ApiManifestResolvePlugin({
    manifestsPath: path.join(
      __dirname,
      'platform',
      !isMobileBuild ? 'platform-api-manifests' : 'platform-api-manifests-mobile'
    ),
    contextPath: __dirname,
    extensions: ['.ts', '.tsx', '.js', '.jsx'],
    noPromise: NoPromiseExternals,
    writeModuleRequest: isServeMode
    // logAsyncModules: true // логирование запросов к асинхронным чанкам платформы
  });

  const bundleBootstrapPlugin = new BundleBootstrapPlugin({
    entries,
    noPromise: NoPromiseExternals
  });

  // добавляем в entries дополнительные модули, необходимые для работы ApiManifest и promise-external
  let resultEntries = bundleBootstrapPlugin.getEntries();
  resultEntries = apiManifestPlugin.getEntries(resultEntries);
  resultEntries = isServeMode ? Object.values(resultEntries).flatMap(x => x) : resultEntries;

  /**
   * @type {import('webpack').Configuration}
   */
  const config = {
    ...params,
    target: 'web',
    devtool: e2e ? 'eval' : 'source-map',
    context: path.join(__dirname),
    entry: resultEntries,
    externals: [apiManifestPlugin.getExternalResolver()],
    module: {
      rules: [
        {
          test: /\.(j|t)sx?$/,
          exclude: /[\\/]node_modules[\\/]/,
          use: [
            {
              loader: 'swc-loader',
              options: swcConfig()
            }
          ]
        },
        {
          test: /\.css$/,
          use: [
            {
              loader: 'style-loader',
              options: {
                insert: '#tessa-css-modules-root'
              }
            },
            {
              loader: 'css-loader',
              options: {
                esModule: false
              }
            }
          ]
        },
        {
          test: /\.scss$/,
          use: [
            {
              loader: 'style-loader',
              options: { insert: '#tessa-css-modules-root' }
            },
            {
              loader: 'css-loader',
              options: {
                esModule: false
              }
            },
            { loader: 'sass-loader' }
          ]
        },
        {
          test: /\.(png|jpg|gif|webp)$/,
          // .bat скрипты не делают глубокое копирование wwwroot. временно грузим как url
          type: 'asset/inline'
          // type: 'asset',
          // generator: {
          //   filename: 'images/[name][ext]'
          // },
          // parser: {
          //   dataUrlCondition: {
          //     maxSize: 10 * 1024 // 10kb
          //   }
          // }
        }
      ]
    },
    optimization: {
      runtimeChunk:
        isServeMode || asModule
          ? false
          : {
              name: 'webpack-runtime'
            },
      moduleIds: 'deterministic',
      minimizer: [
        new TerserPlugin({
          extractComments: false,
          minify: TerserPlugin.swcMinify,
          // `terserOptions` options will be passed to `swc` (`@swc/core`)
          // Link to options - https://swc.rs/docs/config-js-minify
          terserOptions: {}
        })
      ],
      splitChunks: {
        cacheGroups: {
          commons: {
            name: 'commons',
            chunks: 'initial',
            minChunks: 2,
            reuseExistingChunk: true,
            enforce: true
          }
        }
      }
    },
    plugins: [
      apiManifestPlugin,
      bundleBootstrapPlugin,
      new webpack.DefinePlugin({
        'process.env': {
          BUILD_TIME: JSON.stringify(Date.now()),
          E2E: JSON.stringify(e2e),
          PLATFORM: JSON.stringify(!isMobileBuild ? 'web' : 'mobile')
        }
      }),
      new ForkTsCheckerWebpackPlugin({
        async: isServeMode
      }),
      new webpack.ContextReplacementPlugin(/moment[/\\]locale$/, /ru|en-gb/),
      new BundleManifestPlugin({
        fileName: 'extensions.manifest.json',
        chunkOrder: {
          'webpack-runtime.js': -1000,
          'api-manifest-runtime.js': -900,
          'commons.js': -800
        }
      }),
      new WebpackBar({
        profile: false
      }),
      !isServeMode &&
        !asModule &&
        new CopyWebpackPlugin({
          patterns: [
            {
              context: 'src/solution/themes',
              from: '**/*.json',
              to: outputThemePath
            }
          ]
        })
    ].filter(Boolean),
    resolve: {
      extensions: ['.ts', '.tsx', '.js', '.jsx', '.css', '.scss'],
      modules: [path.resolve(__dirname), 'node_modules']
    },
    performance: {
      hints: false
    },
    stats: isServeMode
      ? 'minimal'
      : {
          errorDetails: true
        },
    infrastructureLogging: { level: 'error' }
  };

  return config;
};
