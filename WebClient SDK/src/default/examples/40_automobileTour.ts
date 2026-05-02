import { extension, localize } from '@tessa/application';
import { reaction } from 'mobx';
import { Visibility } from 'tessa/platform';
import { delay } from 'tessa/platform/delay';
import { showConfirmWithCancel } from 'tessa/ui';
import { CardToolbarAction, ICardEditorModel } from 'tessa/ui/cards';
import { TextBoxViewModel } from 'tessa/ui/cards/controls';
import { TourBase } from 'tessa/ui/tour/tourBase';
import { TourHelper } from 'tessa/ui/tour/tourHelper';
import { IWorkplaceViewComponent } from 'tessa/ui/views';
import { ContentPlaceArea, ContentPlaceOrder, ViewButtonViewModel } from 'tessa/ui/views/content';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { WorkspaceStorage } from 'tessa/workspaceStorage';

/** Пример реализации онбоардинга (интерактивного обучения) в представлении Автомобили. */
@extension({ name: 'AutomobileTourViewExtension' })
export class AutomobileTourViewExtension extends WorkplaceViewComponentExtension {
  public getExtensionName(): string {
    return 'AutomobileTourViewExtension';
  }

  public shouldExecute(model: IWorkplaceViewComponent): boolean {
    // Выполняется в представлении "Автомобили".
    return model.dataNodeMetadata.compositionId === '23d03a10-e610-442d-9e8d-714ff05829f4';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    let continueTourButton: ViewButtonViewModel;
    const tour = new AutomobileTour();

    // Добавляем кнопку для продолжение обучения.
    model.contentFactories.set('continue tour', c => {
      continueTourButton = new ViewButtonViewModel(
        c,
        ContentPlaceArea.ToolBarPanel,
        ContentPlaceOrder.AfterAll
      );
      continueTourButton.caption = 'Запустить обучение';
      continueTourButton.icon = 'icon-thin-220';
      continueTourButton.captionPosition = 'after';
      continueTourButton.type = 'small';
      continueTourButton.theme = 'control';
      continueTourButton.visibility = Visibility.Collapsed;
      // Запускаем отложенное обучение.
      continueTourButton.onClick = () => tour.forceStart();
      return continueTourButton;
    });

    model.onRefreshed.addOnce(async () => {
      // Запускаем обучение, если оно не пройдено и не отложено.
      // В этот же момент проставляется флаг 'postponed' у tour.
      await tour.startIfNeed();
    });

    this.disposeList.add(
      reaction(
        () => tour.postponed,
        postponed =>
          (continueTourButton.visibility = postponed ? Visibility.Visible : Visibility.Collapsed)
      )
    );
  }

  public finalized(): void {
    this.disposeList.dispose();
  }
}

//#region tour class

class AutomobileTour extends TourBase {
  constructor() {
    super('automobile', {
      useModalOverlay: true,
      keyboardNavigation: false,
      // Задаем дефолтные настройки для всех создаваемых шагов.
      defaultStepOptions: {
        // Этот класс необходимо добавлять, чтобы подхватить базовые стили.
        classes: 'shepherd-theme',
        scrollTo: true,
        // Сдвигаем всплывающий диалог вниз, чтобы он не перекрывал собой элемент, на который указывает.
        floatingUIOptions: {
          middleware: [TourHelper.addOffset({ mainAxis: 12, crossAxis: 0 })]
        },
        disableScroll: true
      }
    });

    this.completeButtonText = localize('$Tour_Button_Complete');
    this.continueButtonText = localize('$Tour_Button_Continue');

    // Создаём шаги.
    this.createStep0();
    this.createStep1();
    this.createStep2();
    this.createStep3();
    this.createStep3Error();
    this.createStep4();
    this.createStep5();
    this.createStep5a();
    this.createStep6();
    this.createStep7();
    this.createStep8();
    this.createStep9();
    this.createStep10();
    this.createStep11();
  }

  //#region fields

  private readonly continueButtonText: string;
  private readonly completeButtonText: string;

  private carCardEditor: ICardEditorModel | null = null;

  //#endregion

  //#region steps

