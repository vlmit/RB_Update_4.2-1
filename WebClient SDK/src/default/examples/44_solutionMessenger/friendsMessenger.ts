import { MessengerBase, MessengerParamsType } from 'tessa/ui/messengers';

/**
 * Инстанс мессенджера для проектного решения, который описывает логику этого мессенджера.
 * @remarks Вся общая логика построения ссылок, эдитора и валидации определена в базовом классе. Если логика значительно отличается, то можно реализовывать интерфейс {@link MessengerInstance}.
 */
export class FriendsMessenger extends MessengerBase {
  static code = 'fr';

  //#region  ctor

  constructor() {
    // Указываем код мессенджера, его отображаемое название, иконку и тип параметров - поддерживаем как номер телефона, так и юзернейм
    // Можно добавить отдельный спрайт иконок формата svg с иконкой мессенджера и указать в теме (см файл messengers-icons.svg в папке wwwroot/icons сервиса)
    super(FriendsMessenger.code, 'Друзья', 'm-roles', MessengerParamsType.All);
  }

  //#endregion

  //#region methods

  // определяем, как строится ссылка по номеру телефона
  protected override getPhoneLink(phone: string): string {
    return `https://friends.messenger/${phone}`;
  }

  // определяем, как строится ссылка по юзернейму
  protected override getUsernameLink(username: string): string {
    return `https://friends.messenger/${username}`;
  }

  // Можно переопределить названия поля в эдиторе настроек мессенджера, иначе будут использованы названия полей по умолчанию
  protected override usernameFieldCaption = 'Имя друга';

  protected override phoneFieldCaption = 'Номер телефона друга';

  //#endregion
}
