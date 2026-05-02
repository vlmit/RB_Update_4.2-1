using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.TypeSettings;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.Workflow.KrPermissions
{
    /// <summary>
    /// Устанавливает в параметре <see cref="CardTypeExtensionSettings.TokenParameterAlias"/> представления, указанного по ключу <see cref="CardTypeExtensionSettings.ViewControlAlias"/>, информацию о токене безопасности <see cref="KrToken"/>, выданного для карточки.
    /// </summary>
    public sealed class KrTokenToTaskHistoryViewUIExtension :
        CardUIExtension
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override async Task Initializing(ICardUIExtensionContext context)
        {
            var result = await CardHelper
                .ExecuteTypeExtensionsAsync(
                    DefaultCardTypeExtensionTypes.MakeViewTaskHistory,
                    context.Card,
                    context.Model.CardMetadata,
                    ExecuteInitializingAsync,
                    context,
                    cancellationToken: context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        #endregion

        #region Type extension methods

        private static Task ExecuteInitializingAsync(ITypeExtensionContext typeContext)
        {
            if (typeContext.ExternalContext is not ICardUIExtensionContext context)
            {
                return Task.CompletedTask;
            }

            var settings = typeContext.Settings;
            var tokenParameterAlias = settings?.TryGet<string>(CardTypeExtensionSettings.TokenParameterAlias);
            if (string.IsNullOrWhiteSpace(tokenParameterAlias))
            {
                return Task.CompletedTask;
            }

            var viewControlAlias = settings.TryGet<string>(CardTypeExtensionSettings.ViewControlAlias);

            // do not use closure for context/typeContext
            context.Model.ControlInitializers.Add((control, m, r, ct) =>
            {
                if (control is CardViewControlViewModel { ViewMetadata: { } viewMetadata, Parameters: { } parameters } viewControl
                    && viewControl.Name == viewControlAlias
                    && viewMetadata.Parameters.IsDefinedByName(tokenParameterAlias)
                    && KrToken.TryGet(m.Card.Info) is { } token)
                {
                    parameters.Add(new RequestParameter(tokenParameterAlias).Add(EqualsToCriteriaOperator.Instance, token.ToTypedJson()));
                }

                return ValueTask.CompletedTask;
            });

            return Task.CompletedTask;
        }

        #endregion
    }
}
