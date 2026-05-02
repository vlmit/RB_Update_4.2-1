using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Metadata;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Console.ConvertConfiguration
{
    public class ConvertTypesService
    {
        public static async ValueTask<string> RepairAndConvertAsync(
            string typeJson,
            SchemeTableCollection tables,
            CancellationToken cancellationToken = default)
        {
            var repairedJson = await RepairReferencesAsync(typeJson, tables, cancellationToken);
            return await ConvertVersionAsync(repairedJson, cancellationToken);
        }

        public static async Task<string> ConvertVersionAsync(string json, CancellationToken cancellationToken = default)
        {
            var cardType = await CardSerializableObject.DeserializeFromJsonAsync<CardType>(json, null, cancellationToken);
            if (cardType is null)
            {
                throw new InvalidOperationException("Can't deserialize CardType json string.");
            }
            return await cardType.SerializeToJsonAsync(cancellationToken: cancellationToken);
        }

        public static ValueTask<string> RepairReferencesAsync(
            string typeJson,
            SchemeTableCollection tables,
            CancellationToken cancellationToken = default)
        {
            // Подготовить клолнки виртуальных схем
            var metadataStorage = StorageHelper.DeserializeFromTypedJson(typeJson);

            var cardTypeSections = metadataStorage?.TryGet<object>(nameof(CardType.CardTypeSections));
            if (cardTypeSections is not IList { Count: > 0 } sectionList)
            {
                return new(metadataStorage is not null ? StorageHelper.SerializeToTypedJson(metadataStorage, true) : typeJson);
            }

            foreach (var sectionObj in sectionList)
            {
                if (sectionObj is not Dictionary<string, object?> sectionStorage)
                {
                    continue;
                }

                var sectionColumns = sectionStorage.TryGet<IList<object?>>(nameof(CardTypeSection.Columns));

                if (sectionColumns is not { Count: > 0 })
                {
                    continue;
                }

                foreach (var columnObj in sectionColumns)
                {
                    if (columnObj is Dictionary<string, object?> columnStorage)
                    {
                        var columnType = (CardMetadataColumnType) columnStorage.Get<int>(nameof(CardTypeSectionColumn.ColumnType));
                        switch (columnType)
                        {
                            case CardMetadataColumnType.Complex:
                                ProcessComplexColumn(tables, columnStorage, sectionList);
                                break;
                            case CardMetadataColumnType.Physical:
                                break;
                            case CardMetadataColumnType.Reference:
                            default:
                                throw ArgumentOutOfRange(columnType);
                        }
                    }
                }
            }

            return new(metadataStorage is not null ? StorageHelper.SerializeToTypedJson(metadataStorage, true) : typeJson);
        }

        private static void ProcessComplexColumn(SchemeTableCollection tables, Dictionary<string, object?> complexColumnStorage, IList sectionList)
        {
            var refTableID = complexColumnStorage.TryGet<Guid?>(nameof(CardTypeSectionColumn.ReferencedTableID));
            if (refTableID is not null)
            {
                return;
            }

            var refTableName = complexColumnStorage.Get<string>("ReferencedSectionName");
            Dictionary<string, object?>? localRefSectionStorage = null;
            SchemeTable? schemeTable = null;
            if (!string.IsNullOrEmpty(refTableName))
            {
                // Вначале проверка в локальных секциях
                foreach (var localSectionObj in sectionList)
                {
                    if (localSectionObj is not Dictionary<string, object?> localSectionStorage)
                    {
                        continue;
                    }

                    var localSectionName = localSectionStorage.Get<string>(nameof(CardTypeSection.Name));
                    if (localSectionName == refTableName)
                    {
                        localRefSectionStorage = localSectionStorage;
                        var localRefSectionID = localSectionStorage.Get<Guid>(nameof(CardTypeSection.ID));
                        complexColumnStorage[nameof(CardTypeSectionColumn.ReferencedTableID)] = localRefSectionID;
                    }
                }

                // Затем в схеме
                if (localRefSectionStorage is null)
                {
                    schemeTable = tables.FirstOrDefault(x => x.Name == refTableName);

                    // Если и в схеме такой таблицы нет, то связь исправить невозможно, это ошибка.
                    if (schemeTable is null)
                    {
                        var complexColumnID = complexColumnStorage.TryGet<Guid?>(nameof(CardTypeSectionColumn.ID));
                        var complexColumnName = complexColumnStorage.TryGet<string?>(nameof(CardTypeSectionColumn.Name));
                        throw new InvalidOperationException(
                            $"Can't resolve reference table \"{refTableName}\" for complex column \"{complexColumnName}\" with ID \"{complexColumnID}\". Referenced table must exists in file scheme or in virtual sections.");
                    }

                    complexColumnStorage[nameof(CardTypeSectionColumn.ReferencedTableID)] = schemeTable.ID;
                }
            }

            var refColumns = complexColumnStorage.TryGet<IList<object?>>(nameof(CardTypeSectionColumn.ReferencedColumns));
            if (refColumns is not { Count: > 0 })
            {
                return;
            }

            {
                foreach (var refColumnObj in refColumns)
                {
                    if (refColumnObj is Dictionary<string, object?> refColumnStorage)
                    {
                        var refColumnType =
                            (CardMetadataColumnType) refColumnStorage.Get<int>(nameof(CardTypeSectionColumn.ColumnType));

                        switch (refColumnType)
                        {
                            case CardMetadataColumnType.Physical:
                                // Присвоение типа Reference
                                refColumnStorage[nameof(CardTypeSectionColumn.ColumnType)] = Int32Boxes.Box((int) CardMetadataColumnType.Reference);

                                var refColumnName = refColumnStorage.Get<string>(nameof(CardTypeSectionColumn.Name));
                                var complexColumnName = complexColumnStorage.Get<string>(nameof(CardTypeSectionColumn.Name));
                                var refColumnNameWithoutPrefix = refColumnName?.Remove(0, complexColumnName?.Length ?? 0);

                                // Если ссылка на локальную секцию
                                if (localRefSectionStorage is not null)
                                {
                                    var sectionType = (SchemeTableContentType) localRefSectionStorage.Get<int>(nameof(CardTypeSection.TableType));
                                    if (sectionType is SchemeTableContentType.Entries &&
                                        refColumnNameWithoutPrefix == "ID")
                                    {
                                        var sectionID = localRefSectionStorage.Get<Guid>("ID");
                                        refColumnStorage[nameof(CardTypeSectionColumn.ReferencedColumnID)] =
                                            CardTypeSectionsHelper.GetReferenceIDForIDColumn(sectionID);
                                    }
                                    else if (sectionType is SchemeTableContentType.Collections or SchemeTableContentType.Hierarchies &&
                                             refColumnNameWithoutPrefix == "RowID")
                                    {
                                        var sectionID = localRefSectionStorage.Get<Guid>("ID");
                                        refColumnStorage[nameof(CardTypeSectionColumn.ReferencedColumnID)] =
                                            CardTypeSectionsHelper.GetReferenceIDForRowIDColumn(sectionID);
                                    }
                                    else
                                    {
                                        var localColumns = localRefSectionStorage.TryGet<IList<object?>>(nameof(CardTypeSection.Columns));

                                        if (localColumns is { Count: > 0 })
                                        {
                                            foreach (var localColumnObj in localColumns)
                                            {
                                                if (localColumnObj is not Dictionary<string, object?> localColumnStorage)
                                                {
                                                    continue;
                                                }

                                                var localColumnName = localColumnStorage.Get<string>(nameof(CardTypeSectionColumn.Name));
                                                if (localColumnName == refColumnNameWithoutPrefix)
                                                {
                                                    refColumnStorage[nameof(CardTypeSectionColumn.ReferencedColumnID)] =
                                                        localColumnStorage.Get<Guid>(nameof(CardTypeSectionColumn.ID));
                                                }
                                            }
                                        }
                                    }
                                }
                                // Иначе попытка сделать связь с таблицей схемы
                                else
                                {
                                    var schemeColumn = schemeTable?.Columns.FirstOrDefault(x => x.Name == refColumnNameWithoutPrefix);

                                    if (schemeColumn is not null)
                                    {
                                        refColumnStorage[nameof(CardTypeSectionColumn.ReferencedColumnID)] = schemeColumn.ID;
                                    }
                                    else
                                    {
                                        // Если сделать связь не получилось, то это физическая колонка
                                        refColumnStorage[nameof(CardTypeSectionColumn.ColumnType)] = Int32Boxes.Box((int) CardMetadataColumnType.Physical);
                                    }
                                }

                                break;
                            case CardMetadataColumnType.Reference:
                                break;
                            case CardMetadataColumnType.Complex:
                                break;
                            default:
                                throw ArgumentOutOfRange(refColumnType);
                        }
                    }
                }
            }
        }
    }
}
