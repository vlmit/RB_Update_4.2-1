using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Tessa.Platform;
using Tessa.Platform.Placeholders;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Вспомогательные методы для работы с документами формата OpenXml: .docx, .xlsx.
    /// </summary>
    public static class OpenXmlHelper
    {
        #region Static Fields

        /// <summary>
        /// Ширина вставляемой картинки по умолчанию в Emu, когда никакого другого способа определить её размер нет.
        /// </summary>
        public const long DefaultExtentsCx = 990000L;

        /// <summary>
        /// Высота вставляемой картинки по умолчанию в Emu, когда никакого другого способа определить её размер нет.
        /// </summary>
        public const long DefaultExtentsCy = 792000L;

        /// <summary>
        /// Определяет константу для хранения в Info текста плейсхолдера
        /// </summary>
        public const string TextField = "Text";

        /// <summary>
        /// Определяет константу для хранения в Info позиции плейсхолдера
        /// </summary>
        public const string PositionField = "Position";

        /// <summary>
        /// Определяет константу для хранения индекса в результате в Info плейсхолдера
        /// </summary>
        public const string IndexField = "Index";

        /// <summary>
        /// Определяет константу для хранения элемента документа в Info плейсхолдера
        /// </summary>
        public const string BaseElementField = "BaseElement";

        /// <summary>
        /// Определяет константу для хранения в Info плейсхолдера позиции плейсхолдера в элементе документа
        /// </summary>
        public const string OrderField = "Order";

        /// <summary>
        /// Значение определяет принадлежность данного пути плейсхолдера к гиперссылке.
        /// </summary>
        public const string HyperlinkPath = "hyper";

        /// <summary>
        /// Компаратор для сравнения позиций элементов в документе Word, в котором позиция родительского элемента считается меньше чем позиция любого дочернего элемента.
        /// </summary>
        public static IComparer<List<int>> PositionComparer = Comparer<List<int>>.Create((x, y) => Compare(x, y, -1, 1));

        #endregion

        #region Static Methods

        /// <summary>
        /// Метод для получения элемента по позиции в документе
        /// </summary>
        /// <param name="mainPart">Объект документа, в котором производится поиск элемента</param>
        /// <param name="position">Координаты объекта в дереве документа</param>
        /// <returns>Возвращает базовый элемент, который располагается по данной позиции</returns>
        public static OpenXmlElement GetElementByPosition(OpenXmlPart mainPart, IList position)
        {
            OpenXmlElement resultElement = mainPart.RootElement;
            OpenXmlPart part = mainPart;
            foreach (int index in position)
            {
                if (index < 0)
                {
                    part = part.Parts.ElementAt(~index).OpenXmlPart;
                    resultElement = part.RootElement;
                }
                else
                {
                    resultElement = resultElement.ChildElements[index];
                }
            }

            return resultElement;
        }

        public static PartTypeInfo GetImagePartTypeFromPlaceholder(string placeholderImageType)
        {
            switch (placeholderImageType)
            {
                case PlaceholderImageTypes.Bmp:
                    return ImagePartType.Bmp;

                case PlaceholderImageTypes.Emf:
                    return ImagePartType.Emf;

                case PlaceholderImageTypes.Gif:
                    return ImagePartType.Gif;

                case PlaceholderImageTypes.Icon:
                    return ImagePartType.Icon;

                case PlaceholderImageTypes.Jpeg:
                    return ImagePartType.Jpeg;

                case PlaceholderImageTypes.Png:
                    return ImagePartType.Png;

                case PlaceholderImageTypes.Tiff:
                    return ImagePartType.Tiff;

                case PlaceholderImageTypes.Wmf:
                    return ImagePartType.Wmf;

                default:
                    //case PlaceholderImageTypes.Exif:
                    //case PlaceholderImageTypes.Unknown:
                    return ImagePartType.Png;
            }
        }


        public static PartTypeInfo GetImagePartType(IPlaceholderImageParameters imageParameters)
        {
            return GetImagePartTypeFromPlaceholder(imageParameters.ImageType);
        }


        public static string GetPlaceholderImageType(PartTypeInfo imagePartType)
        {
            if (EqualsByContentType(imagePartType, ImagePartType.Bmp))
            {
                return PlaceholderImageTypes.Bmp;
            }

            if (EqualsByContentType(imagePartType, ImagePartType.Gif))
            {
                return PlaceholderImageTypes.Gif;
            }

            if (EqualsByContentType(imagePartType, ImagePartType.Png))
            {
                return PlaceholderImageTypes.Png;
            }

            if (EqualsByContentType(imagePartType, ImagePartType.Tiff))
            {
                return PlaceholderImageTypes.Tiff;
            }

            if (EqualsByContentType(imagePartType, ImagePartType.Icon))
            {
                return PlaceholderImageTypes.Icon;
            }

            if (EqualsByContentType(imagePartType, ImagePartType.Jpeg))
            {
                return PlaceholderImageTypes.Jpeg;
            }

            if (EqualsByContentType(imagePartType, ImagePartType.Emf))
            {
                return PlaceholderImageTypes.Emf;
            }

            if (EqualsByContentType(imagePartType, ImagePartType.Wmf))
            {
                return PlaceholderImageTypes.Wmf;
            }

            return PlaceholderImageTypes.Unknown;

            static bool EqualsByContentType(PartTypeInfo first, PartTypeInfo second) =>
                string.Equals(first.ContentType, second.ContentType, StringComparison.Ordinal);
        }

        public static Int64Value PixelsToEmu(double pixels)
        {
            // в OpenXML используется единица измерения EMU: http://polymathprogrammer.com/2009/10/22/english-metric-units-and-open-xml/
            // в дюйме 914400 EMU, а заданные в плейсхолдере размеры считаем для 96 dpi
            // отсюда: EMU = pixel * 914400 / 96 = pixel * 9525

            return (long) pixels * 9525L;
        }


        public static Int64Value PixelsToEmu(int pixels, double resolution)
            => pixels * (long) (914400f / resolution);


        public static T FindParent<T>(OpenXmlElement element)
            where T : OpenXmlElement
        {
            OpenXmlElement current = element;

            while ((current = current.Parent) != null)
            {
                // сам родительский элемент является типом T
                if (current is T t)
                {
                    return t;
                }
            }

            return null;
        }


        public static T FindInParentChildren<T>(OpenXmlElement element)
            where T : OpenXmlElement
        {
            OpenXmlElement current = element;

            while ((current = current.Parent) != null)
            {
                // сам родительский элемент является типом T
                if (current is T t)
                {
                    return t;
                }

                // в родительском элементе в одном из его непосредственных детей есть объект
                T child = current.GetFirstChild<T>();
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }


        public static void AddPlaceholdersFromText(
            List<IPlaceholderText> result,
            string allTextString,
            IList position,
            bool hasOrder = true)
        {
            int order = 0;
            foreach (Match match in PlaceholderHelper.UnescapedPlaceholdersRegex.Matches(allTextString))
            {
                string text = match.Value;
                string value = PlaceholderHelper.TryGetValue(text);

                if (value != null)
                {
                    var newPlaceholder = new PlaceholderText(text, value);
                    newPlaceholder.Info[PositionField] = position;
                    newPlaceholder.Info[IndexField] = match.Index;

                    if (hasOrder)
                    {
                        newPlaceholder.Info[OrderField] = order++;
                    }

                    result.Add(newPlaceholder);
                }
            }
        }

        public static string TextPosition(IEnumerable position)
        {
            StringBuilder result = StringBuilderHelper.Acquire();

            foreach (object i in position)
            {
                result
                    .Append(i)
                    .Append("->");
            }

            return result
                .Remove(result.Length - 2, 2)
                .ToStringAndRelease();
        }

        /// <summary>
        /// Определяет, находится ли позиция первого элемента раньше или там же, что и позиция второго элемента в структуру документа OpenXML.
        /// </summary>
        /// <param name="position1">Позиция первого элемента.</param>
        /// <param name="position2">Позиция второго элемента.</param>
        /// <param name="resultIfPosition2ContainsPosition1">Результат в ситуации, когда второй элемент содержит в себе первый элемент.</param>
        /// <param name="positionInText1">Индекс позиции в тексте первого элемента. Не задаётся, если позиции сравниваются без индексов внутри текста.</param>
        /// <param name="positionInText2">Индекс позиции в тексте второго элемента. Не задаётся, если позиции сравниваются без индексов внутри текста.</param>
        /// <returns>Значение <c>true</c>, если первый элемент в структуре OpenXML находится раньше, чем второй элемент, или там же.</returns>
        /// <exception cref="ArgumentException">Возникает в ситуации, когда идёт сравнение элементов из разных частей документа.</exception>
        /// <remarks>
        /// Сравнение по индексам производится только в ситуации, когда все координаты элемента в дереве, кроме последней, совпадают между собой.
        /// Сравнение по индексам заменяет собой сравнение по последней координате.
        /// Используется для сравнения позиций элементов, который могут иметь разные координаты в документе, но при этом находиться на одной позиции в тексте (закладки, комментарии, другие элементы, не содержащие текст).
        /// </remarks>
        public static bool IsLessOrEquals(
            IList position1,
            IList position2,
            bool resultIfPosition2ContainsPosition1 = false,
            int? positionInText1 = null,
            int? positionInText2 = null)
        {
            return Compare(
                position1,
                position2,
                0,
                resultIfPosition2ContainsPosition1 ? 0 : 1,
                positionInText1,
                positionInText2) <= 0;
        }

        /// <summary>
        /// Выполняет сравнение позиций элементов в структуре OpenXML.
        /// </summary>
        /// <param name="position1">Позиция первого элемента.</param>
        /// <param name="position2">Позиция второго элемента.</param>
        /// <param name="position1ContainsPosition2">Результат в ситуации, когда первый элемент содержит в себе второй элемент.</param>
        /// <param name="position2ContainsPosition1">Результат в ситуации, когда второй элемент содержит в себе первый элемент.</param>
        /// <param name="positionInText1">Индекс позиции в тексте первого элемента. Не задаётся, если позиции сравниваются без индексов внутри текста.</param>
        /// <param name="positionInText2">Индекс позиции в тексте второго элемента. Не задаётся, если позиции сравниваются без индексов внутри текста.</param>
        /// <returns>
        /// Результат сравнение позиций элементов между собой, где:
        /// <para>-1 - первый элемент в структуре OpenXML находится раньше, чем второй элемент.</para>
        /// <para>0 - оба элемента находятся на одной и той же позиции.</para>
        /// <para>1 - первый элемент в структуре OpenXML находится позже, чем второй элемент.</para>
        /// <para><paramref name="position1ContainsPosition2"/> - в ситуации, когда первый элемент содержит второй элемент.</para>
        /// <para><paramref name="position2ContainsPosition1"/> - в ситуации, когда второй элемент содержит первый элемент.</para>
        /// </returns>
        /// <exception cref="ArgumentException">Возникает в ситуации, когда идёт сравнение элементов из разных частей документа.</exception>
        /// <remarks>
        /// Сравнение по индексам производится только в ситуации, когда все координаты элемента в дереве, кроме последней, совпадают между собой.
        /// Сравнение по индексам заменяет собой сравнение по последней координате.
        /// Используется для сравнения позиций элементов, который могут иметь разные координаты в документе, но при этом находиться на одной позиции в тексте (закладки, комментарии, другие элементы, не содержащие текст).
        /// </remarks>
        public static int Compare(
            IList position1,
            IList position2,
            int position1ContainsPosition2 = 0,
            int position2ContainsPosition1 = 0,
            int? positionInText1 = null,
            int? positionInText2 = null)
        {
            for (int i = 0; i < position1.Count; i++)
            {
                if (position2.Count <= i)
                {
                    return position2ContainsPosition1;
                }

                if (i == position1.Count - 1
                    && i == position2.Count - 1
                    && positionInText1 is not null
                    && positionInText2 is not null)
                {
                    return positionInText1.Value.CompareTo(positionInText2.Value);
                }

                var index1 = (int) position1[i];
                var index2 = (int) position2[i];

                // Если индекс меньше нуля, это Part, который определяет источник данных
                if (index1 < 0
                    || index2 < 0)
                {
                    if (index1 == index2)
                    {
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException($"{nameof(position1)} and {(nameof(position2))} belong to different parts.");
                    }
                }

                if (index1 > index2)
                {
                    return 1;
                }
                else if (index1 < index2)
                {
                    return -1;
                }
            }

            return position1ContainsPosition2;
        }

        public static bool HasSameSubtree(List<int> position1, List<int> position2, int checkDepth)
        {
            if (position1.Count < checkDepth
                || position2.Count < checkDepth)
            {
                return false;
            }

            for (int i = 0; i < checkDepth; i++)
            {
                if (position1[i] != position2[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Выполняет проверку, что переданные позиции элементов относятся к одной и той же части документа.
        /// </summary>
        /// <param name="position1">Позиция первого элемента.</param>
        /// <param name="position2">Позиция второго элемента.</param>
        /// <returns>Значение <c>true</c>, если обе позиции относятся к одной и той же части документа, иначе <c>false</c>.</returns>
        public static bool HasSamePart(List<int> position1, List<int> position2)
        {
            for (int i = 0; i < position1.Count; i++)
            {
                if (position2.Count <= i)
                {
                    return false;
                }

                var index1 = position1[i];
                var index2 = position2[i];

                if (index1 < 0
                    && index2 < 0)
                {
                    if (index1 != index2)
                    {
                        return false;
                    }
                }
                else if (index1 < 0 || index2 < 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Метод для установки для пространства имён в пакете префикса.
        /// </summary>
        /// <param name="package">Пакет документа OpenXML.</param>
        /// <param name="namespace">Пространство имён.</param>
        /// <param name="prefix">Префикс.</param>
        public static void TryUpdateNamespace(OpenXmlPackage package, string @namespace, string prefix)
        {
            const string nameResolverTypeName = "IOpenXmlNamespaceResolver";
            const string getNamespacesMethodName = "GetNamespacesInScope";

            var (type, resolverObj) = package.Features.FirstOrDefault(x => x.Key.Name == nameResolverTypeName);
            if (resolverObj is { }
                && resolverObj.GetType().GetMethod(getNamespacesMethodName) is { } getNamespacesMethod)
            {
                try
                {
                    var namespaceMapping = (IDictionary<string, string>) getNamespacesMethod.Invoke(resolverObj, [null]);
                    namespaceMapping[@namespace] = prefix;
                }
                catch
                {
                    // ignore
                }
            }
        }

        /// <summary>
        /// Возвращает текущий элемент или первый родительский элемент по дереву вверх относительно текущего, который имеет тип <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Искомый тип элемента.</typeparam>
        /// <param name="element">Проверяемый элемент.</param>
        /// <param name="depthDiff">Разница глубины в дереве элементов найденного элемента от текущего.</param>
        /// <returns>Найденный элемент или <c>null</c>, если элемент не найден.</returns>
        public static T? GetSelfOrParent<T>(OpenXmlElement element, out int depthDiff) where T : OpenXmlElement
        {
            depthDiff = 0;
            while (element is not null)
            {
                if (element is T tElement)
                {
                    return tElement;
                }

                depthDiff++;
                element = element.Parent;
            }

            return null;
        }

        #endregion
    }
}