  private createStep0() {
    this.addStep({
      id: '0',
      title: 'Обучение',
      subtitle: 'Пример реализации',
      headerImage: 'images-logo',
      headerBackground: 'linear-gradient(355.12deg, #3182AF -26.16%, #35A7E7 122.56%)',
      titleTextColor: '#fff',
      text: 'Карточки автомобилей выступают в качестве примеров реализации различной функциональности в системе. Для ознакомления с карточкой автомобиля, нажмите кнопку "Продолжить"',
      attachTo: {
        element: null // Если не аттачиться к элементу, то всплывающее окно отобразится по центру.
      },
      buttons: [
        {
          text: this.continueButtonText,
          classes: 'button-small button-theme-transparent',
          action: () => this.next()
        },
        {
          text: this.completeButtonText,
          classes: 'button-small button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep1() {
    this.addStep({
      id: '1',
      text: 'Создадим новую карточку автомобиля',
      attachTo: {
        element: () =>
          // Ищем элемент по селектору. Опционально добавляем предикат.
          TourHelper.findElement({
            selector: '.view-toolbar-panel button',
            innerSelector: 'span',
            find: item => item.innerText === localize('$Views_CreateCardExtension_ButtonCaption')
          }),
        on: 'bottom' // Положение всплывающего окна относительно элемента.
      },
      advanceOnTargetClick: true, // Перейти на следующий шаг при клике на элементе.
      modalOverlayOpeningRadius: 4,
      buttons: [
        {
          text: this.completeButtonText,
          classes: 'button-small button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep2() {
    const step = this.addStep({
      id: '2',
      text: 'Для сохранения карточки необходимо заполнить марку автомобиля',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.control-container-external',
            innerSelector: '.control-caption span',
            find: item => item.innerText === localize('$AbTest_CardTypes_Controls_CarModel')
          }),
        on: 'bottom'
      },
      scrollTo: false,
      beforeShowPromise: async () => {
        // Здесь мы получаем CardEditor открытой карточки и записываем его.
        for (let attempt = 0; attempt < 5; attempt++) {
          const wp = WorkspaceStorage.instance.currentCardWorkspace;
          if (!!wp?.editor) {
            this.carCardEditor = wp.editor;
            break;
          }
          await delay(500);
        }
      },
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      // Определяем функцию для события отображения всплывающего окна.
      when: {
        show: () => {
          const carNameControl = this.carCardEditor?.cardModel?.controlsBag.find(
            x => x.name === 'CarName'
          ) as TextBoxViewModel;
          if (!carNameControl) {
            return;
          }
          // Следим за текстом внутри контрола, если он есть, то переходим на следующий шаг.
          // Не забываем добавить функцию в disposeList. Функция dispose() вызывается при сокрытии или удалении шага.
          step.disposeList.add(
            reaction(
              () => carNameControl.text,
              text => {
                setTimeout(() => {
                  if (!!text) {
                    this.next();
                  }
                });
              }
            )
          );
        }
      }
    });
  }

  private createStep3() {
    const step = this.addStep({
      id: '3',
      text: 'Сохраним карточку автомобиля',
      attachTo: {
        element: '[data-toolbar-group-id="SaveCard_button"]',
        on: 'bottom'
      },
      when: {
        show: () => {
          const saveButton = this.carCardEditor?.toolbar.items.find(
            x => x.name === 'SaveCard'
          ) as CardToolbarAction;
          if (!saveButton) {
            return;
          }
          const saveCommand = saveButton.command;
          // Переопределяем кнопку сохранения.
          saveButton.setCommand(async () => {
            const saved = await this.carCardEditor?.saveCard();
            // Если карточка сохранилась - переходим на этап со следующим номером.
            if (saved) {
              this.show(`${Number(step.id) + 1}`, true);
              // Иначе переходим на этап с ошибкой.
            } else {
              this.show('3error');
            }
          });
          step.disposeList.add(() => saveButton.setCommand(saveCommand!));
        }
      },
      buttons: [
        {
          text: this.completeButtonText,
          classes: 'button-small button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep3Error() {
    this.addStep({
      id: '3error',
      text: 'Для сохранения карточки необходимо корректно заполнить марку автомобиля',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.dialog-wrapper button',
            innerSelector: 'span',
            find: item => item.innerText === localize('$UI_Common_OK')
          }),
        on: 'bottom'
      },
      beforeShowPromise: () => delay(500),
      modalOverlayOpeningRadius: 4,
      onTargetClick: () => {
        // После закрытия ошибки возвращаемся на шаг номер 2.
        this.show('2');
      },
      buttons: [
        {
          text: this.completeButtonText,
          classes: 'button-small button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep4() {
    const step = this.addStep({
      id: '4',
      text: 'Укажем цену автомобиля 1 тыс или более',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.control-container-external',
            innerSelector: '.control-caption span',
            find: item => item.innerText === localize('$AbTest_CardTypes_Controls_Price')
          }),
        on: 'bottom'
      },
      scrollTo: false,
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      keepFocus: true, // Не переключаем фокус на элемент при ресайзе модального окна.
      when: {
        show: () => {
          const fields = this.carCardEditor?.cardModel?.card.sections
            .get('AbCarMainInfo')
            ?.tryGetFields();
          if (!fields) {
            return;
          }
          // Подписываемся на изменения поля и переходим на следующий этап, если цена >= 1000.
          step.disposeList.add(
            fields.fieldChanged.add(e => {
              if (e.fieldName === 'Cost') {
                const value = e.fieldValue as number;
                if (value >= 1000) {
                  this.next();
                }
              }
            })
          );
        }
      }
    });
  }

  private createStep5() {
    const step = this.addStep({
      id: '5',
      text: 'По кнопке обновления в поле "Цена со скидкой" система высчитает цену со скидкой 20%',
      attachTo: {
        element: '[data-toolbar-group-id="Discount"]',
        on: 'bottom'
      },
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      scrollTo: false,
      advanceOnTargetClick: true,
      buttons: [
        {
          text: this.continueButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.show(`${Number(step.id) + 1}`, true)
        },
        {
          text: this.completeButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep5a() {
    this.addStep({
      id: '5a',
      text: 'Нажмём на кнопку "ОК"',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.dialog-wrapper button',
            innerSelector: 'span',
            find: item => item.innerText === localize('$UI_Common_OK')
          }),
        on: 'bottom'
      },
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      scrollTo: false,
      advanceOnTargetClick: true,
      beforeShowPromise: () => delay(200)
    });
  }

  private createStep6() {
    this.addStep({
      id: '6',
      text: 'Это пример реализации кастомной кнопки внутри контрола',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.control-container-external',
            innerSelector: '.control-caption span',
            find: item => item.innerText === localize('$AbTest_CardTypes_Controls_DiscountPrice')
          }),
        on: 'bottom'
      },
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      scrollTo: false,
      // Выключаем взаимодействия с элементами, если в рамках обучения оно не предполагается и может сломать последовательность.
      stopPropagation: true,
      buttons: [
        {
          text: this.continueButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.next()
        },
        {
          text: this.completeButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep7() {
    this.addStep({
      id: '7',
      text: 'Опустившись в нижнюю часть карточки, вызовем диалог создания xml файла по кнопке "Запросить из внешней системы"',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.button-normal',
            innerSelector: 'span',
            find: item =>
              item.innerText === localize('$AbTest_CardTypes_Controls_RequestExternalSystem')
          }),
        on: 'bottom'
      },
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      advanceOnTargetClick: true,
      buttons: [
        {
          text: this.completeButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep8() {
    const step = this.addStep({
      id: '8',
      text: 'Необходимо заполнить марку автомобиля и выбрать имя водителя',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.form',
            innerSelector: '.block-caption-clickable-area',
            find: item => item.innerText === localize('$AbTest_CardTypes_Blocks_DriverInformation')
          }),
        on: 'bottom'
      },
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      // Не используем step.hide() или step.show() тут, чтобы не триггерить связанные ивенты.
      onShowDialog: _ => (step.concealed = true),
      onHideDialog: _ => (step.concealed = false),
      beforeShowPromise: async () => delay(400), // Ожидаем небольшой промежуток времени, чтобы шаг всплывал после рендера привязанного элемента.
      buttons: [
        {
          text: this.continueButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.next()
        },
        {
          text: this.completeButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep9() {
    this.addStep({
      id: '9',
      text: 'Нажимаем на кнопку для формирования xml файла',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.button-theme-card-toolbar',
            innerSelector: '.icon-Int426'
          }),
        on: 'bottom'
      },
      advanceOnTargetClick: true,
      buttons: [
        {
          text: this.completeButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep10() {
    this.addStep({
      id: '10',
      text: 'Сформированный файл отображается в секции файлов',
      attachTo: {
        element: () =>
          TourHelper.findElement({
            selector: '.block',
            innerSelector: '.block-caption-clickable-area',
            find: item => item.innerText === localize('$CardTypes_Blocks_Controls_Files')
          }),
        on: 'bottom'
      },
      modalOverlayOpeningPadding: 2,
      modalOverlayOpeningRadius: 4,
      beforeShowPromise: () => delay(200),
      stopPropagation: true,
      buttons: [
        {
          text: this.continueButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.next()
        },
        {
          text: this.completeButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.postponeTour()
        }
      ]
    });
  }

  private createStep11() {
    this.addStep({
      id: '11',
      text: `Описание остальных примеров реализаций в карточке автомобиля, включая текущий курс обучения, можно найти на сайте
        <a href="https://tessa.ru/docs/4.1/dev/web/examples" target="_blank">tessa.ru</a> в документации, в разделе Разработчику -> Разработка web-расширение -> Примеры расширений`,
      buttons: [
        {
          text: this.completeButtonText,
          classes: 'button-normal button-theme-transparent',
          action: () => this.complete()
        }
      ]
    });
  }

  //#endregion

  //#region methods

  public override complete(): void {
    this.carCardEditor = null;
    super.complete();
  }

  public override postpone(): void {
    this.carCardEditor = null;
    super.postpone();
  }

  private async postponeTour(): Promise<void> {
    // Скрываем элементы онбоардинга, чтобы он не перекрывал диалоговые окна.
    this.hide();
    // Спрашиваем, хотим ли отложить прохождение обучения.
    const shouldPostpone = await showConfirmWithCancel('$Tour_Postpone_Confirm_Text', undefined, {
      YesButtonText: '$Tour_Postpone_Confirm_YesButton',
      NoButtonText: '$Tour_Postpone_Confirm_NoButton'
    });
    // Если нажата отмена, то продолжаем обучение.
    if (shouldPostpone === null) {
      await this.currentStep?.show();
      return;
    }
    // Если нажато "Отложить", то откладываем.
    if (shouldPostpone) {
      this.postpone();
      return;
    }
    // Если нажато "Больше не предлагать", то считаем, что обучение завершено.
    this.complete();
  }

  //#endregion
}

//#endregion
