#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public sealed class PdfAnnotationsGetExtension : CardGetExtension
    {
        #region Fields

        private readonly IPdfAnnotationsStrategy pdfAnnotationsStrategy;

        private IList<PdfAnnotationsData>? pdfAnnsInfos;

        #endregion

        #region Constructors

        public PdfAnnotationsGetExtension(
            IPdfAnnotationsStrategy pdfAnnotationsStrategy)
            => this.pdfAnnotationsStrategy = NotNullOrThrow(pdfAnnotationsStrategy);

        #endregion

        #region Base Overrides

        public override async Task BeforeReleaseLock(ICardGetExtensionContext context)
        {
            if (context.Request.ServiceType == CardServiceType.Default
                || context.Request.RestrictionFlags.Has(CardGetRestrictionFlags.RestrictFiles)
                || (context.CardType?.Flags.HasNot(CardTypeFlags.AllowFiles) ?? true)
                || context.Response?.Card is not { } card)
            {
                return;
            }

            this.pdfAnnsInfos = await this.pdfAnnotationsStrategy.TryGetInfoAsync(card, context.CancellationToken);
        }

        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || context.Request.ServiceType == CardServiceType.Default
                || !context.ValidationResult.IsSuccessful()
                || this.pdfAnnsInfos is null
                || this.pdfAnnsInfos.Count == 0
                || context.Response?.TryGetCard() is not { } card
                || card.TryGetFiles()?.Where(x => x.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) is not { } files
                || !files.Any())
            {
                return;
            }

            foreach (var file in files)
            {
                var pdfAnnotationsID = this.pdfAnnsInfos
                    .FirstOrDefault(x => x.FileVersionRowID == file.VersionRowID)?.ID;
                if (pdfAnnotationsID is not null)
                {
                    file.Info.Add(PdfAnnotationsKeys.PdfAnnotationsKey, pdfAnnotationsID);
                }
            }
        }

        #endregion
    }
}
