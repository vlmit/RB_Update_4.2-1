#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Предоставляет вспомогательные методы для формирования сообщений об ошибках выполнения маршрутов документов.
    /// </summary>
    public static class KrErrorHelper
    {
        #region Public Methods

        /// <summary>
        /// Проверят, что тип указанной карточки равен <see cref="DefaultCardTypes.KrSatelliteTypeID"/>, если это не так, то создаёт исключение <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertKrSatellite(Card card)
        {
            ThrowIfNull(card);

            if (card.TypeID != DefaultCardTypes.KrSatelliteTypeID)
            {
                throw new InvalidOperationException(
                    $"{nameof(Card)}.{nameof(card.TypeID)} != {nameof(DefaultCardTypes)}.{nameof(DefaultCardTypes.KrSatelliteTypeID)}");
            }
        }

        public static void WarnStageTypeIsNull(
            IValidationResultBuilder validationResult,
            Stage stage)
        {
            ThrowIfNull(validationResult);
            ThrowIfNull(stage);

            validationResult
                .BeginSequence()
                .SetObjectName(nameof(IKrProcessRunner))
                .WarningText(
                    "$KrProcessRunner_StageTypeIsNull",
                    stage.Name,
                    stage.RowID)
                .End();
        }

        public static void WarnStageHandlerIsNull(
            IValidationResultBuilder validationResult,
            Stage stage)
        {
            ThrowIfNull(validationResult);
            ThrowIfNull(stage);

            validationResult
                .BeginSequence()
                .SetObjectName(nameof(IKrProcessRunner))
                .WarningText(
                    "$KrProcessRunner_StageHandlerIsNull",
                    stage.Name,
                    stage.RowID,
                    stage.StageTypeID,
                    stage.StageTypeCaption)
                .End();
        }

        [DoesNotReturn]
        public static void PerformerNotSpecified(string stageName) =>
            throw new ProcessRunnerInterruptedException(LocalizeFormat("$UI_Error_PerformerNotSpecified", stageName));

        [DoesNotReturn]
        public static void TimeLimitNotSpecified(string stageName) =>
            throw new ProcessRunnerInterruptedException(LocalizeFormat("$UI_Error_TimeLimitNotSpecified", stageName));

        [DoesNotReturn]
        public static void PlannedNotSpecified(string stageName) =>
            throw new ProcessRunnerInterruptedException(LocalizeFormat("$UI_Error_PlannedNotSpecified", stageName));

        [DoesNotReturn]
        public static void TimeLimitOrPlannedNotSpecified(string stageName) =>
            throw new ProcessRunnerInterruptedException(LocalizeFormat("$UI_Error_TimeLimitOrPlannedNotSpecified", stageName));

        public static string GetTraceTextFromExecutionUnit(
            IKrExecutionUnit unit,
            string? scriptType = null)
        {
            ThrowIfNull(unit);

            var stageName = unit.Instance.Stage?.Name;
            var templateName = unit.Instance.TemplateName;
            var groupName = unit.Instance.StageGroupName;
            var buttonName = unit.Instance.Button?.Name;

            return FormatErrorMessageTrace(scriptType, stageName, templateName, groupName, buttonName);
        }

        public static string GetTraceTextFromStage(
            Stage stage,
            string? scriptType = null)
        {
            ThrowIfNull(stage);

            var stageName = stage.Name;
            var templateName = stage.TemplateName;
            var groupName = stage.StageGroupName;

            return FormatErrorMessageTrace(scriptType, stageName, templateName, groupName, null);
        }

        public static string UnexpectedError(IKrExecutionUnit unit) =>
            // Параметр stage будут проверен в GetTraceTextFromStage
            LocalizeFormat(
                "$KrProcess_ErrorMessage_ErrorFormat",
                GetTraceTextFromExecutionUnit(unit),
                "$KrProcess_ErrorMessage_UnexpectedException",
                string.Empty);

        public static string UnexpectedError(Stage stage) =>
            // Параметр stage будут проверен в GetTraceTextFromStage
            LocalizeFormat(
                "$KrProcess_ErrorMessage_ErrorFormat",
                GetTraceTextFromStage(stage),
                "$KrProcess_ErrorMessage_UnexpectedException",
                string.Empty);

        public static string DesignTimeError(IKrExecutionUnit unit, string errorText, params ReadOnlySpan<object?> args) =>
            ScriptErrorInternal(NotNullOrThrow(unit), "Design", errorText, args);

        public static string SqlDesignTimeError(IKrExecutionUnit unit, string errorText, params ReadOnlySpan<object?> args) =>
            QueryErrorInternal(NotNullOrThrow(unit), "Design", errorText, args);

        public static string RuntimeError(IKrExecutionUnit unit, string errorText, params ReadOnlySpan<object?> args) =>
            ScriptErrorInternal(NotNullOrThrow(unit), "Runtime", errorText, args);

        public static string SqlRuntimeError(IKrExecutionUnit unit, string errorText, params ReadOnlySpan<object?> args) =>
            QueryErrorInternal(NotNullOrThrow(unit), "Runtime", errorText, args);

        /// <summary>
        /// Формирует сообщение, содержащее информацию об ошибке, возникшей при выполнении условия видимости кнопки вторичного процесса.
        /// </summary>
        /// <param name="secondaryProcessName">Название вторичного процесса.</param>
        /// <param name="errorText">Сообщение об ошибке.</param>
        /// <param name="args">Дополнительные значения подставляемые в <paramref name="errorText"/>.</param>
        /// <returns>Сообщение, содержащее информацию об ошибке, возникшей при выполнении условия видимости кнопки вторичного процесса.</returns>
        public static string ButtonVisibilityError(
            string secondaryProcessName,
            string errorText,
            params ReadOnlySpan<object?> args) =>
            SecondaryProcessErrorInternal(
                secondaryProcessName,
                "Visibility",
                "$KrProcess_ErrorMessage_FullScriptInDetails",
                errorText,
                args);

        /// <summary>
        /// Формирует сообщение, содержащее информацию об ошибке, возникшей при выполнении SQL условия видимости кнопки вторичного процесса.
        /// </summary>
        /// <param name="secondaryProcessName">Название вторичного процесса.</param>
        /// <param name="errorText">Сообщение об ошибке.</param>
        /// <param name="args">Дополнительные значения подставляемые в <paramref name="errorText"/>.</param>
        /// <returns>Сообщение, содержащее информацию об ошибке, возникшей при выполнении SQL условия видимости кнопки вторичного процесса.</returns>
        public static string ButtonSqlVisibilityError(
            string secondaryProcessName,
            string errorText,
            params ReadOnlySpan<object?> args) =>
            SecondaryProcessErrorInternal(
                secondaryProcessName,
                "VisibilitySql",
                "$KrProcess_ErrorMessage_FullQueryInDetails",
                errorText,
                args);

        /// <summary>
        /// Формирует сообщение, содержащее информацию об ошибке, возникшей при выполнении условия выполнения вторичного процесса.
        /// </summary>
        /// <param name="secondaryProcessName">Название вторичного процесса.</param>
        /// <param name="errorText">Сообщение об ошибке.</param>
        /// <param name="args">Дополнительные значения подставляемые в <paramref name="errorText"/>.</param>
        /// <returns>Сообщение, содержащее информацию об ошибке, возникшей при выполнении условия выполнения вторичного процесса.</returns>
        public static string SecondaryProcessExecutionError(
            string secondaryProcessName,
            string errorText,
            params ReadOnlySpan<object?> args) =>
            SecondaryProcessErrorInternal(
                secondaryProcessName,
                "Execution",
                "$KrProcess_ErrorMessage_FullScriptInDetails",
                errorText,
                args);

        /// <summary>
        /// Формирует сообщение, содержащее информацию об ошибке, возникшей при выполнении SQL условия выполнения вторичного процесса.
        /// </summary>
        /// <param name="secondaryProcessName">Название вторичного процесса.</param>
        /// <param name="errorText">Сообщение об ошибке.</param>
        /// <param name="args">Дополнительные значения подставляемые в <paramref name="errorText"/>.</param>
        /// <returns>Сообщение, содержащее информацию об ошибке, возникшей при выполнении SQL условия выполнения вторичного процесса.</returns>
        public static string SecondaryProcessSqlExecutionError(
            string secondaryProcessName,
            string errorText,
            params ReadOnlySpan<object?> args) =>
            SecondaryProcessErrorInternal(
                secondaryProcessName,
                "ExecutionSql",
                "$KrProcess_ErrorMessage_FullQueryInDetails",
                errorText,
                args);

        /// <summary>
        /// Формирует сообщение, содержащее информацию об ошибке, возникшей при получении SQL-исполнителей.
        /// </summary>
        /// <param name="stageName">Название этапа. Может быть не задано.</param>
        /// <param name="stageTemplateName">Название шаблона этапов. Может быть не задано.</param>
        /// <param name="stageGroupName">Название группы этапов. Может быть не задано.</param>
        /// <param name="secondaryProcessName">Название вторичного процесса. Может быть не задано.</param>
        /// <param name="errorText">Сообщение об ошибке.</param>
        /// <param name="args">Дополнительные значения подставляемые в <paramref name="errorText"/>.</param>
        /// <returns>Сообщение, содержащее информацию об ошибке, возникшей при получении SQL-исполнителей.</returns>
        public static string SqlPerformersError(
            string? stageName,
            string? stageTemplateName,
            string? stageGroupName,
            string? secondaryProcessName,
            string? errorText,
            params ReadOnlySpan<object?> args)
        {
            var text = LocalizeFormat(errorText, args);
            return LocalizeFormat(
                "$KrProcess_ErrorMessage_ErrorFormat",
                FormatErrorMessageTrace(
                    "$KrProcess_ErrorMessage_SqlPerformersTrace",
                    stageName,
                    stageTemplateName,
                    stageGroupName,
                    secondaryProcessName),
                text,
                "$KrProcess_ErrorMessage_FullQueryInDetails");
        }

        /// <summary>
        /// Формирует сообщение, содержащее информацию о месте возникновения ошибки.
        /// </summary>
        /// <param name="name">Название места возникновения ошибки. Может быть не задано.</param>
        /// <param name="stageName">Название этапа. Может быть не задано.</param>
        /// <param name="stageTemplateName">Название шаблона этапов. Может быть не задано.</param>
        /// <param name="stageGroupName">Название группы этапов. Может быть не задано.</param>
        /// <param name="secondaryProcessName">Название вторичного процесса. Может быть не задано.</param>
        /// <returns>Сообщение, содержащее информацию о месте возникновения ошибки.</returns>
        public static string FormatErrorMessageTrace(
            string? name,
            string? stageName = null,
            string? stageTemplateName = null,
            string? stageGroupName = null,
            string? secondaryProcessName = null) =>
            FormatErrorMessageTrace(name, FormatErrorMessageTrace(stageName, stageTemplateName, stageGroupName, secondaryProcessName));

        /// <summary>
        /// Формирует сообщение, содержащее информацию о месте возникновения ошибки.
        /// </summary>
        /// <param name="name">Название места возникновения ошибки. Может быть не задано.</param>
        /// <param name="location">Строка содержащая информацию о месте возникновения ошибки</param>
        /// <returns>Сообщение, содержащее информацию о месте возникновения ошибки.</returns>
        public static string FormatErrorMessageTrace(string? name, string location)
        {
            var lName = Localize(name);
            return string.IsNullOrWhiteSpace(lName) ? location : $"{lName}: {location}";
        }

        /// <summary>
        /// Формирует сообщение, содержащее информацию о месте возникновения ошибки.
        /// </summary>
        /// <param name="stageName">Название этапа. Может быть не задано.</param>
        /// <param name="stageTemplateName">Название шаблона этапов. Может быть не задано.</param>
        /// <param name="stageGroupName">Название группы этапов. Может быть не задано.</param>
        /// <param name="secondaryProcessName">Название вторичного процесса. Может быть не задано.</param>
        /// <returns>Сообщение, содержащее информацию о месте возникновения ошибки.</returns>
        public static string FormatErrorMessageTrace(
            string? stageName = null,
            string? stageTemplateName = null,
            string? stageGroupName = null,
            string? secondaryProcessName = null)
        {
            var sb = StringBuilderHelper.Acquire(256);

            if (!string.IsNullOrWhiteSpace(stageName))
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(LocalizeFormat("$KrProcess_ErrorMessage_StageTrace", stageName));
            }

            if (!string.IsNullOrWhiteSpace(stageTemplateName))
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(LocalizeFormat("$KrProcess_ErrorMessage_TemplateTrace", stageTemplateName));
            }

            if (!string.IsNullOrWhiteSpace(stageGroupName))
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(LocalizeFormat("$KrProcess_ErrorMessage_GroupTrace", stageGroupName));
            }

            if (!string.IsNullOrWhiteSpace(secondaryProcessName))
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(LocalizeFormat("$KrProcess_ErrorMessage_SecondaryProcessTrace", secondaryProcessName));
            }

            return sb.ToStringAndRelease();
        }

        /// <summary>
        /// Форматирование сообщения о том, что в маршруте нет активных этапов с дополнительным выводом кнопки.
        /// </summary>
        /// <param name="secondaryProcess"></param>
        /// <returns></returns>
        public static string FormatEmptyRoute(
            IKrSecondaryProcess? secondaryProcess)
        {
            var secondPart = secondaryProcess is not null
                ? LocalizeFormat("$KrStages_RouteIsEmptySecondaryProcessDescription", secondaryProcess.Name, secondaryProcess.ID)
                : "$KrProcess_MainRouteHasNoActiveStages";
            return LocalizeFormat("$KrProcess_RouteHasNoActiveStages", secondPart);
        }

        public static string ProcessStartingForDifferentCardID() =>
            LocalizeName("KrSecondaryProcess_ProcessStartingForDifferentCardID");

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ScriptErrorInternal(
            IKrExecutionUnit unit,
            string executionType,
            string errorText,
            ReadOnlySpan<object?> args) =>
            ErrorInternal(unit, executionType, "$KrProcess_ErrorMessage_FullScriptInDetails", errorText, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string QueryErrorInternal(
            IKrExecutionUnit unit,
            string executionType,
            string errorText,
            ReadOnlySpan<object?> args) =>
            ErrorInternal(unit, executionType, "$KrProcess_ErrorMessage_FullQueryInDetails", errorText, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ErrorInternal(
            IKrExecutionUnit unit,
            string executionType,
            string whereIsCode,
            string errorText,
            ReadOnlySpan<object?> args)
        {
            var et = LocalizeFormat(errorText, args);
            var scriptType = LocalizeScriptKrStringType(unit.Instance.KrScriptType, executionType);
            var trace = GetTraceTextFromExecutionUnit(unit, scriptType);
            return LocalizeFormat("$KrProcess_ErrorMessage_ErrorFormat", trace, et, whereIsCode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string SecondaryProcessErrorInternal(
            string secondaryProcessName,
            string executionType,
            string whereIsCode,
            string? errorText,
            ReadOnlySpan<object?> args)
        {
            var et = LocalizeFormat(errorText, args);
            var scriptType = LocalizeName($"KrProcess_ErrorMessage_{executionType}Trace");
            var trace = FormatErrorMessageTrace(scriptType, null, null, null, secondaryProcessName);
            return LocalizeFormat("$KrProcess_ErrorMessage_ErrorFormat", trace, et, whereIsCode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string LocalizeScriptKrStringType(KrScriptType type, string? pref = null) =>
            type switch
            {
                KrScriptType.Before => LocalizeName($"KrProcess_ErrorMessage_{pref}BeforeTrace"),
                KrScriptType.Condition => LocalizeName($"KrProcess_ErrorMessage_{pref}ConditionTrace"),
                KrScriptType.After => LocalizeName($"KrProcess_ErrorMessage_{pref}AfterTrace"),
                _ => throw ArgumentOutOfRange(type)
            };

        #endregion
    }
}
