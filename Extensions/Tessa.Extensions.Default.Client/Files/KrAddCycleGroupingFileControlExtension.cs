#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Files;

namespace Tessa.Extensions.Default.Client.Files
{
    /// <summary>
    /// Управляет появлением виртуальных файлов "версий" при включении и выключении группировки по циклам согласования
    /// </summary>
    public class KrAddCycleGroupingFileControlExtension :
        FileControlExtension
    {
        #region Fileds

        private readonly IKrTypesCache krTypesCache;
        private readonly ICardCache cardCache;

        #endregion

        #region Constructors

        public KrAddCycleGroupingFileControlExtension(IKrTypesCache krTypesCache, ICardCache cardCache)
        {
            this.cardCache = cardCache;
            this.krTypesCache = krTypesCache;
        }

        #endregion

        #region Base Overrides

        public override async Task Initialized(IFileControlExtensionContext context)
        {
            ICardModel? model = context.TryGetCardModel();
            if (model?.InSpecialMode() != false)
            {
                return;
            }

            Card card = model.Card;
            if (card is null
                || (await KrComponentsHelper.GetKrComponentsAsync(card, this.krTypesCache, context.CancellationToken)).HasNot(KrComponents.Routes))
            {
                return;
            }

            var cycleGrouping = context.Groupings.TryGet(CycleFileGroupingNames.Cycle);

            // Добавляем группировку/фильтр
            if (cycleGrouping is null)
            {
                cycleGrouping = new CycleGrouping(CycleFileGroupingNames.Cycle, "$UI_Controls_FilesControl_GroupingByCycle");
                context.Groupings.Add(cycleGrouping);
            }

            // Если по умолчанию не выбрана группировка по циклу согласования, то надо убрать виртуальные файлы версий.
            if (context.Control.SelectedGrouping is not CycleGrouping)
            {
                for (int i = context.Control.Files.Count - 1; i > -1; i--)
                {
                    var file = context.Control.Files[i];
                    if (file.Info.TryGet<object?>(CycleGroupingHelper.CycleIDKey) is null)
                    {
                        continue;
                    }

                    bool removeFile = true;
                    if (card.TryGetFiles() is { Count: > 0 } cardFiles)
                    {
                        foreach (CardFile cardFile in cardFiles)
                        {
                            if (cardFile.RowID == file.ID)
                            {
                                removeFile = cardFile.IsVirtual;
                                break;
                            }
                        }
                    }

                    if (removeFile)
                    {
                        context.Control.Files.RemoveAt(i);
                    }
                }
            }

            var sections = card.TryGetSections();
            if (sections is null)
            {
                return;
            }

            int? state = sections.TryGet("KrApprovalCommonInfoVirtual")?.RawFields.TryGet<int?>("StateID");

            int? currentCycle = sections.TryGet("KrApprovalHistoryVirtual")?.TryGetRows() is { Count: > 0 } rows
                ? rows.Max(p => p.TryGet<int>("Cycle")) // TryGet вернёт 0
                : null;

            context.Control.ContainerFileAdded += (sender, args) =>
            {
                if (state.HasValue
                    && state.Value != KrState.Draft
                    && args.File.Origin is not null)
                {
                    if (currentCycle > 0)
                    {
                        args.File.Info[CycleGroupingHelper.CycleIDKey] = currentCycle.Value;
                    }
                }
            };

            context.Control.PropertyChanged += async (sender, args) =>
            {
                if (args.PropertyName == nameof(IFileControl.SelectedGrouping))
                {
                    var control = (IFileControl) NotNullOrThrow(sender);
                    if (control.SelectedGrouping is CycleGrouping)
                    {
                        var currentMode = control.Info.TryGet<CycleFilesMode?>(CycleGroupingHelper.CycleGroupingModeKey) ?? CycleFilesMode.ShowAllCycleFiles;
                        await CycleGroupingUIHelper.SwitchFilesVisibilityAsync(control, card, currentCycle, currentMode);
                    }
                    else
                    {
                        await CycleGroupingUIHelper.RestoreFilesListAsync(control, card);
                    }
                }
            };

            CycleFilesMode? currentCycleMode = null;
            if (context.Control.Name is not null
                && card.ID == UIContext.Current.CardEditor?.CardModel?.Card.ID
                && UIContext.Current.Info
                    .TryGet<Dictionary<string, CycleFilesMode>>(CycleGroupingHelper.CycleGroupingModeKey)
                    ?.TryGetValue(context.Control.Name, out CycleFilesMode modeFromContext) == true)
            {
                currentCycleMode = modeFromContext;
            }

            if (!currentCycleMode.HasValue)
            {
                var mode = model.Info.TryGet<CycleFilesMode?>(CycleGroupingHelper.CycleGroupingModeKey);
                if (!mode.HasValue
                    && await this.cardCache.Cards.GetAsync(DefaultCardTypes.KrSettingsTypeName, context.CancellationToken) is { IsSuccess: true } krSettings)
                {
                    Card settings = krSettings.GetValue();

                    // Проверим, что тип карточки/документа включён в настройки
                    // Читаем тип карточки/документа ровно один раз
                    Guid docCardTypeID = sections.TryGet("DocumentCommonInfo")?.RawFields.TryGet<Guid?>("DocTypeID") ?? card.TypeID;

                    Guid? settingsRowID = null;
                    if (state.HasValue &&
                        settings.Sections["KrSettingsCycleGrouping"]
                            .Rows.Any(p =>
                            {
                                var typeID = p.Get<Guid>("TypeID");
                                if (typeID == docCardTypeID)
                                {
                                    settingsRowID = p.Get<Guid>("TypesRowID");

                                    // Проверим состояния
                                    if (settings.Sections["KrSettingsCycleGroupingStates"]
                                        .Rows.Where(q => q.Get<Guid>("TypesRowID") == settingsRowID)
                                        .Any(q => q.Get<int>("StateID") == state.Value))
                                    {
                                        return true;
                                    }
                                }

                                return false;
                            }))
                    {
                        CardRow? settingsRow = null;
                        foreach (CardRow row in settings.Sections["KrSettingsCycleGroupingTypes"].Rows)
                        {
                            if (row.RowID == settingsRowID)
                            {
                                settingsRow = row;
                                break;
                            }
                        }

                        currentCycleMode = (CycleFilesMode?) settingsRow?.TryGet<int?>("DefaultModeID");
                    }
                }
            }

            if (currentCycleMode.HasValue)
            {
                context.Control.Info[CycleGroupingHelper.CycleGroupingModeKey] = currentCycleMode;

                if (context.Control.SelectedGrouping is null)
                {
                    await context.Control.SelectGroupingAsync(cycleGrouping, context.CancellationToken);
                }
            }
        }

        #endregion
    }
}
