using System;
using System.IO;
using System.Threading.Tasks;
using BarcodeStandard;
using NLog;
using SkiaSharp;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Imaging.Cards;
using Tessa.Imaging.DocLoad;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Licensing;
using Tessa.Platform.Storage;
using Type = BarcodeStandard.Type;

namespace Tessa.Extensions.Default.Imaging.Files
{
    /// <summary>
    /// Расширение получения.
    /// </summary>
    public sealed class DocLoadBarcodeGetFileContentExtension :
        CardGetFileContentExtension
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IDbScope dbScope;

        private readonly ICardCache cardCache;

        private readonly ICardRepository cardRepository;

        private readonly ILicenseManager licenseManager;

        private readonly IBarcodeConverter barcodeConverter;

        #endregion

        #region Constructors

        public DocLoadBarcodeGetFileContentExtension(
            IDbScope dbScope,
            ICardCache cardCache,
            ICardRepository cardRepository,
            ILicenseManager licenseManager,
            IBarcodeConverter barcodeConverter)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.cardCache = NotNullOrThrow(cardCache);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.licenseManager = NotNullOrThrow(licenseManager);
            this.barcodeConverter = NotNullOrThrow(barcodeConverter);
        }

        #endregion

        #region Base Overrides

        public override async Task BeforeRequest(ICardGetFileContentExtensionContext context)
        {
            var docLoad = await this.cardCache.Cards.GetAsync(CardHelper.DocLoadTypeName, context.CancellationToken);
            if (!docLoad.IsSuccess)
            {
                return;
            }

            var fields = docLoad.GetValue().Sections[DocLoadStrings.DocLoadSettingsSectionName].Fields;
            var isEnabled = fields.TryGet<bool>(DocLoadStrings.IsEnabledFieldName);
            if (!isEnabled || !(await this.licenseManager.GetLicenseAsync(context.CancellationToken)).Modules.Contains(LicenseModules.DocLoadID))
            {
                return;
            }

            context.Response = new CardGetFileContentResponse();

            var tableName = fields.TryGet<string>(DocLoadStrings.DefaultBarcodeTableNameFieldName)!;
            var fieldName = fields.TryGet<string>(DocLoadStrings.DefaultBarcodeFieldNameFieldName)!;
            var barcodeType = fields.TryGet<string>(DocLoadStrings.BarcodeWriteNameFieldName)!;
            var barcodeLabel = fields.TryGet<bool>(DocLoadStrings.BarcodeLabelFieldName);
            var barcodeWidth = fields.TryGet<double>(DocLoadStrings.BarcodeWidthFieldName);
            var barcodeHeight = fields.TryGet<double>(DocLoadStrings.BarcodeHeightFieldName);

            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;

            var barcode = await db
                .SetCommand(this.dbScope.BuilderFactory
                        .Select()
                        .C(fieldName)
                        .From(tableName).NoLock()
                        .Where()
                        .C("ID").Equals().P("ID")
                        .Build(),
                    db.Parameter("ID", context.Request.CardID))
                .LogCommand()
                .ExecuteAsync<string>(context.CancellationToken);

            if (string.IsNullOrWhiteSpace(barcode))
            {
                var getRequest = new CardGetRequest { CardID = context.Request.CardID };
                var getResponse = await this.cardRepository.GetAsync(getRequest, context.CancellationToken);
                if (!getResponse.ValidationResult.IsSuccessful())
                {
                    context.ValidationResult.Add(getResponse.ValidationResult.Build());
                    return;
                }

                var card = getResponse.Card;
                var cardDigest = context.Request.TryGetDigest();
                if (string.IsNullOrEmpty(cardDigest))
                {
                    cardDigest = await this.cardRepository.GetDigestAsync(card, CardDigestEventNames.DocLoadStore, context.CancellationToken);
                }

                card.RemoveAllButChanged();

                var storeRequest = new CardStoreRequest
                {
                    Card = card,
                    Info =
                    {
                        [DocLoadBarcodeStoreExtension.CreateBarcodeKey] = BooleanBoxes.True
                    }
                };
                storeRequest.SetDigest(cardDigest);

                var storeResponse = await this.cardRepository.StoreAsync(storeRequest, context.CancellationToken);
                if (!storeResponse.ValidationResult.IsSuccessful())
                {
                    context.ValidationResult.Add(storeResponse.ValidationResult.Build());
                    return;
                }

                barcode = storeResponse.Info.Get<string>(DocLoadBarcodeStoreExtension.CreateBarcodeKey);
                context.Response.Info["RefreshCard"] = BooleanBoxes.True;
            }

            barcode = TransformBarcode(barcodeType, barcode);

            try
            {
                using var bc = new Barcode { IncludeLabel = barcodeLabel };
                var type = this.barcodeConverter.GetBarcodeForWrite(barcodeType);
                if (type == Type.Unspecified)
                {
                    throw new InvalidOperationException($"Can't parse barcode type: {barcodeType}");
                }

                byte[] data;

                using (var img = bc.Encode(this.barcodeConverter.GetBarcodeForWrite(barcodeType), barcode, (int) barcodeWidth, (int) barcodeHeight))
                {
                    await using var stream = new MemoryStream();
                    using var encodedData = img.Encode(SKEncodedImageFormat.Png, 100);
                    encodedData.SaveTo(stream);
                    data = stream.ToArray();
                }

                context.ContentFuncAsync = ct => new ValueTask<Stream?>(new MemoryStream(data));
                context.Response.Size = data.LongLength;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogException(e, LogLevel.Error);
                throw new InvalidOperationException("Invalid input barcode. Please, check barcode format.", e);
            }
        }

        #endregion

        #region Private Methods

        private static string? TransformBarcode(string barcodeType, string? barcode) =>
            barcodeType switch
            {
                "CODABAR" => $"A{barcode}B",
                _ => barcode
            };

        #endregion
    }
}
