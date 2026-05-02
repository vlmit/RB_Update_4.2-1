import classnames from 'classnames';
import { LoaderUIContainer } from 'components/loadingOverlay';

/**
 * Переопределяем дефолтный компонент экрана загрузки.
 * Аналогичные изменения должны быть сделаны в `Views/Shared/index.cshtml` в папке с веб-сервисом.
 */

/*
`Views/Shared/index.cshtml`

```
<div id="tessa-app"></div>
<div id="application-loader-overlay">
  <div class="application-loader-container">
    <span class="circle circle-1"></span>
    <span class="circle circle-2"></span>
    <span class="circle circle-3"></span>
    <span class="circle circle-4"></span>
    <span class="circle circle-5"></span>
  </div>
</div>
```
->

```
<div id="tessa-app"></div>
<div id="application-loader-overlay">
  <div class="application-loader-container">
    <span>Loading...</span>
  </div>
</div>
```
*/

LoaderUIContainer.loader = ({ wrapperClass }) => (
  <div className={classnames(wrapperClass, 'application-loader-container')}>
    <span>Loading...</span>
  </div>
);
