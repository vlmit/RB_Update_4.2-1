#nullable enable

using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using Tessa.Cards.Extensions;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.FileConverters.Workers;
using Tessa.Extensions.Default.Server.PdfAnnotations.Types;
using Tessa.FileConverters;
using Tessa.Files;
using Tessa.Platform.IO;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Platform;
using Tessa.Stamping;
using Unity;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public sealed class PdfAnnotationsRequestExtension(
        IPdfAnnotationsStrategy pdfAnnotationsStrategy,
        ICardRepository cardRepository,
        ICardFileManager fileManager,
        ICardStreamServerRepository cardStreamRepository,
        IPlaceholderManager placeholderManager,
        IUnityContainer container,
        [Dependency(FileConverterWorkerNames.SvgToPng)]
        IFileConverterWorker svgToPngWorker,
        IStampingProcessor stampingProcessor) : CardRequestExtension
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IPdfAnnotationsStrategy pdfAnnotationsStrategy = NotNullOrThrow(pdfAnnotationsStrategy);
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly ICardFileManager fileManager = NotNullOrThrow(fileManager);
        private readonly ICardStreamServerRepository cardStreamRepository = NotNullOrThrow(cardStreamRepository);
        private readonly IPlaceholderManager placeholderManager = NotNullOrThrow(placeholderManager);
        private readonly IUnityContainer container = NotNullOrThrow(container);
        private readonly IFileConverterWorker svgToPngWorker = NotNullOrThrow(svgToPngWorker);
        private readonly IStampingProcessor stampingProcessor = NotNullOrThrow(stampingProcessor);

        #endregion

        #region Base Overrides

        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful || context.Response is null)
            {
                return;
            }

            var pdfAnnotationsInfo = PdfAnnotationsHelper.GetPdfAnnotationsInfo(context.Request);
            if (pdfAnnotationsInfo is null || pdfAnnotationsInfo.Data is null)
            {
                return;
            }

            switch (pdfAnnotationsInfo.Action)
            {
                case PdfAnnotationsAction.Get:
                    {
                        context.Response.Info[PdfAnnotationsKeys.PdfAnnotationsKey] = await this.pdfAnnotationsStrategy
                            .RequestMergedAnnsBeforeStoreAsync(
                                    pdfAnnotationsInfo.Data,
                                    context.Session.User.ID,
                                    context.CancellationToken);
                        break;
                    }
                case PdfAnnotationsAction.AddStamp:
                    {
                        if (PdfAnnotationsHelper.GetAddStampInfo(context.Request) is not { Data: { } } addStampInfo)
                        {
                            return;
                        }

                        if (await HandleAddStampAsync(addStampInfo, context) is { } bytes)
                        {
                            context.Response.Info[PdfAnnotationsKeys.PdfAnnotationsKey] = Convert.ToBase64String(bytes);
                        }

                        break;
                    }
                case PdfAnnotationsAction.Export:
                    {
                        if (PdfAnnotationsHelper.GetExportInfo(context.Request) is not { Data: { } } exportInfo)
                        {
                            return;
                        }

                        if (await this.ExportStampAsync(exportInfo, context) is { } bytes)
                        {
                            context.Response.Info[PdfAnnotationsKeys.PdfAnnotationsKey] = Convert.ToBase64String(bytes);
                        }
                        break;
                    }
                case PdfAnnotationsAction.GetBar:
                    {
                        if (PdfAnnotationsHelper.GetStampsBarInfo(context.Request) is not { Data: { } } barInfo)
                        {
                            return;
                        }

                        if (await this.ExportBarAsync(barInfo, context) is { } bytes)
                        {
                            context.Response.Info[PdfAnnotationsKeys.PdfAnnotationsKey] = bytes;
                        }
                        break;
                    }
                case PdfAnnotationsAction.GetQR:
                    {
                        if (PdfAnnotationsHelper.GetStampsQRInfo(context.Request) is not { Data: { } } qrInfo)
                        {
                            return;
                        }

                        if (await this.ExportQRAsync(qrInfo, context) is { } bytes)
                        {
                            context.Response.Info[PdfAnnotationsKeys.PdfAnnotationsKey] = bytes;
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Private Methods

        private async Task<string?> ExportQRAsync(StampsQRInfo qrInfo, ICardRequestExtensionContext context)
        {
            var data = NotNullOrThrow(qrInfo.Data);
            var textBuilder = new StringBuilder($"{{{data.PlaceholderContent?.Trim(' ', '{', '}')}:#qrcode(w={data.Width};h={data.Height};");
            if (data.QRCodeType is { } qrCodeType)
            {
                textBuilder.Append($"t={qrCodeType};");
            }
            if (data.ECC is { } ecc)
            {
                textBuilder.Append($"ecc={ecc};");
            }
            if (data.UTF8 ?? false)
            {
                textBuilder.Append("utf8;");
            }
            if (data.BOM ?? false)
            {
                textBuilder.Append("bom;");
            }
            if (data.PX is { } px)
            {
                textBuilder.Append($"px={px};");
            }
            textBuilder.Append(")}");
            var text = textBuilder.ToString();
            logger.Trace($"ExportQR, placeholder text = {text}.");

            var (result, validationResult) = await this.placeholderManager.ReplaceTextAsync(
                text,
                null,
                context.Session,
                this.container,
                context.DbScope,
                cardID: qrInfo.Data!.CardID,
                cancellationToken: context.CancellationToken);
            context.Response!.ValidationResult.Add(validationResult);
            return validationResult?.IsSuccessful == true ? result : string.Empty;
        }

        private async Task<string?> ExportBarAsync(StampsBarInfo barInfo, ICardRequestExtensionContext context)
        {
            var data = NotNullOrThrow(barInfo.Data);
            var labelValue = data.ShowLabel ? "BottomCenter" : "None";
            var text = $"{{{data.PlaceholderContent?.Trim(' ', '{', '}')}:#barcode(t={data.BarcodeType};w={data.Width};h={data.Height};l={labelValue})}}";
            logger.Trace($"ExportBar, placeholder text = {text}.");

            var (result, validationResult) = await this.placeholderManager.ReplaceTextAsync(
                text,
                null,
                context.Session,
                this.container,
                context.DbScope,
                cardID: barInfo.Data!.CardID,
                cancellationToken: context.CancellationToken);
            context.Response!.ValidationResult.Add(validationResult);
            if (!validationResult?.IsSuccessful ?? false)
            {
                return string.Empty;
            }
            return result;
        }

        private async Task<byte[]?> ExportStampAsync(ExportInfo exportInfo, ICardRequestExtensionContext context)
        {
            var response = await this.cardRepository.GetAsync(new CardGetRequest { CardID = exportInfo.Data!.StampsConstructorCardID }, context.CancellationToken);
            if (!response.ValidationResult.IsSuccessful())
            {
                context.Response!.ValidationResult.Add(response.ValidationResult);
                return null;
            }
            var stampCard = response.Card;

            var exportData = exportInfo.Data!;

            var svgBytes = await this.ResolveBarAndQRPlaceholderForStampAsync(stampCard, exportData.StampsRowID, exportData.CardID, exportData.Preview, context);
            if (svgBytes is null || exportInfo.Data.ExportType == ExportType.Svg)
            {
                return svgBytes;
            }

            var stampStream = await this.GetPngStreamFromSvgAsync(svgBytes, exportData.CardID, opacity: 1, context);

            return stampStream?.ReadAllBytes();
        }

        private async Task<byte[]?> HandleAddStampAsync(AddStampInfo stampInfo, ICardRequestExtensionContext context)
        {
            logger.Trace("start stamping.");
            // load card, get by id and fileVersionRowId from StampPlaces
            var response = await this.cardRepository.GetAsync(new CardGetRequest { CardID = stampInfo.Data!.StampsConstructorCardID }, context.CancellationToken);
            if (!response.ValidationResult.IsValid())
            {
                context.Response!.ValidationResult.Add(response.ValidationResult);
                return null;
            }
            var cardID = stampInfo.Data.CardID;
            var stampCard = response.Card;

            // get anns from Place
            if (stampCard.TryGetSections()?
                .TryGet("StampPlaces")?
                .TryGetRows()?
                .FirstOrDefault(x => x.RowID == stampInfo.Data!.StampsRowID)?
                .TryGet<string>("Place") is not { } placeAnns)
            {
                return null;
            }

            var obj = StorageHelper.DeserializeListFromTypedJson(placeAnns);

            if (PdfAnnotationsData.GetAnnotations(obj) is not { } anns)
            {
                return null;
            }

            // get file content
            var (versionRowID, fileContent) = await this.GetFileContentAsync(stampInfo, context);
            if (fileContent is null)
            {
                return null;
            }
            var fileContentLength = fileContent.Length;

            var stampPlacement = (anns.FirstOrDefault(x => x.AnnType == AnnotationType.Canvas) as CanvasAnnotation)?.StampPlacement;

            bool needToSave = false;
            var annsAndStreams = new List<(ImageKindAnnotation ann, Stream stream)>();
            foreach (var ann in anns)
            {
                logger.Trace($"process ann: {ann.ID}.");
                byte[]? svgBytes = null;
                // get stamp
                if (ann is not ImageKindAnnotation imageKindAnn)
                {
                    logger.Trace($"end process of ann {ann.ID}.");
                    continue;
                }

                switch (ann.AnnType)
                {
                    case AnnotationType.Image:
                        {
                            logger.Trace("ann is an image.");

                            var imageAnn = (ImageAnnotation)ann;
                            if (imageAnn.ReferenceID is { } referenceID)
                            {
                                logger.Trace("have referenceId.");
                                svgBytes = await this.ResolveBarAndQRPlaceholderForStampAsync(stampCard, referenceID, cardID, preview: false, context);
                            }
                            else if (imageAnn.FileName.ToLower().EndsWith(".svg", StringComparison.OrdinalIgnoreCase) && imageAnn.DataUrl is not null)
                            {
                                logger.Trace("ann image does not have referenceId and it is a svg file.");
                                svgBytes = Convert.FromBase64String(DeleteBase64Preambule(imageAnn.DataUrl)!);
                            }

                            if (svgBytes is null && imageAnn.DataUrl is not null)
                            {
                                logger.Trace("ann is an image and it does not have reference to stamp.");
                                var svgText = GetSvgFromImage(imageAnn);
                                svgBytes = Encoding.UTF8.GetBytes(svgText);
                            }
                            break;
                        }
                    case AnnotationType.Bar:
                        {
                            logger.Trace("ann is a bar.");
                            var barAnn = (BarAnnotation)ann;
                            var imageStr = await this.ExportBarAsync(
                                    new StampsBarInfo
                                    {
                                        Data = new StampsBarData
                                        {
                                            CardID = cardID,
                                            Width = barAnn.Width,
                                            Height = barAnn.Height,
                                            BarcodeType = barAnn.BarcodeType,
                                            PlaceholderContent = barAnn.PlaceholderContent,
                                            ShowLabel = barAnn.ShowLabel
                                        }
                                    }, context);
                            if (imageStr is { })
                            {
                                barAnn.DataUrl = "data:image/png;base64," + imageStr;
                                var svgText = GetSvgFromImage(barAnn);
                                svgBytes = Encoding.UTF8.GetBytes(svgText);
                            }
                            break;
                        }
                    case AnnotationType.QR:
                        {
                            logger.Trace("ann is a qr.");
                            var qrAnn = (ann as QRAnnotation)!;
                            var imageStr = await this.ExportQRAsync(
                                    new StampsQRInfo
                                    {
                                        Data = new StampsQRData
                                        {
                                            CardID = cardID,
                                            Width = qrAnn.Width,
                                            Height = qrAnn.Height,
                                            QRCodeType = qrAnn.QRCodeType,
                                            PlaceholderContent = qrAnn.PlaceholderContent,
                                            ECC = qrAnn.ECC,
                                            UTF8 = qrAnn.UTF8,
                                            BOM = qrAnn.BOM,
                                            PX = qrAnn.PX,
                                        }
                                    }, context);
                            if (imageStr is { })
                            {
                                qrAnn.DataUrl = "data:image/png;base64," + imageStr;
                                var svgText = GetSvgFromImage(qrAnn);
                                svgBytes = Encoding.UTF8.GetBytes(svgText);
                            }
                            break;
                        }
                    default:
                        {
                            logger.Trace($"ann type is {ann.AnnType}.");
                            break;
                        }
                }

                if (svgBytes is not null && await this.GetPngStreamFromSvgAsync(svgBytes, cardID, imageKindAnn.Opacity, context) is { } stampStream)
                {
                    logger.Trace("process svgBytes.");
                    annsAndStreams.Add((imageKindAnn, stampStream));
                }

                logger.Trace($"end process of ann {ann.ID}.");
            }

            var addedImageAnnotations = new List<ImageAnnotation>();
            if (annsAndStreams.Count > 0)
            {
                logger.Trace("add stamp.");
                // insert stamp to every page of fileId of cardId
                var (pageAndNames, (fileStream, fileLength)) = await this.InsertStampAsync(
                    annsAndStreams,
                    fileContent,
                    fileContentLength,
                    stampPlacement,
                    context);

                if (fileStream is not null && pageAndNames is not null)
                {
                    fileContent = fileStream;
                    fileContentLength = fileLength;
                    needToSave = true;

                    for (var j = 0; j < pageAndNames.Keys.Count; j++)
                    {
                        var pageNumber = pageAndNames.Keys.ElementAt(j);
                        var pageAndName = pageAndNames[pageNumber];

                        for (var k = 0; k < pageAndName.Count; k++)
                        {
                            var imageKindAnn = annsAndStreams[k].ann;
                            var rand = new Random();
                            addedImageAnnotations.Add(
                                    new ImageAnnotation
                                    {
                                        ID = new PDFRef
                                        {
                                            ObjectNumber = rand.NextDouble(),
                                            GenerationNumber = rand.NextDouble()
                                        },
                                        FileName = Guid.NewGuid() + ".png",
                                        State = AnnotationState.Inserted,
                                        Date = DateTime.UtcNow,
                                        Page = pageNumber,
                                        UserID = context.Session.User.ID,
                                        UserName = context.Session.User.Name,
                                        X = imageKindAnn.X,
                                        Y = imageKindAnn.Y,
                                        Width = imageKindAnn.Width,
                                        Height = imageKindAnn.Height,
                                        SelfRotationAngle = imageKindAnn.SelfRotationAngle,
                                        Opacity = imageKindAnn.Opacity,
                                        XObjectKey = pageAndName[k]
                                    });
                        }
                    }
                }
            }

            if (needToSave)
            {
                logger.Trace("need to save.");
                if (stampInfo.Data.StoreToCard)
                {
                    // saveFile
                    if (await SaveFileAsync(cardID, stampInfo.Data.FileID, fileContent, context) is { } newFileVersionRowID)
                    {
                        // TODO save anns to the anns of a stamped file with refs to images back to the base
                        var pdfAnnotations = await this.pdfAnnotationsStrategy.RequestMergedAnnsBeforeStoreAsync(
                                new PdfAnnotationsData
                                {
                                    CardID = cardID,
                                    FileID = stampInfo.Data.FileID,
                                    FileVersionRowID = versionRowID!.Value,
                                    Modified = DateTime.UtcNow,
                                    ModifiedByID = context.Session.User.ID,
                                    Annotations = [.. addedImageAnnotations.Cast<Annotation>()]
                                }, context.Session.User.ID, context.CancellationToken);
                        pdfAnnotations.FileVersionRowID = newFileVersionRowID;
                        await this.pdfAnnotationsStrategy.StoreAsync(pdfAnnotations, context.Session.User.ID, context.CancellationToken);
                    }
                }
                else
                {
                    logger.Trace("return content.");
                    return fileContent?.ReadAllBytes();
                }
            }

            logger.Trace("end of stamping.");
            return null;
        }

        private async Task<Stream?> GetPngStreamFromSvgAsync(byte[] svgBytes, Guid cardID, double opacity, ICardRequestExtensionContext context)
        {
            logger.Trace("process svgBytes.");
            var svgText = Encoding.UTF8.GetString(svgBytes);
            svgText = InsertOpacity(svgText, opacity);

            var (resultSvg, validationResult) = await this.placeholderManager.ReplaceTextAsync(
                svgText,
                null,
                context.Session,
                this.container,
                context.DbScope,
                cardID: cardID,
                cancellationToken: context.CancellationToken);
            context.Response!.ValidationResult.Add(validationResult);
            if (!validationResult?.IsSuccessful ?? false)
            {
                logger.Trace("not successful.");
                return null;
            }

            svgBytes = Encoding.UTF8.GetBytes(resultSvg!);

            var guidName = Guid.NewGuid().ToString("N");
            var svgFileName = guidName + ".svg";
            var pngFileName = guidName + ".png";
            using var svgTempFile = TempFile.Acquire(svgFileName);
            using var pngTempFile = TempFile.Acquire(pngFileName);

            await using (var svgMemoryStream = new MemoryStream(svgBytes, false))
            await using (var svgStream = FileHelper.Create(svgTempFile.Path))
            {
                await svgMemoryStream.CopyToAsync(svgStream, context.CancellationToken);
                await svgStream.FlushAsync();
            }

            // convert stamp to width and height
            return await this.ConvertStampToPngWithWidthHeightAsync(svgTempFile, pngTempFile, context.CancellationToken);
        }

        private async Task<byte[]?> ResolveBarAndQRPlaceholderForStampAsync(Card stampConstructorCard, Guid stampID, Guid cardID, bool preview, ICardRequestExtensionContext context)
        {
            var stampItem = stampConstructorCard.TryGetSections()?
                .TryGet("Stamps")?
                .TryGetRows()?
                .FirstOrDefault(x => x.RowID == stampID);
            if (stampItem?.TryGet(preview ? "PreviewFile" : "File") is not byte[] svgBytes)
            {
                logger.Trace("does not have svgBytes.");
                return null;
            }

            if (!preview && stampItem?.TryGet<string>("Annotations") is { } stampAnnsStr &&
                    StorageHelper.DeserializeListFromTypedJson(stampAnnsStr) is { } obj &&
                    PdfAnnotationsData.GetAnnotations(obj) is { } stampAnns &&
                    stampAnns.Where(x => x.AnnType == AnnotationType.Bar || x.AnnType == AnnotationType.QR).ToList() is { Count: > 0 } barQrs)
            {
                var svgText = Encoding.UTF8.GetString(svgBytes);
                foreach (var stampAnn in barQrs)
                {
                    logger.Trace($"Replace bar/qr for id {stampAnn.ID}.");
                    string? imageContent = null;
                    if (stampAnn.AnnType == AnnotationType.Bar)
                    {
                        var barAnn = (BarAnnotation)stampAnn;
                        imageContent = await this.ExportBarAsync(
                            new StampsBarInfo
                            {
                                Data = new StampsBarData
                                {
                                    CardID = cardID,
                                    Width = barAnn.Width,
                                    Height = barAnn.Height,
                                    BarcodeType = barAnn.BarcodeType,
                                    PlaceholderContent = barAnn.PlaceholderContent,
                                    ShowLabel = barAnn.ShowLabel
                                }
                            }, context);
                    }
                    else if (stampAnn.AnnType == AnnotationType.QR)
                    {
                        var qrAnn = (QRAnnotation)stampAnn;
                        imageContent = await this.ExportQRAsync(
                                new StampsQRInfo
                                {
                                    Data = new StampsQRData
                                    {
                                        CardID = cardID,
                                        Width = qrAnn.Width,
                                        Height = qrAnn.Height,
                                        QRCodeType = qrAnn.QRCodeType,
                                        PlaceholderContent = qrAnn.PlaceholderContent,
                                        ECC = qrAnn.ECC,
                                        UTF8 = qrAnn.UTF8,
                                        BOM = qrAnn.BOM,
                                        PX = qrAnn.PX,
                                    }
                                }, context);
                    }

                    if (imageContent is null)
                    {
                        logger.Trace("Cannot get new image for an ann.");
                        continue;
                    }
                    var imageStr = "data:image/png;base64," + imageContent;
                    var refID = stampAnn.ID.ToString();
                    logger.Trace($"refID to replace {refID}.");

                    var refIndex = svgText.IndexOf(refID, StringComparison.OrdinalIgnoreCase);
                    if (refIndex == -1)
                    {
                        logger.Trace("id not found.");
                        continue;
                    }

                    var firstPart = svgText[refIndex..];
                    var hrefIndex = firstPart.IndexOf("href=", StringComparison.OrdinalIgnoreCase);
                    if (hrefIndex == -1)
                    {
                        logger.Trace("href not found.");
                        continue;
                    }

                    var quoteIndex = firstPart[(hrefIndex + 6)..].IndexOf('"', StringComparison.OrdinalIgnoreCase);
                    if (quoteIndex == -1)
                    {
                        logger.Trace("closing quote not found.");
                        continue;
                    }

                    var lastPart = firstPart[quoteIndex..];

                    svgText = svgText[..(refIndex + hrefIndex)] + "href=\"" + imageStr + lastPart;
                }
                svgBytes = Encoding.UTF8.GetBytes(svgText);
            }

            return svgBytes;
        }

        private static string GetSvgFromImage(ImageKindAnnotation imageAnn) =>
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 {imageAnn.Width} {imageAnn.Height}\" width=\"{imageAnn.Width}\" height=\"{imageAnn.Height}\"><image  width=\"{imageAnn.Width}\" height=\"{imageAnn.Height}\" preserveAspectRatio=\"none\" style=\"transform-box: fill-box;transform-origin: center center;\" href=\"{imageAnn.DataUrl}\"></image></svg>";

        private static string InsertOpacity(string svgText, double opacity)
        {
            const string elementStr = "<svg";
            if (!svgText.StartsWith(elementStr, StringComparison.OrdinalIgnoreCase))
            {
                return svgText;
            }

            return $"{elementStr} opacity=\"{opacity}\" {svgText.AsSpan(elementStr.Length)}";
        }

        private async Task<(Guid? versionRowID, Stream?)> GetFileContentAsync(AddStampInfo stampInfo, ICardRequestExtensionContext context)
        {
            var response = await this.cardRepository.GetAsync(new CardGetRequest { CardID = stampInfo.Data!.CardID }, context.CancellationToken);
            if (!response.ValidationResult.IsSuccessful())
            {
                context.Response!.ValidationResult.Add(response.ValidationResult);
                return (null, null);
            }

            if (response.Card.TryGetFiles()?.FirstOrDefault(x => x.RowID == stampInfo.Data.FileID) is not { } file)
            {
                context.Response!.ValidationResult.AddError("File not found");
                return (null, null);
            }

            var contentRequest = new CardGetFileContentRequest
            {
                ServiceType = CardServiceType.Default,
                CardID = stampInfo.Data!.CardID,
                FileID = file.RowID,
                FileName = file.Name,
                VersionRowID = file.LastVersion!.RowID
            };

            var contentResult = await this.cardStreamRepository.GetFileContentAsync(
                contentRequest, context.CancellationToken);

            var contentResponse = contentResult.Response;

            context.ValidationResult.Add(contentResponse.ValidationResult);

            if (!contentResponse.ValidationResult.IsSuccessful() || !contentResult.HasContent)
            {
                return (null, null);
            }
            var fileContent = await contentResult.GetContentOrThrowAsync(cancellationToken: context.CancellationToken);
            return (file.LastVersion!.RowID, fileContent);
        }

        /// <summary>
        /// Just converting svg to png without width and height change! Scaling part is taking part in pdf itextshart scaling
        /// </summary>
        /// <param name="svgTempFile">A stamp file path</param>
        /// <param name="pngTempFile">A stamp file path</param>
        /// <param name="cancellationToken">A cancellationToken</param>
        /// <returns>A png stream</returns>
        private async Task<Stream> ConvertStampToPngWithWidthHeightAsync(ITempFile svgTempFile, ITempFile pngTempFile, CancellationToken cancellationToken)
        {
            var context = new FileConverterContext(
                    (_) => ValueTask.FromResult(System.IO.File.OpenRead(svgTempFile.Path) as Stream),
                    (_) =>
                    {
                        var s = System.IO.File.OpenRead(pngTempFile.Path) as Stream;
                        return ValueTask.FromResult((s, s.Length));
                    },
                    inputExtension: "svg",
                    outputExtension: "png",
                    suggestedName: pngTempFile.Name,
                    cancellationToken: cancellationToken
                    );
            await this.svgToPngWorker.ConvertFileAsync(context, cancellationToken);
            var validationResult = context.ValidationResult.Build();
            logger.LogResult(validationResult);
            if (!validationResult.IsSuccessful)
            {
                throw new ValidationException(validationResult);
            }

            return (await context.GetOutputContentAsync(cancellationToken)).Stream;
        }

        async Task<(IDictionary<int, IList<string>>?, (Stream? stream, long length))> InsertStampAsync(
                List<(ImageKindAnnotation, Stream)> annsAndStreams,
                Stream pdfFileStream,
                long pdfFileStreamLength,
                StampPlacement? stampPlacement,
                ICardRequestExtensionContext context)
        {

            const string pdfFileName = "name.pdf";
            const string stampName = "stamp.png";

            var result = new List<(int, string)>();

            var parameters = annsAndStreams.Select(((ImageKindAnnotation ann, Stream stream) annStrm) => new StampParametersServer
            {
                UseUpperRightCorner = true,
                Left = (float)annStrm.ann.X,
                Top = (float)annStrm.ann.Y,
                Width = (float)annStrm.ann.Width,
                Height = (float)annStrm.ann.Height,
                Angle = (float)(annStrm.ann.SelfRotationAngle ?? 0),
                StampName = stampName,
                GetStampContentAsync = (_) => new(annStrm.stream),
            });

            IEnumerable<int> pages = stampPlacement switch
            {
                StampPlacement.First => [1],
                StampPlacement.Last => [-1],
                _ => [],
            };

            var stampingContext = new StampingContext(
                    [.. parameters],
                    [.. pages],
                    pdfFileName,
                    (_) => new(pdfFileStream),
                    pdfFileStreamLength,
                    (_) => ValueTask.FromResult<(Stream, long)>(new(new MemoryStream(), -1)),
                    context.CancellationToken
                    );

            await this.stampingProcessor.AddStampAsync(
                    stampingContext,
                    context.CancellationToken);

            if (!stampingContext.ValidationResult.IsSuccessful())
            {
                context.Response!.ValidationResult.Add(stampingContext.ValidationResult);
                return (null, (null, -1));
            }

            return (stampingContext.PagesAndImageNames, await stampingContext.GetOutputContentAsync(context.CancellationToken));
        }

        private async Task<Guid?> SaveFileAsync(Guid cardID, Guid fileID, Stream content, ICardRequestExtensionContext context)
        {
            var resp = await this.cardRepository.GetAsync(
                new CardGetRequest
                {
                    CardID = cardID
                },
                context.CancellationToken);

            if (!resp.ValidationResult.IsSuccessful())
            {
                context.Response!.ValidationResult.Add(resp.ValidationResult);
                return null;
            }


            if (resp.TryGetCard() is { } cardWithFiles)
            {
                await using var cardContainer = await this.fileManager.CreateContainerAsync(cardWithFiles, cancellationToken: context.CancellationToken);
                if (cardContainer.FileContainer.Files.TryGet(fileID) is { } file)
                {
                    var fileName = Guid.NewGuid().ToString();
                    using (var tempFile = TempFile.Acquire(fileName))
                    {
                        await using var fileStream = FileHelper.Create(tempFile.Path);
                        await content.CopyToAsync(fileStream, context.CancellationToken);
                        fileStream.Close();

                        var validationResult = await file.ReplaceAsync(tempFile.Path, cancellationToken: context.CancellationToken);
                        if (!validationResult.IsSuccessful)
                        {
                            context.Response!.ValidationResult.Add(validationResult);
                            return null;
                        }
                    }

                    var storeResponse = await cardContainer.StoreAsync(
                        (_, request, _) =>
                        {
                            request.ServiceType = CardServiceType.Default;
                            return new ValueTask();
                        },
                        cancellationToken: context.CancellationToken);
                    if (!storeResponse.ValidationResult.IsSuccessful())
                    {
                        context.Response!.ValidationResult.Add(storeResponse.ValidationResult);
                        return null;
                    }

                    return file.Versions[^1].ID;
                }
            }

            return null;
        }

        private static string? DeleteBase64Preambule(string? input)
        {
            if (input is not { })
            {
                return input;
            }
            var index = input.IndexOf(',', StringComparison.OrdinalIgnoreCase);
            return index == -1 ? input : input[(index + 1)..];
        }

        #endregion
    }
}
