#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Platform.Shared.Cards;
using Tessa.Files;
using Tessa.Localization;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Workflow.Actions;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="StageTypeDescriptors.AddFromTemplateDescriptor"/>.
    /// </summary>
    /// <param name="cardStreamRepository"><inheritdoc cref="CardStreamRepository" path="/summary"/></param>
    /// <param name="placeholderManager"><inheritdoc cref="PlaceholderManager" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="DbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="Session" path="/summary"/></param>
    /// <param name="unityContainer"><inheritdoc cref="UnityContainer" path="/summary"/></param>
    /// <param name="krScope"><inheritdoc cref="KrScope" path="/summary"/></param>
    public class AddFromTemplateStageTypeHandler(
        ICardStreamServerRepository cardStreamRepository,
        IPlaceholderManager placeholderManager,
        IDbScope dbScope,
        ISession session,
        IUnityContainer unityContainer,
        IKrScope krScope) :
        StageTypeHandlerBase
    {
        #region Properties

        /// <inheritdoc cref="ICardStreamServerRepository" path="/summary"/>
        protected ICardStreamServerRepository CardStreamRepository { get; } = NotNullOrThrow(cardStreamRepository);

        /// <inheritdoc cref="IPlaceholderManager" path="/summary"/>
        protected IPlaceholderManager PlaceholderManager { get; } = NotNullOrThrow(placeholderManager);

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        protected IDbScope DbScope { get; } = NotNullOrThrow(dbScope);

        /// <summary>
        /// Unity-контейнер.
        /// </summary>
        protected IUnityContainer UnityContainer { get; } = NotNullOrThrow(unityContainer);

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; } = NotNullOrThrow(session);

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope KrScope { get; } = NotNullOrThrow(krScope);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<StageHandlerResult> HandleStageStartAsync(
            IStageTypeHandlerContext context)
        {
            var templateID = context.Stage.SettingsStorage.TryGet<Guid?>(KrConstants.KrAddFromTemplateSettingsVirtual.FileTemplateID);
            var fileName = LocalizationManager.EscapeIfLocalizationString(
                context.Stage.SettingsStorage.TryGet<string>(KrConstants.KrAddFromTemplateSettingsVirtual.Name));
            var fileCategoryID = context.Stage.SettingsStorage.TryGet<Guid?>(KrConstants.KrAddFromTemplateSettingsVirtual.FileCategoryID);
            var fileCategoryName = context.Stage.SettingsStorage.TryGet<string>(KrConstants.KrAddFromTemplateSettingsVirtual.FileCategoryName);

            if (templateID.HasValue)
            {
                var result = await this.CardStreamRepository.GenerateFileFromTemplateAsync(
                    templateID.Value,
                    context.MainCardID,
                    info: new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        [PlaceholderHelper.CardFuncAsyncKey] = new Func<CancellationToken, ValueTask<Card>>(ct =>
                            context.MainCardAccessStrategy.GetCardAsync(
                                withoutTransaction: true,
                                cancellationToken: ct)),
                    },
                    cancellationToken: context.CancellationToken);

                context.ValidationResult.Add(result.Response.ValidationResult);

                if (result.HasContent)
                {
                    var fileContainer = await context.MainCardAccessStrategy.GetFileContainerAsync(cancellationToken: context.CancellationToken);

                    if (fileContainer is null)
                    {
                        return StageHandlerResult.EmptyResult;
                    }

                    await using var s = await result.GetContentOrThrowAsync(context.CancellationToken);
                    var data = await s.ReadAllBytesAsync(context.CancellationToken);

                    var fileBuilder = fileContainer
                        .FileContainer
                        .BuildFile(await this.GetFileNameAsync(context, result.Response.TryGetSuggestedFileName() ?? string.Empty, fileName))
                        .SetContent(data);

                    if (!string.IsNullOrWhiteSpace(fileCategoryName))
                    {
                        fileBuilder.SetCategory(fileCategoryName, fileCategoryID);
                    }

                    var generatedFile = await fileBuilder.AddWithNotificationAsync(cancellationToken: context.CancellationToken);

                    if (generatedFile.File is { } file && generatedFile.Result.IsSuccessful)
                    {
                        if (result.Response.Info.TryGetValue(FileTemplateHelper.FileOptionsKey, out var optionsObject)
                            && optionsObject is Dictionary<string, object?> options)
                        {
                            StorageHelper.Merge(options, file.Options);
                            await file.NotifyAsync(FileNotificationType.OptionsModified);
                        }

                        context.Info[WorkflowActionsHelper.FileTemplateIDKey] = templateID.Value;
                        context.Info[WorkflowActionsHelper.GeneratedFileIDKey] = file.ID;

                        if (fileCategoryID.HasValue)
                        {
                            context.Info[WorkflowActionsHelper.FileCategoryIDKey] = fileCategoryID.Value;
                        }

                        if (fileCategoryName is not null)
                        {
                            context.Info[WorkflowActionsHelper.FileCategoryNameKey] = fileCategoryName;
                        }
                    }
                }
            }

            return StageHandlerResult.CompleteResult;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Возвращает имя создаваемого файла.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="suggestedName">Предпочитаемое имя файла, которое используется для загрузки предпросмотра или создания файла по шаблону, или <see langword="null"/>, если используется уже известное имя файла (то, которое задано в шаблоне).</param>
        /// <param name="fileNameTemplate">Имя файла заданное в шаблоне.</param>
        /// <returns>Имя создаваемого файла.</returns>
        protected async Task<string> GetFileNameAsync(
            IStageTypeHandlerContext context,
            string suggestedName,
            string? fileNameTemplate)
        {
            if (string.IsNullOrWhiteSpace(fileNameTemplate))
            {
                return suggestedName;
            }

            var extension = Path.GetExtension(suggestedName);

            return await this.ExtendFileNameAsync(context, fileNameTemplate) + extension;
        }

        /// <summary>
        /// Заменяет плейсхолдеры в указанном имени файла.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="fileNameTemplate">Имя файла заданное в шаблоне.</param>
        /// <returns>Имя файла в котором заменены плейсхолдеры.</returns>
        protected async Task<string?> ExtendFileNameAsync(IStageTypeHandlerContext context, string fileNameTemplate) =>
            await this.PlaceholderManager.ReplaceTextAsync(
                fileNameTemplate,
                this.Session,
                this.UnityContainer,
                this.DbScope,
                null,
                await context.MainCardAccessStrategy.GetCardAsync(cancellationToken: context.CancellationToken),
                info: CreatePlaceholderInfo(context),
                cancellationToken: context.CancellationToken);

        /// <summary>
        /// Создаёт дополнительную информацию, добавляемая в info замены плейсхолдеров.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Дополнительная информацию, добавляемая в info замены плейсхолдеров.</returns>
        protected static Dictionary<string, object?> CreatePlaceholderInfo(
            IStageTypeHandlerContext context) =>
            new(StringComparer.Ordinal)
            {
                [PlaceholderHelper.TaskKey] = context.TaskInfo?.Task,
                ["WorkflowProcess"] = context.WorkflowProcess,
                ["Stage"] = context.Stage,
            };

        #endregion
    }
}
