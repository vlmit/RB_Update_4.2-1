import { ValidationResult } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  ICardService,
  ICardService$,
  IViewRepository,
  IViewRepository$,
  ViewCriteriaOperators,
  ViewRequest,
  CardGetRequest,
  ViewResult
} from '@tessa/platform';
import { TileExtension, ITileGlobalExtensionContext, Tile, TileGroups } from 'tessa/ui/tiles';
import { showNotEmpty, showMessage } from 'tessa/ui';

/**
 * В этом расширении демонстрируется:
 * - Тайл-группа на левой панели.
 * - Запрос карточки с сервера.
 * - Запрос данных представления с сервера.
 * - Заполнение параметров поиска в запросе к представлению.
 * - Доступ к результату запроса представления.
 *
 * Результат работы расширения:
 * На левую панель добавляет групповой тайл - “Запросы”. По клику показываются дочерние тайлы - “Запрос карточки” и “Запрос представления”.
 * При клике на “Запрос карточки” происходит вызов сервера, получается карточка прав, и необработанное содержимое показывается в модальном окне.
 * Аналогично, с помощью CardService можно делать и другие реквесты (request, get, store и т.д.).
 * При клике на “Запрос представления” происходит вызов сервера, получаются данные списка контрагентов, и необработанное содержимое показывается в модальном окне.
 */
@extension()
export class RequestTileExtension extends TileExtension {
  constructor(
    @inject(ICardService$) private _cardService: ICardService,
    @inject(IViewRepository$) private _viewRepository: IViewRepository
  ) {
    super();
  }

  override async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    // получаем доступ к левой боковой панели
    const panel = context.workspace.leftPanel;

    // создаем тайл-группу
    const groupTile = new Tile({
      name: 'TestGroupRequestTileExtension',
      caption: 'Запросы',
      icon: 'ta icon-thin-100',
      contextSource: panel.contextSource,
      group: TileGroups.CardsTop,
      order: 100
    });

    // создаем содержимое тайл-группы
    const cardRequestTile = new Tile({
      name: 'TestCardRequestTileExtension',
      caption: 'Запрос карточки',
      icon: 'ta icon-thin-100',
      contextSource: panel.contextSource,
      group: TileGroups.CardsTop,
      order: 1,
      command: () => this.cardRequestCommand()
    });
    const viewRequestTile = new Tile({
      name: 'TestViewRequestTileExtension',
      caption: 'Запрос представления',
      icon: 'ta icon-thin-100',
      contextSource: panel.contextSource,
      group: TileGroups.CardsTop,
      order: 2,
      command: () => this.viewRequestCommand()
    });

    // добавляем созданные тайлы в группу
    groupTile.tiles.push(viewRequestTile, cardRequestTile);

    // добавляем тайл-группу в левую боковую панель
    panel.tiles.push(groupTile);
  }

  private async cardRequestCommand() {
    const cardGetRequest = new CardGetRequest();

    // ID карточки типа "Правила доступа" с названием "Default access rules"
    cardGetRequest.cardId = '9fac55ba-afab-4a40-8789-79c97d96ace2';
    cardGetRequest.cardTypeId = 'fa9dbdac-8708-41df-bd72-900f69655dfa';
    cardGetRequest.cardTypeName = 'KrPermissions';

    // при запросе все расширения CardGetExtension будут вызываны
    const cardGetResponse = await this._cardService.get(cardGetRequest);
    if (!cardGetResponse.validationResult.isSuccessful) {
      // если в validationResult есть ошибки, то показываем их
      await showNotEmpty(cardGetResponse.validationResult.build());
      return;
    }

    // берем storage карточки как строку
    const cardStr = JSON.stringify(cardGetResponse.card.getStorage());
    await showMessage(cardStr.slice(0, 100));
  }

  private async viewRequestCommand() {
    // пытаемся найти представление "Контрагенты"
    const partnersView = await this._viewRepository.getByName('Partners');
    if (!partnersView) {
      return;
    }

    const viewMetadata = await partnersView.getMetadata();
    const request = new ViewRequest(viewMetadata);

    // добавляем параметр фильтрации по имени контрагента (для примера, что имя не равно null)
    request.addParameter('Name', b =>
      b.addCriteria(ViewCriteriaOperators.IsNotNull).asRequestParameter()
    );

    let result: ViewResult;
    try {
      // в getData будут добавлены параметры currentUserId и locale
      result = await partnersView.getData(request);
    } catch (err) {
      await showNotEmpty(ValidationResult.fromError(err));
      return;
    }

    // конвертируем строки в ViewResultRow[] для удобства
    const rows = result.getRowsAsMap();

    const text: string[] = [];
    rows.forEach(row => {
      const rowText: string[] = [];
      row.forEach((v, k) => {
        rowText.push(`${k}: ${v}`);
      });
      text.push(rowText.join(';'));
    });

    await showMessage(text.join('\n'));
  }
}
