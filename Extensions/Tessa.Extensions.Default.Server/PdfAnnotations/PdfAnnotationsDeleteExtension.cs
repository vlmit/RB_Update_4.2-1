#nullable enable

using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    /// <summary>
    /// Расширение, удаляющее записи об ознакомлении при удалении карточки.
    /// </summary>
    public sealed class PdfAnnotationsDeleteExtension : CardDeleteExtension
    {
        #region Fields

        private readonly IPdfAnnotationsStrategy pdfAnnotationsStrategy;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="PdfAnnotationsDeleteExtension"/>.
        /// </summary>
        /// <param name="pdfAnnotationsStrategy"><inheritdoc cref="IPdfAnnotationsStrategy" path="/summary"/></param>
        public PdfAnnotationsDeleteExtension(IPdfAnnotationsStrategy pdfAnnotationsStrategy) =>
            this.pdfAnnotationsStrategy = NotNullOrThrow(pdfAnnotationsStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Task BeforeCommitTransaction(ICardDeleteExtensionContext context)
        {
            // если карточка удаляется из корзины
            if (context.CardTypeIs(CardHelper.DeletedTypeID))
            {
                if (!context.Request.GetRestoreMode()
                    && context.TryGetCardToDelete() is { } card)
                {
                    return this.pdfAnnotationsStrategy.DeleteCardsFilesAnnotationsAsync(new[] { card.ID }, withBackup: false, context.CancellationToken);
                }
            }
            // если карточка удаляется сразу, минуя корзину
            else if (context.Request.DeletionMode == CardDeletionMode.WithoutBackup
                && context.Request.CardID is { } cardID)
            {
                return this.pdfAnnotationsStrategy.DeleteCardsFilesAnnotationsAsync(new[] { cardID }, withBackup: false, context.CancellationToken);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
