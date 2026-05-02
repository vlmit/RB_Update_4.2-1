#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Controls;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.Views
{
    public sealed class CreateCardCopyExtensionViewModel :
        BaseContentItem
    {
        #region Nested Classes

        private record DocTypeInfo(
            ValidationResult Result,
            Guid? CardTypeID,
            Guid? DocTypeID,
            string? DocTypeTitle);

        #endregion

        #region Fields

        private readonly CreateCardCopyExtensionSettings settings;

        private readonly IUIHost uiHost;

        private readonly Func<ICardEditorModel> createEditorFunc;

        private readonly ICardRepository cardRepository;

        private readonly ICardTemplateManager templateManager;

        private readonly IWorkplaceViewComponent component;

        #endregion

        #region Constructors

        public CreateCardCopyExtensionViewModel(
            CreateCardCopyExtensionSettings settings,
            IUIHost uiHost,
            Func<ICardEditorModel> createEditorFunc,
            ICardRepository cardRepository,
            ICardTemplateManager templateManager,
            IIconContainer iconContainer,
            IWorkplaceViewComponent component,
            IEnumerable<IPlaceArea>? placeAreas,
            Func<IPlaceArea, DataTemplate>? dataTemplateFunc = null,
            int ordering = PlacementOrdering.Middle)
            : base(placeAreas, dataTemplateFunc, ordering)
        {
            this.settings = NotNullOrThrow(settings);
            this.uiHost = NotNullOrThrow(uiHost);
            this.createEditorFunc = NotNullOrThrow(createEditorFunc);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.templateManager = NotNullOrThrow(templateManager);

            this.component = NotNullOrThrow(component);
            this.component.Selection.SelectionChanged += this.ComponentSelectionChanged;
            this.component.PropertyChanged += this.ComponentPropertyChanged;

            this.Icon = new IconViewModel("Thin251", iconContainer);

            this.CreateCardCommand = new DelegateCommand(this.CreateCardActionAsync, p => this.canCreateCard);
        }

        #endregion

        #region Private Methods

        private void ComponentSelectionChanged(object? sender, SelectionStateEventArgs e)
        {
            this.Refresh();
        }

        private void ComponentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IWorkplaceViewComponent.Data) or nameof(IWorkplaceViewComponent.IsDataLoading))
            {
                this.Refresh();
            }
        }

        private void Refresh()
        {
            this.CanCreateCard = this.component.SelectedRow is not null
                && !this.component.IsDataLoading;
        }

        private void CreateCardActionAsync(object obj)
        {
            this.CreateCardActionBySelectedRowAsync(obj);
        }

        private async void CreateCardActionBySelectedRowAsync(object parameter)
        {
            if (!this.canCreateCard ||
                this.component is not { SelectedRow: { } row, IsDataLoading: false, View: { } view } ||
                (await view.GetMetadataAsync()).References.FirstOrDefault(x =>
                    x is { IsCard: true, OpenOnDoubleClick: true }) is not { } reference)
            {
                return;
            }

            UIContextExecutorAsync contextExecutorAsync = this.component.Workplace.UIContextExecutorAsync;

            Guid? cardID = row.GetCardID(reference);
            if (cardID.HasValue)
            {
                DocTypeInfo? typeInfo = null;
                await contextExecutorAsync(async (ctx, ct) =>
                {
                    (ValidationResult result, Guid? cardTypeID, Guid? docTypeID, string docTypeTitle) =
                        await DefaultExtensionHelper.GetDocTypeInfoAsync(this.cardRepository, cardID.Value, ct).ConfigureAwait(false);

                    typeInfo = new DocTypeInfo(result, cardTypeID, docTypeID, docTypeTitle);
                });

                if (typeInfo is not null)
                {
                    if (typeInfo.Result.IsSuccessful)
                    {
                        TessaDialog.ShowNotEmpty(typeInfo.Result);
                    }
                    else
                    {
                        TessaDialog.ShowNotEmpty(new ValidationResultBuilder()
                            .AddError(this, "$Views_CreateCardExtension_ErrorGettingType")
                            .Add(typeInfo.Result));
                        return;
                    }

                    if (typeInfo.CardTypeID.HasValue)
                    {
                        Dictionary<string, object?>? info = typeInfo.DocTypeID.HasValue
                            ? new Dictionary<string, object?>
                            {
                                { "docTypeID", typeInfo.DocTypeID.Value },
                                { "docTypeTitle", typeInfo.DocTypeTitle },
                            }
                            : null;

                        await this.CreateCardAsync(cardID.Value, cardTypeID: typeInfo.CardTypeID.Value, info: info);

                        return;
                    }
                }
            }

            TessaDialog.ShowError("$Views_CreateCardExtension_ErrorGettingType");
        }

        private async Task CreateCardAsync(
            Guid cardID,
            Guid? cardTypeID = null,
            string? cardTypeName = null,
            Dictionary<string, object?>? info = null)
        {
            IUIContext context = this.component.Workplace.Context;
            using ISplash splash = TessaSplash.Create(TessaSplashMessage.CreatingCard);
            await using (UIContext.Create(context))
            {
                var idParam = this.settings.IDParam;
                var inSelectionMode = this.component.InSelectionMode();

                var viewMetadata = await this.component.GetViewMetadataAsync(this.component);
                var idParamMeta = viewMetadata.Parameters.FindByName(idParam);
                var hasIDParam = idParamMeta is not null;

                // creating a copy of card an showing it in new tab
                var response = await this.templateManager.CopyAsync(new CardCopyRequest { SourceCardID = cardID }).ConfigureAwait(false);
                if (!response.ValidationResult.IsSuccessful())
                {
                    TessaDialog.ShowNotEmpty(response.ValidationResult);
                    return;
                }

                var editor = this.createEditorFunc();

                // response - ответ на запрос, полученный из ICardTemplateManager
                var model = await editor.CreateAndInitializeModelAsync(
                    response.Card,
                    response.SectionRows).ConfigureAwait(false);
                await editor.SetCardModelAsync(model).ConfigureAwait(false);

                var obj = await this.uiHost.ShowCardAsync(editor).ConfigureAwait(false);
                if (obj is not null)
                {
                    await DispatcherHelper.InvokeInUIAsync(() =>
                    {
                        editor.WorkspaceName = "$UI_Common_DefaultDigest_NewCard";
                        editor.OperationStatusText = CardUIManager.CardIsCopiedStatus;
                    }).Task.ConfigureAwait(false);
                }

                // в режиме открытия во вкладке представление автоматически обновляется за счёт свойства ICardEditorModel.IsUpdatedServer;
                // при открытии в диалоге приходится обновлять вручную, а если режим отбора - то ещё и выбирать значение после рефреша
                if (inSelectionMode)
                {
                    if (hasIDParam && this.component.View is { } view)
                    {
                        var request = new TessaViewRequest(viewMetadata.Alias)
                        {
                            new RequestParameter(idParam).Add(EqualsToCriteriaOperator.Instance, response.Card.ID)
                        };

                        var result = await view.GetDataAsync(request);
                        if (result.Rows.FirstOrDefault() is { } row)
                        {
                            this.component.Workplace.DoubleClickAction?.DoubleClick(
                                new ViewDoubleClickInfo
                                {
                                    View = viewMetadata,
                                    Context = context,
                                    Sender = this,
                                    SelectedObject = result.CreateRowStorage(row)
                                });

                            return;
                        }
                    }

                    await this.component.RefreshViewAsync();
                }
            }
        }

        #endregion

        #region Properties

        private bool canCreateCard;

        public bool CanCreateCard
        {
            get => this.canCreateCard;
            set
            {
                if (this.canCreateCard != value)
                {
                    this.canCreateCard = value;
                    this.OnPropertyChanged(nameof(this.CanCreateCard));
                }
            }
        }

        public string ToolTip =>
            LocalizeName("Views_CreateCardCopyExtension_Selection_ToolTip");

        public IconViewModel Icon { get; }

        public ICommand CreateCardCommand { get; }

        #endregion
    }
}
