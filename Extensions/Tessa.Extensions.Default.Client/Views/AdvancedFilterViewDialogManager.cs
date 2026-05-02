#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tessa.Cards;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views.Parameters;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <inheritdoc cref="IAdvancedFilterViewDialogManager"/>
    /// <param name="createDialogFormFuncAsync"><inheritdoc cref="CreateDialogFormFuncAsync" path="/summary"/></param>
    /// <param name="advancedCardDialogManager"><inheritdoc cref="IAdvancedCardDialogManager" path="/summary"/></param>
    /// <param name="parameterFormatter"><inheritdoc cref="IViewParameterFormatter" path="/summary"/></param>
    public class AdvancedFilterViewDialogManager(
        CreateDialogFormFuncAsync createDialogFormFuncAsync,
        IAdvancedCardDialogManager advancedCardDialogManager,
        IViewParameterFormatter parameterFormatter)
        :
            IAdvancedFilterViewDialogManager
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFuncAsync = NotNullOrThrow(createDialogFormFuncAsync);

        private readonly IAdvancedCardDialogManager advancedCardDialogManager = NotNullOrThrow(advancedCardDialogManager);

        private readonly IViewParameterFormatter parameterFormatter = NotNullOrThrow(parameterFormatter);

        #endregion

        #region IAdvancedViewParametersDialogManager Members

        /// <inheritdoc/>
        public virtual async Task OpenAsync(
            FilterViewDialogDescriptor descriptor,
            IViewParameters parameters,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(descriptor);
            ThrowIfNull(parameters);

            var dialogCardModel = await this.CreateDialogCardModelAsync(
                descriptor.DialogName,
                descriptor.FormAlias,
                cancellationToken);

            await this.ShowDialogAsync(
                descriptor,
                parameters,
                dialogCardModel,
                cancellationToken);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Создаёт модель карточки диалога, содержащего параметры представления.
        /// </summary>
        /// <param name="dialogName">Имя типа диалога.</param>
        /// <param name="formAlias">Алиас формы диалога или <see langword="null"/>, если требуется создать форму для первой вкладки типа диалога.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Модель карточки диалога.</returns>
        /// <exception cref="InvalidOperationException">При создании модели карточки диалога произошла ошибка.</exception>
        protected async ValueTask<ICardModel> CreateDialogCardModelAsync(
            string dialogName,
            string? formAlias,
            CancellationToken cancellationToken = default)
        {
            var (form, cardModel) = await this.createDialogFormFuncAsync(
                dialogName,
                formAlias,
                modifyModelAsync: static (newCardModel, _) =>
                {
                    newCardModel.Card.Version = 1;
                    newCardModel.Flags |= CardModelFlags.IgnoreChanges;

                    return ValueTask.CompletedTask;
                },
                cancellationToken: cancellationToken);

            if (form is null
                || cardModel is null)
            {
                throw new InvalidOperationException(
                    $"Failed to create dialog. Dialog name: \"{dialogName}\". Form alias: \"{FormatNullable(formAlias)}\".");
            }

            cardModel.MainForm = form;

            return cardModel;
        }

        /// <summary>
        /// Отображает диалог, содержащий параметры фильтрации представления.
        /// </summary>
        /// <param name="descriptor"><inheritdoc cref="FilterViewDialogDescriptor" path="/summary"/></param>
        /// <param name="parameters"><inheritdoc cref="IViewParameters" path="/summary"/></param>
        /// <param name="dialogCardModel">Модель карточки диалога.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual async Task ShowDialogAsync(
            FilterViewDialogDescriptor descriptor,
            IViewParameters parameters,
            ICardModel dialogCardModel,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isApplied = false;
                var context = await this.advancedCardDialogManager.ShowCardAsync(
                    dialogCardModel,
                    prepareEditorActionAsync: async (editor, _) =>
                    {
                        editor.StatusBarIsVisible = false;

                        await this.FillFieldsAsync(
                            parameters,
                            editor.CardModel.Card,
                            descriptor.ParametersMapping,
                            CancellationToken.None);

                        editor.Toolbar.Actions.Clear();
                        editor.BottomToolbar.Actions.Clear();
                        editor.BottomDialogButtons.Clear();

                        editor.BottomDialogButtons.Add(
                            new UIButton(
                                "$UI_Common_OK",
                                async _ =>
                                {
                                    if (UIContext.Current.CardEditor is { OperationInProgress: false } e)
                                    {
                                        isApplied = true;
                                        await e.CloseAsync(cancellationToken: CancellationToken.None);
                                    }
                                },
                                isDefault: true));

                        editor.BottomDialogButtons.Add(
                            new UIButton(
                                "$UI_Common_Cancel",
                                async static _ =>
                                {
                                    if (UIContext.Current.CardEditor is { OperationInProgress: false } e)
                                    {
                                        await e.CloseAsync(cancellationToken: CancellationToken.None);
                                    }
                                },
                                isCancel: true));

                        return true;
                    },
                    options: new OpenCardOptions
                    {
                        DisplayValue = "$Views_FilterDialog_Caption",
                        WithDialogWallpaper = false,
                        DialogWindowModifierAction = static window =>
                        {
                            window.MaxWidth = 800;
                            window.SizeToContent = SizeToContent.Height;
                        }
                    },
                    cancellationToken: cancellationToken);

                if (isApplied)
                {
                    using (parameters.SuspendChangesNotification())
                    {
                        parameters.Clear();
                        CreateParameters(
                            parameters,
                            context.CardEditor.CardModel.Card,
                            descriptor.ParametersMapping);
                    }
                }
            }
            catch (NotSupportedException)
            {
                // Используется FakeUIHost, например, мы открыты в TessaAdmin, игнорируем ошибку.
            }
        }

        /// <summary>
        /// Заполняет поля карточки, данными параметров запроса к представлению.
        /// </summary>
        /// <param name="parameters">Список параметров представления.</param>
        /// <param name="card">Карточка, содержащая параметры.</param>
        /// <param name="parameterMappings">Коллекция, содержащая информацию о связи параметров представления и полей карточки.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async ValueTask FillFieldsAsync(
            IViewParameters parameters,
            Card card,
            IReadOnlyCollection<ParameterMapping> parameterMappings,
            CancellationToken cancellationToken = default)
        {
            var sections = card.Sections;

            foreach (var parameterMapping in parameterMappings)
            {
                await this.FillFieldAsync(parameters, sections, parameterMapping, cancellationToken);
            }
        }

        /// <summary>
        /// Заполняет поле <see cref="ParameterMapping.ValueFieldName"/> в секции <see cref="ParameterMapping.ValueSectionName"/>, содержащее параметр фильтрации представления <see cref="ParameterMapping.Alias"/>.
        /// </summary>
        /// <param name="parameters">Список параметров представления.</param>
        /// <param name="sections">Секции, содержащиеся в карточке диалога с параметрами представления.</param>
        /// <param name="parameterMapping"><inheritdoc cref="ParameterMapping" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async ValueTask FillFieldAsync(
            IViewParameters parameters,
            IReadOnlyDictionary<string, CardSection> sections,
            ParameterMapping parameterMapping,
            CancellationToken cancellationToken = default)
        {
            var parameter = parameters.FindByName(parameterMapping.Alias);
            if (parameter is null)
            {
                return;
            }

            var value = parameter.CriteriaValues.FirstOrDefault()?.Values.FirstOrDefault();
            if (value is null)
            {
                return;
            }

            if (!sections.TryGetValue(parameterMapping.ValueSectionName, out var valueSection))
            {
                return;
            }

            valueSection.Fields[parameterMapping.ValueFieldName] = value.Value;

            if (string.IsNullOrEmpty(parameterMapping.DisplayValueSectionName)
                || !sections.TryGetValue(parameterMapping.DisplayValueSectionName, out var displayValueSection))
            {
                return;
            }

            if (!string.IsNullOrEmpty(parameterMapping.DisplayValueFieldName)
                && parameters.Metadata.FindByName(parameter.Name) is { } parameterMetadata)
            {
                displayValueSection.Fields[parameterMapping.DisplayValueFieldName] = await this.parameterFormatter.FormatAsync(value, parameterMetadata, cancellationToken);
            }
        }

        /// <summary>
        /// Создаёт параметры запроса к представлению.
        /// </summary>
        /// <param name="parameters">Список параметров представления.</param>
        /// <param name="card">Карточка, содержащая параметры.</param>
        /// <param name="parameterMappings">Коллекция, содержащая информацию о связи параметров представления и полей карточки.</param>
        protected static void CreateParameters(
            IViewParameters parameters,
            Card card,
            IReadOnlyCollection<ParameterMapping> parameterMappings)
        {
            var sections = card.Sections;

            foreach (var parameterMapping in parameterMappings)
            {
                AddParameterIfValueNotEmpty(
                    parameters,
                    sections,
                    parameterMapping);
            }
        }

        /// <summary>
        /// Добавляет параметр в запрос к представлению, если поле <see cref="ParameterMapping.ValueFieldName"/> в секции <see cref="ParameterMapping.ValueSectionName"/> содержит данные.
        /// </summary>
        /// <param name="parameters">Список параметров представления.</param>
        /// <param name="sections">Секции, содержащиеся в карточке диалога с параметрами представления.</param>
        /// <param name="parameterMapping"><inheritdoc cref="ParameterMapping" path="/summary"/></param>
        protected static void AddParameterIfValueNotEmpty(
            IViewParameters parameters,
            IReadOnlyDictionary<string, CardSection> sections,
            ParameterMapping parameterMapping)
        {
            if (!sections.TryGetValue(parameterMapping.ValueSectionName, out var valueSection)
                || !valueSection.Fields.TryGetValue(parameterMapping.ValueFieldName, out var valueField)
                || valueField is null
                || parameters.Metadata.FindByName(parameterMapping.Alias) is not { } parameterMetadata)
            {
                return;
            }

            var displayValue = !string.IsNullOrEmpty(parameterMapping.DisplayValueSectionName)
                && !string.IsNullOrEmpty(parameterMapping.DisplayValueFieldName)
                && sections.TryGetValue(parameterMapping.DisplayValueSectionName, out var displayValueSection)
                && displayValueSection.RawFields.TryGetValue(parameterMapping.DisplayValueFieldName, out var displayValueObj)
                    ? displayValueObj?.ToString()
                    : null;

            parameters.Add(new RequestParameter(parameterMetadata.Alias)
                .Add(parameterMapping.CriteriaOperator ?? parameterMetadata.GetDefaultCriteria(), valueField, displayValue));
        }

        #endregion
    }
}
