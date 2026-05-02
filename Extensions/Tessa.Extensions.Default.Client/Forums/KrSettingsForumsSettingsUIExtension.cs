#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using Tessa.Cards;
using Tessa.Extensions.Default.Client.UI;
using Tessa.Extensions.Default.Client.Workflow.Wf;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.Wf;
using Tessa.Forums;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;

namespace Tessa.Extensions.Default.Client.Forums
{
    /// <summary>
    /// Визуальное расширение для карточки настроек типов карточек и типа документов,
    /// связанных с системой форумов.
    /// </summary>
    public sealed class KrSettingsForumsSettingsUIExtension : CardUIExtension
    {
        #region Base Overrides

        public override async Task Initialized(ICardUIExtensionContext context)
        {
            var model = context.Model;
            if (model.InSpecialMode())
            {
                return;
            }

            var hasForumLicense = context.Card.TryGetInfo()?.TryGet<bool>(ForumHelper.LicenseWarningFlag) != true;

            var cardTypeID = model.CardType.ID;
            if (cardTypeID == DefaultCardTypes.KrSettingsTypeID)
            {
                if (context.Card.TryGetSections()?.TryGet(WfHelper.CardTypeSettingsSection) is { } section
                    && model.Controls.TryGet<GridViewModel>(KrTypesUIHelper.TypesControl) is { } grid)
                {
                    // скрываем / отображаем контролы, связанные с использованием резолюции
                    grid.RowInvoked += (s, e) =>
                    {
                        if (e.Action is GridRowAction.Inserted or GridRowAction.Opening)
                        {
                            if (e.RowModel.Blocks.TryGet(KrTypesUIHelper.UseForumBlock) is { } forumsBlock)
                            {
                                var useForumFirst = e.Row.Get<bool>(ForumHelper.UseForumField);
                                WfUIHelper.SetControlVisibility(forumsBlock, ForumHelper.UseForumSuffix, useForumFirst);
                                EventHandler<CardFieldChangedEventArgs> fieldChangedHandler = (s2, e2) =>
                                {
                                    if (e2.FieldName == ForumHelper.UseForumField)
                                    {
                                        var useForum = (bool) NotNullOrThrow(e2.FieldValue);

                                        ((ICardFieldContainer) NotNullOrThrow(s2)).Fields[ForumHelper.UseDefaultDiscussionTabField] = BooleanBoxes.Box(useForum);
                                        WfUIHelper.SetControlVisibility(forumsBlock, ForumHelper.UseForumSuffix, useForum);
                                    }
                                };

                                var row = e.Row;
                                row.FieldChanged += fieldChangedHandler;
                                forumsBlock.Form.Closed += (_, _) => row.FieldChanged -= fieldChangedHandler;
                            }

                            if (!hasForumLicense
                                && e.RowModel.Controls.TryGet(ForumHelper.LicenseWarningControlAlias) is { } warningLabel)
                            {
                                warningLabel.ControlVisibility = Visibility.Visible;
                                e.RowModel.MainFormWithBlocks.RearrangeSelf();
                            }
                        }
                    };

                    // сбрасываем состояние полей, когда галка "использовать систему форумов" очищается
                    var cardTypeRows = section.Rows;
                    foreach (var row in cardTypeRows)
                    {
                        row.FieldChanged += OnRowFieldChanged;
                    }

                    cardTypeRows.ItemChanged += static (s, e) =>
                    {
                        // чтобы не было утечек памяти при удалении строки внутри обработчика нельзя использовать замыканий
                        if (e is { Action: ListStorageAction.Insert, Item: { } item })
                        {
                            item.FieldChanged += OnRowFieldChanged;
                        }
                    };

                    static void OnRowFieldChanged(object? s, CardFieldChangedEventArgs e)
                    {
                        if (e.FieldName == ForumHelper.UseForumField)
                        {
                            var useForum = (bool) NotNullOrThrow(e.FieldValue);
                            ((ICardFieldContainer) NotNullOrThrow(s)).Fields[ForumHelper.UseDefaultDiscussionTabField] = BooleanBoxes.Box(useForum);
                        }
                    }
                }
            }
            else if (cardTypeID == DefaultCardTypes.KrDocTypeTypeID)
            {
                if (context.Card.TryGetSections()?.TryGet(WfHelper.DocTypeSettingsSection) is { } section
                    && model.Blocks.TryGet(KrTypesUIHelper.UseForumBlock, out var useForumBlock))
                {
                    // скрываем / отображаем контролы, связанные с использованием резолюции
                    var useForumFirst = section.RawFields.Get<bool>(ForumHelper.UseForumField);
                    WfUIHelper.SetControlVisibility(useForumBlock, ForumHelper.UseForumSuffix, useForumFirst);

                    section.FieldChanged += (s, e) =>
                    {
                        if (e.FieldName == ForumHelper.UseForumField)
                        {
                            // сбрасываем состояние полей, когда галка "использовать систему форумов" очищается
                            var useForum = (bool) NotNullOrThrow(e.FieldValue);
                            ((ICardFieldContainer) NotNullOrThrow(s)).Fields[ForumHelper.UseDefaultDiscussionTabField] = BooleanBoxes.Box(useForum);

                            // скрываем / отображаем контролы, связанные с использованием резолюции
                            WfUIHelper.SetControlVisibility(useForumBlock, ForumHelper.UseForumSuffix, useForum);
                        }
                    };
                }
                
                if (!hasForumLicense
                    && context.Model.Controls.TryGet(ForumHelper.LicenseWarningControlAlias) is { } warningLabel)
                {
                    warningLabel.ControlVisibility = Visibility.Visible;
                    warningLabel.Block.Form.RearrangeSelf();
                }
            }
        }

        #endregion
    }
}
