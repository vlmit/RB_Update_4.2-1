import {
  Guid,
  TypedField,
  ValidationResult,
  ValidationResultBuilder,
  ValidationResultType
} from '@tessa/core';
import { localize } from '@tessa/application';
import {
  Card,
  CardRow,
  CardRowState,
  IKrType,
  IKrTypesCache,
  IViewRepository,
  ViewCriteriaOperators,
  ViewRequest,
  ViewRequestParameterBuilder
} from '@tessa/platform';
import { UIContext } from 'tessa/ui';

export const CompiledCardTypes = [
  '2fa85bb3-bba4-4ab6-ba97-652106db96de', // KrStageTemplates
  '66cd517b-5423-43db-8374-f50ec0d967eb', // KrStageCommonMethods
  '9ce8e9f4-cbf0-4b5f-a569-b508b1fd4b3a', // KrStageGroup
  '61420fa1-cc1f-47cb-b0bb-4ea8ee77f51a' // KrSecondaryProcess
];

/**
 * Идентификатор роли "Вычисляемые исполнители".
 */
export const sqlApproverRoleId = 'cd4d4a0d-414f-478d-a226-319aa8417f88';

/**
 * Название роли "Вычисляемые исполнители".
 */
export const sqlApproverRoleName = '$KrProcess_SqlPerformersRole';

/**
 * Идентификатор карточки вида задания "Рекомендательное согласование".
 */
export const advisoryTaskKindId = '2e6c5d3e-d408-4f98-8a55-e9d1316bf2cc';

/**
 * Метка добавляемая к строке как признак наличия доп. согласующих.
 */
export const additionalApproverMark = '(+) ';

export async function sendCompileRequest(compileFlag: string): Promise<void> {
  const context = UIContext.current;
  const editor = context.cardEditor;
  if (!editor || !editor.cardModel) {
    return;
  }

  if (await editor.cardModel.hasChanges()) {
    const success = await editor.saveCard(context);
    if (!success) {
      return;
    }
  }

  await editor.saveCard(context, {
    [compileFlag]: TypedField.trueBoolean
  });
}

/**
 * Возвращает эффективные настройки для типа карточки или типа документа {@link IKrType} по карточке card, которая загружена со всеми секциями, или null, если настройки нельзя получить.
 *
 * @param krTypesCache Кэш типов карточек.
 * @param card Карточка, загруженная со всеми секциями.
 * @param cardTypeId Идентификатор типа карточки.
 * @param validationResult Объект, в который записываются сообщения об ошибках, или null, если сообщения никуда не записываются.
 * @returns Эффективные настройки для типа карточки или типа документа или null, если настройки нельзя получить.
 */
export async function tryGetKrType(
  krTypesCache: IKrTypesCache,
  card: Card,
  cardTypeId: string,
  validationResult: ValidationResultBuilder | null = null
): Promise<IKrType | null> {
  const krCardType = (await krTypesCache.getCardTypes()).find(x => Guid.equals(x.id, cardTypeId));
  if (krCardType == null) {
    // карточка может не входить в типовое решение, тогда возвращается null
    // при этом нельзя кидать ошибку в ValidationResult, иначе любое действие с такой карточкой будет неудачным
    return null;
  }

  let result: IKrType | null = krCardType;
  if (krCardType.useDocTypes) {
    const section = card.sections.tryGet('DocumentCommonInfo');
    if (section) {
      const value = section.fields.tryGetField('DocTypeID');
      if (value) {
        result =
          (await krTypesCache.getDocTypes()).find(x =>
            Guid.equals(x.id, TypedField.tryGetString(value))
          ) ?? null;
        if (!result) {
          if (validationResult) {
            validationResult.add(
              ValidationResult.fromText(
                localize('$KrMessages_UnableToFindTypeWithID', TypedField.get(value)),
                ValidationResultType.Error
              )
            );
          }

          return null;
        }
      } else {
        if (validationResult) {
          validationResult.add(
            ValidationResult.fromText(
              localize('$KrMessages_DocTypeNotSpecified', krCardType.name, cardTypeId, card.id),
              ValidationResultType.Error
            )
          );
        }

        return null;
      }
    }
  }

  return result;
}

