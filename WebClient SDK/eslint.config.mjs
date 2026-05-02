import { defineConfig } from 'eslint/config';
import js from '@eslint/js';
import ts from '@typescript-eslint/eslint-plugin';
import tsParser from '@typescript-eslint/parser';
import react from 'eslint-plugin-react';
import prettier from 'eslint-plugin-prettier';
import babelParser from '@babel/eslint-parser';
import globals from 'globals';

const baseJSConfig = sourceType => ({
  languageOptions: {
    parser: babelParser,
    ecmaVersion: 'latest',
    sourceType,
    parserOptions: {
      requireConfigFile: false,
      ecmaFeatures: {
        jsx: false
      }
    },
    globals: {
      ...globals.node
    }
  },
  plugins: { prettier },
  rules: {
    ...js.configs.recommended.rules,
    ...prettier.configs.recommended.rules,

    'no-unused-vars': ['error', { argsIgnorePattern: '^_' }]
  }
});

export default defineConfig([
  {
    ignores: ['node_modules', 'build', 'wwwroot']
  },

  //
  // js files
  //
  {
    files: ['**/*.js'],
    ...baseJSConfig('commonjs')
  },

  {
    files: ['**/*.mjs'],
    ...baseJSConfig('module')
  },

  //
  // main project
  //
  {
    files: ['src/**/*.ts', 'src/**/*.tsx'],
    languageOptions: {
      parser: tsParser,
      ecmaVersion: 2020,
      sourceType: 'module',
      parserOptions: {
        ecmaFeatures: {
          jsx: true
        }
      }
    },
    settings: {
      react: { version: 'detect' }
    },
    plugins: {
      '@typescript-eslint': ts,
      react,
      prettier
    },
    rules: {
      ...ts.configs.recommended.rules,
      ...react.configs.recommended.rules,
      ...prettier.configs.recommended.rules,

      'no-shadow': 'off',
      'comma-dangle': 'off',
      'no-dupe-class-members': 'off',
      'no-bitwise': 'off',
      'no-undef': 'off',

      'react/no-did-mount-set-state': 'off',
      'react/prop-types': 'off',
      'react/jsx-uses-react': 'off',
      'react/react-in-jsx-scope': 'off',

      'prettier/prettier': 'error',

      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-shadow': 'off',
      '@typescript-eslint/no-empty-function': 'off',
      '@typescript-eslint/no-empty-interface': 'off',
      '@typescript-eslint/no-non-null-assertion': 'off',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      '@typescript-eslint/no-unsafe-function-type': 'off',
      '@typescript-eslint/no-unused-expressions': 'off',
      '@typescript-eslint/no-empty-object-type': 'off',
      '@typescript-eslint/no-namespace': 'off',
      '@typescript-eslint/explicit-module-boundary-types': [
        'error',
        {
          allowArgumentsExplicitlyTypedAsAny: true
        }
      ]
    }
  }
]);
