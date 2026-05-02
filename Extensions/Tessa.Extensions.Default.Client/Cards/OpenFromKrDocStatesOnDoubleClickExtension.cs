#nullable enable
using Tessa.Extensions.Platform.Client.Cards;
using Tessa.Platform.Runtime;
using Tessa.UI.Cards;
using Tessa.UI.Views;

namespace Tessa.Extensions.Default.Client.Cards
{
    /// <summary>
    /// Расширение, выполняющее открытие виртуальной карточки по строке в активных операциях.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="OpenFromKrDocStatesOnDoubleClickExtensionConfigurator"/>
    /// </remarks>
    public sealed class OpenFromKrDocStatesOnDoubleClickExtension :
        OpenInDialogOnDoubleClickExtensionBase
    {
        #region Constructors

        public OpenFromKrDocStatesOnDoubleClickExtension(
            ISession session,
            IAdvancedCardDialogManager advancedCardDialogManager)
            : base(session, advancedCardDialogManager)
        {
        }

        #endregion

        #region IWorkplaceViewComponentExtension Members

        public override void Initialize(IWorkplaceViewComponent model)
        {
            if (this.Session.Token?.ApplicationID == ApplicationIdentifiers.TessaAdmin)
            {
                // в TessaAdmin не будем ничего менять, т.к. там предпросмотр представлений
                return;
            }

            if (model.InSelectionMode())
            {
                // в режиме отбора не реагируем на двойной клик
                return;
            }

            model.DoubleClickAction = new OpenFromDocStatesDoubleClickAction(this.AdvancedCardDialogManager, this.Settings);
        }

        #endregion
    }
}