/**
 * Возвращает значение, показывающее, может ли указанный тип карточки содержать шаблоны этапов.
 *
 * @param typeId Идентификатор типа карточки.
 * @returns Значение true, если указанный тип карточки может содержать шаблоны этапов, иначе - false.
 */
export function designTimeCard(typeId: string): boolean {
  return (
    typeId === '2fa85bb3-bba4-4ab6-ba97-652106db96de' || // KrStageTemplates
    typeId === '61420fa1-cc1f-47cb-b0bb-4ea8ee77f51a' // KrSecondaryProcess
  );
}

/**
 * Возвращает значение, показывающее, является ли указанный тип карточки типом карточки в котором выполняется маршрут.
 *
 * @param typeId Идентификатор типа карточки.
 * @returns Значение true, если указанный тип карточки может содержать выполняющийся маршрут, иначе - false.
 */
export function runtimeCard(typeId: string): boolean {
  return !designTimeCard(typeId);
}

/**
 * Возвращает значение, показывающее, возможен ли пропуск указанного этапа.
 *
 * @param row Строка этапа, для которого выполняется проверка.
 * @returns Значение, показывающее, возможен ли пропуск указанного этапа.
 */
export function canBeSkipped(row: CardRow): boolean {
  return row.tryGet<string>('BasedOnStageTemplateID') != null && !!row.tryGet('CanBeSkipped');
}

/**
 * Выполняет пропуск этапа.
 *
 * @param row Строка этапа, пропуск которого выполняется.
 * @returns Значение true, если этап был пропущен, иначе - false.
 */
export function skipStage(row: CardRow): boolean {
  if (canBeSkipped(row)) {
    if (row.state === CardRowState.Deleted) {
      row.state = CardRowState.Modified;
    }

    row.set('Skip', TypedField.trueBoolean);
    return true;
  }

  return false;
}

/**
 * Проверяет, содержит ли строка метку о наличии доп. согласующих.
 * @param str Проверяемая строка.
 * @returns `true`, если строка содержит метку, иначе - `false`.
 */
export function existsMarkName(str: string | null | undefined): boolean {
  return !!str && str.startsWith(additionalApproverMark);
}

/**
 * Добавляет метку о наличии доп. согласующих, если она отсутствует, в указанную строку.
 * @param str Строка, в которую требуется добавить метку.
 * @returns Результирующая строка.
 */
export function markName(str: string | null | undefined): string | null | undefined {
  if (!str || str.length === 0 || str.startsWith(additionalApproverMark)) {
    return str;
  }

  return str[0] === '$' ? `${additionalApproverMark}{${str}}` : `${additionalApproverMark}${str}`;
}

/**
 * Удаляет метку о доп. согласующих из указанной строки, если она присутствует.
 * @param str Строка, из которой требуется удалить метку.
 * @returns Результирующая строка.
 */
export function unmarkName(str: string | null | undefined): string | null | undefined {
  if (str && str.startsWith(additionalApproverMark)) {
    let from = 4;
    let len = str.length;
    // additionalApproverMark{} - Если расширенная локализация, скобки надо удалить.
    if (str.length >= 6 && str[4] === '{' && str[str.length - 1] === '}') {
      from++;
      len -= 1;
    }
    return str.substring(from, len);
  }

  return str;
}

/**
 * Возвращает название вида задания.
 * @param viewRepository {@link IViewRepository}
 * @param id Идентификатор вида задания.
 * @returns Название вида задания или значение `null`, если его не удалось получить.
 */
export async function getKindCaption(
  viewRepository: IViewRepository,
  id: string
): Promise<string | null> {
  const viewName = 'TaskKinds';
  const taskKindsView = await viewRepository.getByName(viewName);

  if (!taskKindsView) {
    throw new Error(`Can not find view with alias "${viewName}" at view service.`);
  }

  const viewMetadata = await taskKindsView.getMetadata();
  const request = new ViewRequest(viewMetadata);

  const idParam = new ViewRequestParameterBuilder()
    .withMetadata(viewMetadata.parameters.get('ID')!)
    .addCriteria(ViewCriteriaOperators.EqualsTo, '', id)
    .asRequestParameter();

  request.parameters.push(idParam);

  const result = await taskKindsView.getData(request);

  if (!result.rows || result.rows.length === 0) {
    return null;
  }

  const row = result.rows[0];
  return row[1] as string;
}
