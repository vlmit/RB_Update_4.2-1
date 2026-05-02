#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow
{
    /// <inheritdoc cref="IKrHistoryStrategy"/>
    /// <param name="calendarService"><inheritdoc cref="IBusinessCalendarService" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    public sealed class KrHistoryStrategy(
        IBusinessCalendarService calendarService,
        ICardMetadata cardMetadata,
        ISession session)
        : IKrHistoryStrategy
    {
        #region Fields

        private readonly IBusinessCalendarService calendarService = NotNullOrThrow(calendarService);
        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);
        private readonly ISession session = NotNullOrThrow(session);

        #endregion

        #region IKrWorkflowHistoryStrategy Members

        /// <inheritdoc />
        public async ValueTask<CardTaskHistoryItem?> CreateTaskHistoryAsync(
            Guid taskTypeID,
            Guid optionID,
            string? result,
            IValidationResultBuilder validationResult,
            Guid? groupRowID = null,
            DateTime? storeDateTime = null,
            CancellationToken cancellationToken = default)
        {
            var cardTypes = await this.cardMetadata.GetCardTypesAsync(cancellationToken);

            if (!cardTypes.TryGetValue(taskTypeID, out var taskType))
            {
                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(this)
                    .Error(CardValidationKeys.UnknownCardType, taskTypeID)
                    .End();

                await CardComponentHelper.AddDamagedCardTypeValidationResultAsync(
                    taskTypeID,
                    null,
                    this.cardMetadata,
                    validationResult,
                    cancellationToken);

                return null;
            }

            return await this.CreateTaskHistoryAsync(
                taskTypeID,
                taskType.Name,
                taskType.Caption,
                optionID,
                result,
                validationResult,
                groupRowID,
                storeDateTime,
                cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask<CardTaskHistoryItem?> CreateTaskHistoryAsync(
            Guid taskTypeID,
            string? taskTypeName,
            string? taskTypeCaption,
            Guid optionID,
            string? result,
            IValidationResultBuilder validationResult,
            Guid? groupRowID = null,
            DateTime? storeDateTime = null,
            CancellationToken cancellationToken = default)
        {
            var enumerations = await this.cardMetadata.GetEnumerationsAsync(cancellationToken);

            if (!enumerations.CompletionOptions.TryGetValue(optionID, out var option))
            {
                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(this)
                    .Error(CardValidationKeys.UnknownTaskOption, Guid.Empty, optionID)
                    .End();

                return null;
            }

            // Временная зона текущего сотрудника и календарь, для записи в историю заданий
            var userID = this.session.User.ID;
            var userName = this.session.User.Name;

            var userCalendarInfo = await this.calendarService.GetRoleCalendarInfoAsync(
                userID,
                cancellationToken);

            if (userCalendarInfo is null)
            {
                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(this)
                    .ErrorText("$KrMessages_NoRoleCalendar", userID)
                    .End();

                return null;
            }

            var userZoneInfo = await this.calendarService.GetRoleTimeZoneInfoAsync(
                userID,
                cancellationToken);

            var settings = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [KrConstants.TaskHistorySettingsKeys.PerformerID] = userID,
                [KrConstants.TaskHistorySettingsKeys.PerformerName] = userName
            };

            storeDateTime ??= DateTime.UtcNow;

            var newItem = new CardTaskHistoryItem
            {
                State = CardTaskHistoryState.Inserted,
                RowID = Guid.NewGuid(),
                TypeID = taskTypeID,
                TypeName = taskTypeName,
                TypeCaption = taskTypeCaption,
                Created = storeDateTime.Value,
                Planned = storeDateTime.Value,
                InProgress = storeDateTime.Value,
                Completed = storeDateTime.Value,
                UserID = userID,
                UserName = userName,
                AuthorID = userID,
                AuthorName = userName,
                Result = result,
                OptionID = optionID,
                OptionCaption = option.Caption,
                OptionName = option.Name,
                ParentRowID = null,
                CompletedByID = userID,
                CompletedByName = userName,
                CompletedByRole = userName,
                GroupRowID = groupRowID,
                TimeZoneID = userZoneInfo.TimeZoneID,
                TimeZoneUtcOffsetMinutes = (int?) userZoneInfo.TimeZoneUtcOffset.TotalMinutes,
                CalendarID = userCalendarInfo.CalendarID,
                Settings = settings,
                AssignedOnRole = userName,
            };

            return newItem;
        }

        #endregion
    }
}
