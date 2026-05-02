#nullable enable

using System;
using System.IO;
using Tessa.Platform.Expressions;
using Tessa.Platform.Placeholders;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Объект для создания объекта документа для обработки плейсхолдеров в файлах Word.
    /// </summary>
    public sealed class WordPlaceholderDocumentBuilder
    {
        #region Fields

        private readonly IWordDocumentParser wordDocumentParser;
        private readonly IExpressionInterpreterProvider expressionInterpreterPovider;
        private readonly IWordDocumentMoveProcessor moveProcessor;

        #endregion

        #region Constructors

        public WordPlaceholderDocumentBuilder(
            IWordDocumentParser wordDocumentParser,
            IExpressionInterpreterProvider expressionInterpreterPovider,
            IWordDocumentMoveProcessor moveProcessor)
        {
            this.wordDocumentParser = NotNullOrThrow(wordDocumentParser);
            this.expressionInterpreterPovider = NotNullOrThrow(expressionInterpreterPovider);
            this.moveProcessor = NotNullOrThrow(moveProcessor);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Создаёт объект документа для обработки плейсхолдеров и возвращает в <paramref name="getDocumentContentFunc"/> функцию получения данных обработанного документа.
        /// </summary>
        /// <param name="templateCardID">Идентификатор шаблона файлов.</param>
        /// <param name="documentStream">Поток с исходными данными документа.</param>
        /// <param name="getDocumentContentFunc">Функцию получения данных обработанного документа</param>
        /// <returns>Объекта документа для обработки плейсхолдеров в файлах Word.</returns>
        public IPlaceholderDocument Build(
            Guid templateCardID,
            MemoryStream documentStream,
            out Func<IPlaceholderDocument, byte[]> getDocumentContentFunc)
        {
            ThrowIfNull(documentStream);

            var document = new WordPlaceholderDocument(
                documentStream,
                templateCardID,
                this.wordDocumentParser,
                this.expressionInterpreterPovider,
                this.moveProcessor);

            getDocumentContentFunc = x => ((WordPlaceholderDocument) x).Stream.ToArray();

            return document;
        }

        #endregion
    }
}
