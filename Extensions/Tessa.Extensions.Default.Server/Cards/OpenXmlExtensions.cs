#nullable enable
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Header = DocumentFormat.OpenXml.Wordprocessing.Header;

namespace Tessa.Extensions.Default.Server.Cards
{
    public static class OpenXmlExtensions
    {
        /// <summary>
        /// Безопасное удаление элемента. Удаляет элемент в случае, если он прикреплен к родительскому элементу
        /// </summary>
        /// <param name="element">Удаляемый элемент</param>
        public static void SafeRemove(this OpenXmlElement element)
        {
            if (element.Parent is not null)
            {
                element.Remove();
            }
        }

        /// <summary>
        /// Метод для получения OpenXmlPart по OpenXmlElement
        /// </summary>
        /// <param name="element">Элемент, относительно которого ведется поиск OpenXmlPart</param>
        /// <returns>Возвращает первый родительский OpenXmPart, или null, если OpenXmlPart не найден.</returns>
        public static OpenXmlPart? GetPart(this OpenXmlElement element)
        {
            var checkElement = element;
            do
            {
                if (checkElement is OpenXmlPartRootElement partRootElement)
                {
                    switch (partRootElement)
                    {
                        case Document document:
                            return document.MainDocumentPart;

                        case Footer footer:
                            return footer.FooterPart;

                        case Header header:
                            return header.HeaderPart;

                        case Workbook workbook:
                            return workbook.WorkbookPart;
                    }
                }

                checkElement = checkElement.Parent;
            } while (checkElement is not null);

            return null;
        }

        public static ImagePart? TryAddImagePart(this OpenXmlPart openXmlPart, PartTypeInfo partType) =>
            openXmlPart switch
            {
                // important ones
                MainDocumentPart p => p.AddImagePart(partType),
                FooterPart p => p.AddImagePart(partType),
                HeaderPart p => p.AddImagePart(partType),

                // others in alphabet order
                ChartDrawingPart p => p.AddImagePart(partType),
                ChartPart p => p.AddImagePart(partType),
                ChartsheetPart p => p.AddImagePart(partType),
                DiagramDataPart p => p.AddImagePart(partType),
                DiagramLayoutDefinitionPart p => p.AddImagePart(partType),
                DiagramPersistLayoutPart p => p.AddImagePart(partType),
                DocumentSettingsPart p => p.AddImagePart(partType),
                DrawingsPart p => p.AddImagePart(partType),
                EndnotesPart p => p.AddImagePart(partType),
                ExtendedChartPart p => p.AddImagePart(partType),
                FootnotesPart p => p.AddImagePart(partType),
                GlossaryDocumentPart p => p.AddImagePart(partType),
                HandoutMasterPart p => p.AddImagePart(partType),
                InternationalMacroSheetPart p => p.AddImagePart(partType),
                MacroSheetPart p => p.AddImagePart(partType),
                NotesMasterPart p => p.AddImagePart(partType),
                NotesSlidePart p => p.AddImagePart(partType),
                NumberingDefinitionsPart p => p.AddImagePart(partType),
                RibbonAndBackstageCustomizationsPart p => p.AddImagePart(partType),
                RibbonExtensibilityPart p => p.AddImagePart(partType),
                SlideLayoutPart p => p.AddImagePart(partType),
                SlideMasterPart p => p.AddImagePart(partType),
                SlidePart p => p.AddImagePart(partType),
                ThemeOverridePart p => p.AddImagePart(partType),
                ThemePart p => p.AddImagePart(partType),
                VmlDrawingPart p => p.AddImagePart(partType),
                WebExtensionPart p => p.AddImagePart(partType),
                WordprocessingCommentsExPart p => p.AddImagePart(partType),
                WordprocessingCommentsIdsPart p => p.AddImagePart(partType),
                WordprocessingCommentsPart p => p.AddImagePart(partType),
                WorksheetPart p => p.AddImagePart(partType),
                _ => null
            };
    }
}
