#nullable enable

using Newtonsoft.Json;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.AiViewSearch;
using Tessa.Ai.Models;
using Tessa.Ai.Prompts;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Properties.Resharper;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для получения информации по замещениям сотрудников.
    /// </summary>
    /// <param name="aiEmployeeSearchService"><inheritdoc cref="IAiEmployeeSearchService" path="/summary"/></param>
    /// <param name="viewService"><inheritdoc cref="ICurrentUserViewService" path="/summary"/></param>>
    public sealed class DeputyInfoAiAgentPlugin(
        IAiEmployeeSearchService aiEmployeeSearchService,
        ICurrentUserViewService viewService,
        IDbScope dbScope) : IAiAgentPlugin
    {
        #region Nested Types

        /// <summary>
        /// Данные для взаимодействия с AI моделью.
        /// </summary>
        private class DeputiesInfoAiResult
        {
            [Display(
                Name = "Список сотрудников",
                Description = "Сотрудники, для которых выполняется поиск информации по замещениям (может быть в виде Фамилия, или Фамилия Имя, или Фамилия Имя Отчество). Если пользователь спрашивает про себя, то возьми ФИО пользователя.")]
            [UsedImplicitly]
            public List<AiEmployee>? Deputies { get; set; }
        }

        /// <summary>
        /// Данные после распознания результатов из AI модели и поиска через <see cref="IAiEmployeeSearchService"/>.
        /// </summary>
        private class DeputiesInfo : StorageSerializable
        {
            /// <summary>
            /// Распознанные заместители.
            /// </summary>
            public List<AiEmployeeInfo>? Deputies { get; set; }

            #region Storage serializable

            /// <inheritdoc />
            protected override void SerializeCore(Dictionary<string, object?> storage)
            {
                storage.SetIfNotNull(nameof(this.Deputies), ToObjectList(this.Deputies));
            }

            /// <inheritdoc />
            protected override void DeserializeCore(Dictionary<string, object?> storage)
            {
                this.Deputies = GetObjectList<AiEmployeeInfo>(storage, nameof(this.Deputies));
            }

            #endregion
        }

        #endregion

        #region Constants

        private const string RoleDeputiesViewName = "RoleDeputies";
        private const string RoleDeputiesByDocTypesViewName = "RoleDeputiesNewByDocTypes";
        private const string AvailableOnDateParamName = "AvailableOnDate";
        private const string IsActiveParamName = "IsActive";
        private const string DeputyParamName = "Deputy";
        private const string DeputizedParamName = "Deputized";

        private const string ToolPrompt =
            """
            Ты умный помощник, который из запроса пользователя вычленяет необходимые параметры, которые в дальнейшем используются в информационной системе. 
            Тебе не нужно искать заместителей, только дай ответ, о ком спрашивает пользователь.
            """;

        #endregion

        #region Fields

        private readonly IAiEmployeeSearchService aiEmployeeSearchService = NotNullOrThrow(aiEmployeeSearchService);
        private readonly ICurrentUserViewService viewService = NotNullOrThrow(viewService);
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private AiCachedToolSettings? baseSettings;
        private AiCachedPrompts? cachedPrompts;

        #endregion

        #region IAiAgentPlugin Members

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            [(await this.GetBaseSettingsAsync(cancellationToken)).Settings];

        /// <inheritdoc />
        public async ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<DeputyInfoAiAgentPlugin>(toolId, toolInfo.ID);
            return toolInfo;
        }

        /// <inheritdoc />
        public async ValueTask<AiToolApplicability> IsApplicableAsync(string toolId, AiRequest? request, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            if (toolId != toolInfo.ID)
            {
                return new AiToolApplicability();
            }

            // Инструмент глобальный, то есть не содержит контекста (либо явное указание на это, либо его фактическое отсутствие)
            if (request is not null)
            {
                return
                    request.Context?.Type is null or AiContextType.None
                        ? new AiToolApplicability { Available = true, Visible = true }
                        : new AiToolApplicability();
            }

            return new AiToolApplicability { Available = true };
        }

        /// <inheritdoc />
        public async ValueTask<AiInstructionsResult> GetInstructionsAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, baseScheme) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<DeputyInfoAiAgentPlugin>(toolId, toolInfo.ID);

            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var actual = await AiPromptsHelper.CacheAsync(this.cachedPrompts, settings, baseScheme, cancellationToken);
            this.cachedPrompts = actual;
            var fullName = await AiHelper.GetUserFullNameAsync(this.dbScope, context.Session.User.ID, cancellationToken);

            var prompt =
                new AiPromptBuilder()
                    .WithTemplates(actual.Templates)
                    .WithRoot(AiPromptTemplates.AiTool.RootPromptName)
                    .WithCurrentUser(fullName!)
                    .Build();

            return new(prompt, actual.Schema);
        }

        /// <inheritdoc />
        public async ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(string toolId, string? data, AiAgentContext context,
            CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<DeputyInfoAiAgentPlugin>(toolId, toolInfo.ID);
            ThrowIfNullOrWhiteSpace(data);

            var result = JsonConvert.DeserializeObject<DeputiesInfoAiResult>(data);
            if (result is null)
            {
                context.ValidationResult.AddError(AiConstants.FailedToDeserializeAiLocalization);
                return AiRecognitionResult.Error;
            }

            if (result.Deputies is not { Count: > 0 })
            {
                // Если фамилии никакой нет: Для получения информации о настроенных замещениях необходимо указать ФИО замещаемого или заместителя.
                // В этом случае пользователь может дальше что-то написать в чат, его сообщение мы снова отправляем в ИИ опять с json схемой и предыдущей перепиской.
                context.Response.Message = await LocalizeAsync("$Ai_DeputyInfoPlugin_Message_ClarifyEmployees");
                return AiRecognitionResult.Clarification;
            }

            var deputiesInfo = new DeputiesInfo { Deputies = [] };

            // Сохраняем полученные справочные данные в локальные кэши (словари),
            // чтобы не искать одни и те же записи извне по несколько раз.
            // Локальный кэш найденных сотрудников.
            // Ключ - ФИО сотрудника по шаблону: "Фамилия;Имя;Отчество".
            // Значение - список справочных данных найденных сотрудников.
            var usersLocalCache = new Dictionary<string, IList<AiEmployeeInfo>?>();

            var outMessageBuilder = StringBuilderHelper.Acquire();
            // Проверим пользователей
            var (hasErrors, isNeedToClarify) =
                await this.CheckEmployeesAsync(
                    result.Deputies!, deputiesInfo.Deputies, context.ValidationResult, usersLocalCache, outMessageBuilder,
                    "$Ai_Plugin_Message_NoDeputyLastName", cancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                outMessageBuilder.Release();
                return AiRecognitionResult.Error;
            }

            if (!hasErrors)
            {
                outMessageBuilder.Release();
                context.Response.Data = deputiesInfo;
                return AiRecognitionResult.Processing;
            }

            if (isNeedToClarify)
            {
                outMessageBuilder.AppendLine().AppendLine(await LocalizeAsync("$Ai_Plugin_Message_Clarify"));
            }

            // При наличии ошибок в запросе пользователя (или распознавании) данные не передаем.
            context.Response.Message = outMessageBuilder.ToStringAndRelease();

            // Если есть прямое указание на то, что требуется уточнение данных. Признак isNeedToClarify.
            // Либо не было найдено ни одного замещаемого и ни одного заместителя.
            return
                isNeedToClarify ||
                deputiesInfo.Deputies is { Count: 0 }
                    ? AiRecognitionResult.Clarification
                    : AiRecognitionResult.Error;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<AddDeputyAiAgentPlugin>(toolId, toolInfo.ID);

            if (context.Response.Data is not DeputiesInfo deputiesInfo)
            {
                context.ValidationResult.AddError(AiConstants.FailedToDeserializeAiLocalization);
                return;
            }

            var getKeyFunc =
                new Func<Dictionary<string, object?>, string>(value =>
                    $"{value.Get<string>("Employee")};{value.Get<string>("Deputy")};{value.Get<string>("DocumentTypes")};{value.Get<string>("Period")}");

            var viewResults = new Dictionary<string, Dictionary<string, object?>>();
            if (deputiesInfo.Deputies is { Count: > 0 })
            {
                foreach (var deputy in deputiesInfo.Deputies)
                {
                    var deputyResult = await this.SearchEmployeeInViewAsync(deputy, context.ValidationResult, RoleDeputiesViewName,
                        DeputyParamName, cancellationToken);
                    deputyResult?.ForEach(result => viewResults.TryAdd(getKeyFunc(result), result));
                    
                    var deputyByDocTypeResult = await this.SearchEmployeeInViewAsync(deputy, context.ValidationResult,
                        RoleDeputiesByDocTypesViewName, DeputyParamName, cancellationToken);
                    deputyByDocTypeResult?.ForEach(result => viewResults.TryAdd(getKeyFunc(result), result));
                    
                    var employeeResult = await this.SearchEmployeeInViewAsync(deputy, context.ValidationResult,
                        RoleDeputiesViewName, DeputizedParamName, cancellationToken);
                    employeeResult?.ForEach(result => viewResults.TryAdd(getKeyFunc(result), result));

                    var employeeByDocTypeResult =
                        await this.SearchEmployeeInViewAsync(deputy, context.ValidationResult, RoleDeputiesByDocTypesViewName,
                            DeputizedParamName, cancellationToken);
                    employeeByDocTypeResult?.ForEach(result => viewResults.TryAdd(getKeyFunc(result), result));
                }
            }

            if (viewResults is not { Count: 0 })
            {
                context.Response.Message = await LocalizeAsync("$Ai_DeputyInfoPlugin_Message_ResultTable");
                context.Response.Data = new AiTableData
                {
                    Table =
                        new AiTable(
                            [
                                new("Employee", "$Ai_DeputyInfoPlugin_ResultColumns_Employee", AiTableColumnType.String),
                                new("Deputy", "$Ai_DeputyInfoPlugin_ResultColumns_Deputy", AiTableColumnType.String),
                                new("DocumentTypes", "$Ai_DeputyInfoPlugin_ResultColumns_DocumentTypes", AiTableColumnType.String),
                                new("Period", "$Ai_DeputyInfoPlugin_ResultColumns_Period", AiTableColumnType.String)
                            ],
                            viewResults.Values.OrderBy(p => p.Get<string>("Employee")).ToList())
                }.ToSerializedDictionary();
                return;
            }

            // Если в результате поиска не нашлось ни одной строки, то в чат пишем:
            // Не найдены заместители для сотрудников (или сотрудника) ....У сотрудников(или сотрудника) нет заместителей....
            var outMessageBuilder = StringBuilderHelper.Acquire();
            switch (deputiesInfo.Deputies?.Count)
            {
                case > 1:
                    outMessageBuilder
                        .Append(await LocalizeAsync("$Ai_DeputyInfoPlugin_Message_DeputiesRowsNotFound"))
                        .AppendJoin(", ", deputiesInfo.Deputies.Select(p => p.DisplayName)).AppendLine(".  ")
                        .Append(await LocalizeAsync("$Ai_DeputyInfoPlugin_Message_EmployeesRowsNotFound"))
                        .AppendJoin(", ", deputiesInfo.Deputies.Select(p => p.DisplayName)).AppendLine(".  ");
                    break;
                case 1:
                    outMessageBuilder
                        .Append(
                            await LocalizeFormatAsync(
                                "$Ai_DeputyInfoPlugin_Message_DeputyRowsNotFound",
                                deputiesInfo.Deputies.First().DisplayName)).AppendLine("  ")
                        .Append(
                            await LocalizeFormatAsync(
                                "$Ai_DeputyInfoPlugin_Message_EmployeeRowsNotFound",
                                deputiesInfo.Deputies.First().DisplayName)).AppendLine("  ");
                    break;
            }

            context.Response.Message = outMessageBuilder.ToStringAndRelease();
        }

        #endregion

        #region Private Methods

        private async ValueTask<AiCachedToolSettings> GetBaseSettingsAsync(CancellationToken cancellationToken)
        {
            if (this.baseSettings is { } settings)
            {
                return settings;
            }

            // если будет много конкурентных обращений, нужно будет добавить AsyncLock
            var baseScheme = await JsonSchema.FromType<DeputiesInfoAiResult>().EmbedReferencesAsync(cancellationToken);

            var toolInfo = new AiToolSettings()
            {
                ID = "deputy_info",
                Name = "Поиск информации о настроенных замещениях",
                Description = "Дополнительные условия: если в запросе пользователя присутствуют слова \"поставь\", \"добавь\", \"настрой\", \"сделай\", то это не текущий инструмент поиска информации о замещениях, а инструмент настройки замещений.",
                Hint = "По фамилии сотрудника можно уточнить, кого он замещает, или кто замещает его.",

                PluginName = nameof(DeputyInfoAiAgentPlugin),
                Prompts = AiPromptsHelper.GetPrompts(AiPromptTemplates.AiTool.Templates
                    .Append(new(AiPromptTemplates.AiTool.AutoPromptName, ToolPrompt))),
                Scheme = baseScheme.ExtractDescription(),
            };

            settings = new(toolInfo, baseScheme);
            this.baseSettings = settings;
            return settings;
        }

        private async Task<(bool HasErrors, bool NeedToClarify)> CheckEmployeesAsync(
            List<AiEmployee>? employeesToCheck,
            List<AiEmployeeInfo> resultEmployees,
            IValidationResultBuilder validationResult,
            Dictionary<string, IList<AiEmployeeInfo>?> usersLocalCache,
            StringBuilder outMessageBuilder,
            string noLastNameErrorMessageConstant,
            CancellationToken cancellationToken = default)
        {
            var hasErrors = false;
            var isNeedToClarify = false;
            if (employeesToCheck is not null)
            {
                foreach (var employee in employeesToCheck)
                {
                    if (string.IsNullOrWhiteSpace(employee.LastName))
                    {
                        outMessageBuilder
                            .Append(await LocalizeAsync(noLastNameErrorMessageConstant)).AppendLine("  ");
                        hasErrors = true;
                        continue;
                    }

                    // Ищем данные по сотруднику/заместителю.
                    var employeeInfo =
                        await this.aiEmployeeSearchService.FindUserInfoOrAddErrorAsync(
                            employee,
                            validationResult,
                            outMessageBuilder,
                            usersLocalCache,
                            cancellationToken);
                    if (employeeInfo is not null)
                    {
                        resultEmployees.Add(employeeInfo);
                    }
                    else
                    {
                        hasErrors = true;
                    }

                    // isNeedToClarify мог быть установлен в true ранее.
                    isNeedToClarify = isNeedToClarify || employeeInfo is null;
                }
            }

            return (hasErrors, isNeedToClarify);
        }

        private async ValueTask<List<Dictionary<string, object?>>?> SearchEmployeeInViewAsync(
            AiEmployeeInfo employee,
            IValidationResultBuilder validationResult,
            string viewName,
            string paramName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(employee);

            var view = await this.viewService.GetByNameAsync(viewName, cancellationToken);
            if (view is null)
            {
                validationResult.AddError(this, "$UI_Cards_Exception_ViewNotFound", viewName);
                return null;
            }

            var request = new TessaViewRequest(viewName);
            request.Parameters.Add(
                new RequestParameter(IsActiveParamName)
                    .Add(IsTrueCriteriaOperator.Instance));
            request.Parameters.Add(
                new RequestParameter(AvailableOnDateParamName)
                    .Add(EqualsToCriteriaOperator.Instance, DateTime.UtcNow));
            request.Parameters.Add(
                new RequestParameter(paramName)
                    .Add(EqualsToCriteriaOperator.Instance, employee.ID));

            var viewResult = await view.GetDataAsync(request, cancellationToken);

            if (viewResult.Rows is not { Count: > 0 })
            {
                return null;
            }

            var result = new List<Dictionary<string, object?>>();
            foreach (var row in viewResult.Rows)
            {
                var rowStorage = viewResult.CreateRowStorage(row);
                var documentTypes = rowStorage.TryGet<string?>("RoleDeputyTypes");
                var deputyFrom = rowStorage.TryGet<DateTime?>("RoleDeputyFrom");
                var deputyTo = rowStorage.TryGet<DateTime?>("RoleDeputyTo");

                result.Add(
                    new Dictionary<string, object?>
                    {
                        { "Employee", rowStorage.TryGet<string?>("RoleDeputizedName") },
                        { "Deputy", rowStorage.TryGet<string?>("RoleDeputyName") },
                        {
                            "DocumentTypes",
                            string.IsNullOrWhiteSpace(documentTypes)
                                ? "$Ai_DeputyInfoPlugin_ResultConstants_AllDocumentTypes"
                                : documentTypes
                        },
                        {
                            "Period",
                            deputyFrom is null && deputyTo is null
                                ? "$Ai_Message_PermanentCaption"
                                : FormatDate(deputyFrom) + " - " + FormatDate(deputyTo)
                        }
                    });
            }

            return result;
        }

        #endregion
    }
}
