#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Runtime;
using Tessa.UI;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Controls;
using Tessa.UI.Views;

namespace Tessa.Extensions.Default.Client.Cards
{
    /// <summary>
    /// Расширение, открывающее параметры этапа по двойному клику по строке представления <b>KrStageRows</b>.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="OpenStageSettingsOnDoubleClickExtensionConfigurator"/>
    /// </remarks>
    public sealed class OpenStageSettingsOnDoubleClickExtension :
        IWorkplaceViewComponentExtension
    {
        #region Fields

        private readonly ISession session;
        private readonly IUIHost uiHost;

        #endregion

        #region Constructors

        public OpenStageSettingsOnDoubleClickExtension(
            ISession session,
            IUIHost uiHost)
        {
            this.session = NotNullOrThrow(session);
            this.uiHost = NotNullOrThrow(uiHost);
        }

        #endregion

        #region IWorkplaceViewComponentExtension Members

        /// <inheritdoc/>
        public void Initialize(
            IWorkplaceViewComponent model)
        {
            if (this.session.Token?.ApplicationID == ApplicationIdentifiers.TessaAdmin
                || model.InSelectionMode())
            {
                return;
            }

            model.DoubleClickAction = new DoubleClickAction(this.uiHost);
        }

        /// <inheritdoc/>
        public void Initialized(
            IWorkplaceViewComponent model)
        {
        }

        /// <inheritdoc/>
        public void Clone(
            IWorkplaceViewComponent source,
            IWorkplaceViewComponent cloned,
            ICloneableContext context)
        {
        }

        #endregion

        #region DoubleClickAction Private Class

        private sealed class DoubleClickAction :
            OpenCardDoubleClickAction
        {
            #region Fields

            private readonly IUIHost uiHost;

            #endregion

            #region Constructors

            public DoubleClickAction(IUIHost uiHost) =>
                this.uiHost = uiHost;

            #endregion

            #region Base Overrides

            /// <inheritdoc/>
            protected override async Task OpenCardAsync(
                Guid cardID,
                string? displayValue,
                IUIContext context,
                ViewDoubleClickInfo info,
                Guid? cardTypeID = null,
                string? cardTypeName = null)
            {
                var selectedRow = context.ViewContext.SelectedRow;
                if (selectedRow is null
                    || !selectedRow.TryGetValue("StageRowID", out var stageRowIDObj)
                    || stageRowIDObj is not Guid stageRowID)
                {
                    return;
                }

                ICardUIContextObject? cardUIContextObject;
                using (var splash = TessaSplash.Create(TessaSplashMessage.OpeningCard))
                {
                    cardUIContextObject = await this.uiHost.OpenCardAsync(
                        cardID,
                        options: new OpenCardOptions
                        {
                            DisplayValue = displayValue,
                            UIContext = context,
                            Splash = splash,
                        });
                }

                if (cardUIContextObject is null)
                {
                    return;
                }

                var cardModel = cardUIContextObject.Context.CardEditor.CardModel;

                var approvalProcessTab = cardModel
                    .MainFormWithTabs
                    .Tabs
                    .FirstOrDefault(i => i.Name == KrConstants.Ui.KrApprovalProcessFormAlias);

                if (approvalProcessTab is null
                    || !cardModel.Controls.TryGet(KrConstants.Ui.KrApprovalStagesControlAlias, out var control)
                    || control is not GridViewModel grid
                    || grid.Rows.FirstOrDefault(i => i.Model.RowID == stageRowID) is not { } stageRowViewModel)
                {
                    return;
                }

                cardModel.MainFormWithTabs.SelectedTab = approvalProcessTab;
                grid.SelectedRow = stageRowViewModel;

                await grid.EditRowAsync(stageRowViewModel);
            }

            #endregion
        }

        #endregion
    }
}
