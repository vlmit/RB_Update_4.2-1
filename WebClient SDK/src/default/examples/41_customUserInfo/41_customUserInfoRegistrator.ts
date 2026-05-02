import { createElement } from 'react';
import { DiContainer } from '@tessa/application';
import { IUserInfoFactory$, UserInfo } from '@tessa/platform';
import { ComponentsRegistry } from '@tessa/ui';
import {
  UserInfoContactRegistry,
  UserInfoContactRegistryItem
} from 'ui/userInfo/userInfoContactRegistry';
import { UserInfoViewModelFactoryParams } from 'ui/userInfo/userInfoType';
import { UserInfoViewModel } from 'ui/userInfo/userInfoViewModel';
import {
  IUserInfoPopoverViewModelFactory$,
  IUserInfoViewModelFactory$
} from 'ui/userInfo/userInfoInjects';
import { CustomUserInfo } from './41_customUserInfoComponent';
import { VacationInfoViewModel } from './41_vacationInfoViewModel';
import { VacationInfo } from './41_vacationInfo';

export function registerCustomUserInfoTypes(container: DiContainer): void {
  // Описываем тип контакта "Телеграм" для отображения ссылки на телеграм пользователя в блоке user info.
  const telegramContact: UserInfoContactRegistryItem = {
    type: 'telegram',
    icon: 'ta icon-thin-014',
    href: value =>
      createElement(
        'a',
        {
          href: `https://t.me/${value}}`,
          target: '_blank'
        },
        `@${value}`
      )
  };

  // Добавляем в реестр контактов новый тип контакта "Телеграм"
  UserInfoContactRegistry.instance.register(telegramContact.type, telegramContact);

  // Переопределяем создание UserInfo, чтобы добавить новый контакт и инфо.
  container.rebind(IUserInfoFactory$).toConstantValue(params => {
    // В params.rawData в дефолтном случае будет хранилище ключ-значение, из которого инициализируется UserInfo.
    // Из хранилища можно можно достать доп. данные. Эти значения можно установить на серверной стороне:
    // - вариант 1 - реализовать собственный IUserInfoProvider, переопределив типовой
    // - вариант 2 - реализовать собственный IUserInfoHandler и зарегистрировать для необходимого type.
    // В params.type хранится название типа обработчика, который занимается получением данных для UserInfo на сервере.
    // Это значение можно использовать чтобы определить для какого сценария запрашивается UserInfo и какие данные содержит.

    const userInfo = params?.userInfo;
    if (userInfo) {
      // Добавляем в инфо всю нужную доп. информацию, которую потом будем доставать и отображать в компоненте.
      userInfo.contacts ??= [];
      userInfo.contacts.push({
        type: 'telegram',
        text: '@test_telegram_username',
        value: 'test_telegram_username'
      });
      userInfo.info = {
        office: 'Москва',
        vacationFrom: '10.10.2024',
        vacationTo: '15.10.2024'
      };
    }

    return new UserInfo(userInfo);
  });

  // Регистрируем наш кастомный компонент для UserInfo
  ComponentsRegistry.instance.register(UserInfoViewModel, CustomUserInfo);

  // Регистрируем компонент для футера
  ComponentsRegistry.instance.register(VacationInfoViewModel, VacationInfo);

  // Переопределяем создание модели представления поповера, чтобы добавить туда футер
  const popoverDefaultFactory = container.get(IUserInfoPopoverViewModelFactory$);

  container
    .rebind(IUserInfoPopoverViewModelFactory$)
    .toConstantValue((args: UserInfoViewModelFactoryParams) => {
      // В args может быть задан type - название типа обработчика, который занимается получением данных для UserInfo на сервере.
      // Это значение можно использовать чтобы определить для какого сценария запрашивается UserInfo и какие данные содержит.

      const userInfo = popoverDefaultFactory(args);
      userInfo.footer = new VacationInfoViewModel(userInfo);
      return userInfo;
    });

  // Переопределяем создание модели представления самого компонента, чтобы добавить туда футер
  const defaultFactory = container.get(IUserInfoViewModelFactory$);

  container
    .rebind(IUserInfoViewModelFactory$)
    .toConstantValue((args: UserInfoViewModelFactoryParams) => {
      // В args может быть задан type - название типа обработчика, который занимается получением данных для UserInfo на сервере.
      // Это значение можно использовать чтобы определить для какого сценария запрашивается UserInfo и какие данные содержит.

      const userInfo = defaultFactory(args);
      userInfo.footer = new VacationInfoViewModel(userInfo);
      return userInfo;
    });
}
