using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Views;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
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
    public sealed class CreateCardExtensionViewModel :
        BaseContentItem
    {
        #region Constants

        public const string CreateAndSelectID = "CreateAndSelectID";

        #endregion

        #region Constructors

        public CreateCardExtensionViewModel(
            CreateCardExtensionSettings settings,
            IUIHost uiHost,
            IAdvancedCardDialogManager advancedCardDialogManager,
            ICardRepository cardRepository,
            IIconContainer iconContainer,
            IKrTypesCache krTypesCache,
            IWorkplaceViewComponent component,
            IEnumerable<IPlaceArea> placeAreas,
            Func<IPlaceArea, DataTemplate> dataTemplateFunc = null,
            int ordering = PlacementOrdering.Middle)
            : base(placeAreas, dataTemplateFunc, ordering)
        {
            this.settings = settings;

            this.uiHost = uiHost;
            this.advancedCardDialogManager = advancedCardDialogManager;
            this.cardRepository = cardRepository;
            this.krTypesCache = krTypesCache;

            this.component = component;
            this.component.Selection.SelectionChanged += this.ComponentSelectionChanged;
            this.component.PropertyChanged += this.ComponentPropertyChanged;

            string iconKey = settings.CardOpeningKind == CardOpeningKind.ModalDialog ? "Thin2" : "Thin1";
            this.Icon = new IconViewModel(iconKey, iconContainer);

            this.CreateCardCommand = new DelegateCommand(this.CreateCardActionAsync, p => this.canCreateCard);
        }

        #endregion

        #region DocTypeInfoResult Private Class

        private sealed class DocTypeInfo
        {
            #region Constructors

            public DocTypeInfo(
                ValidationResult result,
                Guid? cardTypeID,
                Guid? docTypeID,
                string docTypeTitle)
            {
                this.Result = result;
                this.CardTypeID = cardTypeID;
                this.DocTypeID = docTypeID;
                this.DocTypeTitle = docTypeTitle;
            }

            #endregion

            #region Properties

            public ValidationResult Result { get; }

            public Guid? CardTypeID { get; }

            public Guid? DocTypeID { get; }

            public string DocTypeTitle { get; }

            #endregion
        }

        #endregion

        #region Fields

        private readonly CreateCardExtensionSettings settings;

        private readonly IUIHost uiHost;

        private readonly IAdvancedCardDialogManager advancedCardDialogManager;

        private readonly ICardRepository cardRepository;

        private readonly IKrTypesCache krTypesCache;

        private readonly IWorkplaceViewComponent component;

        #endregion

        #region Private Methods

        private void Refresh()
        {
            switch (this.settings.CardCreationKind)
            {
                case CardCreationKind.ByTypeFromSelection:
                    this.CanCreateCard = this.component.SelectedRow != null
                        && !this.component.IsDataLoading;
                    break;
                case CardCreationKind.ByTypeAlias:
                    this.CanCreateCard = !string.IsNullOrWhiteSpace(this.settings.TypeAlias)
                        && !this.component.IsDataLoading;
                    break;
                case CardCreationKind.ByDocTypeIdentifier:
                    this.CanCreateCard = !string.IsNullOrWhiteSpace(this.settings.DocTypeIdentifier)
                        && !this.component.IsDataLoading;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void ComponentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IWorkplaceViewComponent.Data)
                || e.PropertyName == nameof(IWorkplaceViewComponent.IsDataLoading))
            {
                this.Refresh();
            }
        }


        private void ComponentSelectionChanged(object sender, SelectionStateEventArgs e)
        {
            this.Refresh();
        }


        private void CreateCardActionAsync(object obj)
        {
            switch (this.settings.CardCreationKind)
            {
                case CardCreationKind.ByTypeFromSelection:
                    this.CreateCardActionBySelectedRowAsync(obj);
                    break;

                case CardCreationKind.ByTypeAlias:
                    this.CreateCardActionByTypeAliasAsync(obj);
                    break;

                case CardCreationKind.ByDocTypeIdentifier:
                    this.CreateCardActionByDocTypeIdentifierAsync(obj);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(CreateCardExtensionSettings.CardCreationKind));
            }
        }


        private async void CreateCardActionBySelectedRowAsync(object parameter)
        {
            ITessaView view;
            IViewReferenceMetadata reference;
            IDictionary<string, object> row;
            if (!this.canCreateCard
                || (row = this.component.SelectedRow) == null
                || this.component.IsDataLoading
                || (view = this.component.View) == null
                || (reference = (await view.GetMetadataAsync()).References.FirstOrDefault(x => x.IsCard && x.OpenOnDoubleClick)) == null)
            {
                return;
            }

            UIContextExecutorAsync contextExecutorAsync = this.component.Workplace.UIContextExecutorAsync;

            Guid? cardID = row.GetCardID(reference);
            if (cardID.HasValue)
            {
                DocTypeInfo typeInfo = null;
                await contextExecutorAsync(async (ctx, ct) =>
                {
                    (ValidationResult result, Guid? cardTypeID, Guid? docTypeID, string docTypeTitle) =
                        await DefaultExtensionHelper.GetDocTypeInfoAsync(this.cardRepository, cardID.Value, ct).ConfigureAwait(false);

                    typeInfo = new DocTypeInfo(result, cardTypeID, docTypeID, docTypeTitle);
                });

                if (typeInfo != null)
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
                        Dictionary<string, object> info = typeInfo.DocTypeID.HasValue
                            ? new Dictionary<string, object>
                            {
                                { "docTypeID", typeInfo.DocTypeID.Value },
                                { "docTypeTitle", typeInfo.DocTypeTitle },
                            }
                            : null;

                        await this.CreateCardAsync(
                            cardTypeID: typeInfo.CardTypeID.Value,
                            displayValue: this.settings.DisplayValue,
                            info: info
                        );

                        return;
                    }
                }
            }

            TessaDialog.ShowError("$Views_CreateCardExtension_ErrorGettingType");
        }


        private async void CreateCardActionByDocTypeIdentifierAsync(object o)
        {
            if (!this.canCreateCard || this.component.IsDataLoading)
            {
                return;
            }

            KrDocType docType;
            if (!Guid.TryParse(this.settings.DocTypeIdentifier, out Guid docTypeID)
                || (docType = (await this.krTypesCache.GetDocTypesAsync()).FirstOrDefault(x => x.ID == docTypeID)) is null)
            {
                return;
            }

            var info = new Dictionary<string, object>
            {
                { "docTypeID", docTypeID },
                { "docTypeTitle", docType.Caption },
            };

            await this.CreateCardAsync(
                cardTypeID: docType.CardTypeID,
                displayValue: this.settings.DisplayValue,
                info: info
            );
        }


        private async void CreateCardActionByTypeAliasAsync(object o)
        {
            if (!this.canCreateCard || this.component.IsDataLoading)
            {
                return;
            }

            await this.CreateCardAsync(cardTypeName: this.settings.TypeAlias, displayValue: this.settings.DisplayValue);
        }

        private async Task CreateCardAsync(
            Guid? cardTypeID = null,
            string cardTypeName = null,
            string displayValue = null,
            Dictionary<string, object> info = null)
        {
            IUIContext context = this.component.Workplace.Context;
            using ISplash splash = TessaSplash.Create(TessaSplashMessage.CreatingCard);
            await using (UIContext.Create(context))
            {
                var idParam = this.settings.IDParam;
                var inSelectionMode = this.component.InSelectionMode();

                var viewMetadata = await this.component.GetViewMetadataAsync(this.component);
                var hasIDParam = viewMetadata.Parameters.IsDefinedByName(idParam);

                if (inSelectionMode && hasIDParam)
                {
                    context.Info[CreateAndSelectID] = null;
                }

                if (inSelectionMode || this.settings.CardOpeningKind == CardOpeningKind.ApplicationTab)
                {
                    await this.uiHost.CreateCardAsync(
                        cardTypeID: cardTypeID,
                        cardTypeName: cardTypeName,
                        options: new CreateCardOptions
                        {
                            UIContext = context,
                            Splash = splash,
                            Info = info,
                            OpenToTheRightOfSelectedTab = true,
                            DisplayValue = displayValue
                        });
                }
                else
                {
                    await this.advancedCardDialogManager.CreateCardAsync(
                        cardTypeID: cardTypeID,
                        cardTypeName: cardTypeName,
                        options: new CreateCardOptions
                        {
                            UIContext = context,
                            Splash = splash,
                            Info = info,
                            OpenToTheRightOfSelectedTab = true,
                            OpenInFullscreen = this.settings.OpenInFullscreen,
                            ShowOnlyFirstTab = this.settings.OpenOnlyFirstTab,
                            DisplayValue = displayValue
                        });
                }

                // в режиме открытия во вкладке представление автоматически обновляется за счёт свойства ICardEditorModel.IsUpdatedServer;
                // при открытии в диалоге приходится обновлять вручную, а если режим отбора - то ещё и выбирать значение после рефреша
                if (inSelectionMode || this.settings.CardOpeningKind == CardOpeningKind.ModalDialog)
                {
                    if (inSelectionMode
                        && hasIDParam
                        && context.Info.TryGetValue(CreateAndSelectID, out var idObj)
                        && idObj is Guid id
                        && this.component.View is { } view)
                    {
                        var result = await view.GetDataAsync(
                            new TessaViewRequest(viewMetadata.Alias) { new RequestParameter(idParam).Add(EqualsToCriteriaOperator.Instance, id) });

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

        private bool canCreateCard; // = false

        public bool CanCreateCard
        {
            get => this.canCreateCard;
            set
            {
                if (this.canCreateCard != value)
                {
                    this.canCreateCard = value;
                    this.OnPropertyChanged(nameof(CanCreateCard));
                }
            }
        }


        public string ToolTip =>
            LocalizeName(
                this.settings.CardCreationKind == CardCreationKind.ByTypeFromSelection
                    ? "Views_CreateCardExtension_Selection_ToolTip"
                    : "Views_CreateCardExtension_SpecifiedType_ToolTip");

        public IconViewModel Icon { get; }

        public ICommand CreateCardCommand { get; }

        #endregion
    }
}
