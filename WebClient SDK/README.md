
# VSCode

- `.vscode/settings.json.sample` рекомендуемые базовые настройки для `VSCode`.
- `.vscode/extensions.json` рекомендуемые расширения для `VSCode`.

# Scripts

- `npm run start` - стартует webpack-dev-server с hot-reload.

  Для правильной работы веб-сервис должен быть запущен с флагом `SdkHot`:
  ```ps
  cd "C:\Tessa\web"
  $Env:ASPNETCORE_ENVIRONMENT = "SdkHot"
  ./Tessa.Web.Server.exe
  ```

  Чтобы подключиться с устройства, веб-сервис должен быть запущен с дополнительными настройками:
  ```ps
  cd "C:\Tessa\web"
  $Env:ASPNETCORE_ENVIRONMENT = "SdkHot"
  $Env:WEBPACK_PUBLIC_PATH = "//192.168.1.1:3000"
  ./Tessa.Web.Server.exe http://192.168.1.1:5000
  ```
  Для запуска по другому порту:

  `npm run start '--' --port 9000`
  ```ps
  cd "C:\Tessa\web"
  $Env:ASPNETCORE_ENVIRONMENT = "SdkHot"
  $Env:WEBPACK_PUBLIC_PATH = "//0.0.0.0:9000"
  ./Tessa.Web.Server.exe
  ```

- `npm run build` - сборка бандлов в прод режиме. Бандлы собираются в папку `wwwroot/extensions`.

- `npm run build:module` - сборка бандла как стендэлон модуля (например, МЕДО). Дефолтные бандлы (`default`, `solution`) собираться не будут. Бандлы собираются в папку `wwwroot/extensions`.

- `npm run type-check` - проверка корректности типизации.

- `npm run lint` - проверка линтером.
