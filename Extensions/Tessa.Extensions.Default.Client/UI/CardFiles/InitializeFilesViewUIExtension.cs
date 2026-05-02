#nullable enable

using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Client.UI.CardFiles
{
    /// <summary>
    /// Реализация расширения типа карточки "Список файлов в представлении" для карточки.
    /// </summary>
    public sealed class InitializeFilesViewUIExtension : CardUIExtension
    {
        #region Private fields

        private readonly IViewCardControlInitializationStrategy initializationStrategy;
        private readonly IExtensionContainer extensionContainer;
        private readonly ISession session;
        private readonly IViewService viewService;
        private readonly IProcessNameResolver processNameResolver;
        private readonly ICardMetadata cardMetadata;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="InitializeFilesViewUIExtension"/>.
        /// </summary>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="extensionContainer"><inheritdoc cref="IExtensionContainer" path="/summary"/></param>
        /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
        /// <param name="processNameResolver"><inheritdoc cref="IProcessNameResolver" path="/summary"/></param>
        /// <param name="initializationStrategy"><inheritdoc cref="IViewCardControlInitializationStrategy" path="/summary"/></param>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        public InitializeFilesViewUIExtension(
            ISession session,
            IExtensionContainer extensionContainer,
            IViewService viewService,
            IProcessNameResolver processNameResolver,
            ICardMetadata cardMetadata,
            [Dependency(nameof(FilesViewCardControlInitializationStrategy))] IViewCardControlInitializationStrategy initializationStrategy)
        {
            this.session = NotNullOrThrow(session);
            this.extensionContainer = NotNullOrThrow(extensionContainer);
            this.viewService = NotNullOrThrow(viewService);
            this.processNameResolver = NotNullOrThrow(processNameResolver);
            this.initializationStrategy = NotNullOrThrow(initializationStrategy);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
        }

        #endregion

        #region Private methods

        private async Task ExecuteInitializingActionAsync(ITypeExtensionContext context)
        {
            var uiContext = (ICardUIExtensionContext) NotNullOrThrow(context.ExternalContext);
            FilesViewGeneratorHelper.ProcessInitializingFilesView(
                this.session,
                this.extensionContainer,
                this.viewService,
                this.processNameResolver,
                context,
                uiContext.Model
            );
        }

        private async Task ExecuteInitializedActionAsync(ITypeExtensionContext context)
        {
            var uiContext = (ICardUIExtensionContext) NotNullOrThrow(context.ExternalContext);
            await FilesViewGeneratorHelper.ProcessInitializedFilesView(
                    this.initializationStrategy,
                    this.session,
                    this.extensionContainer,
                    this.viewService,
                    this.processNameResolver,
                    context,
                    uiContext.Model
                );
        }

        #endregion

        #region Base overrides

        /// <inheritdoc />
        public override async Task Initializing(ICardUIExtensionContext context)
        {
            ValidationResult result = await CardHelper
                .ExecuteTypeExtensionsAsync(
                    DefaultCardTypeExtensionTypes.InitializeFilesView,
                    context.Card,
                    this.cardMetadata,
                    this.ExecuteInitializingActionAsync,
                    context,
                    cancellationToken: context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        /// <inheritdoc />
        public override async Task Initialized(ICardUIExtensionContext context)
        {
            // Вот здесь проходит основная инициализация. Она не запускается при открытии мелкой карточки.
            ValidationResult result = await CardHelper
                .ExecuteTypeExtensionsAsync(
                    DefaultCardTypeExtensionTypes.InitializeFilesView,
                    context.Model.Card,
                    this.cardMetadata,
                    this.ExecuteInitializedActionAsync,
                    context);

            context.ValidationResult.Add(result);
        }

        #endregion
    }
}
