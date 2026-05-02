#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.VirtualFiles;
using Tessa.Notices;
using Tessa.Notices.Extensions;
using Tessa.Platform;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Notices.MobileApproval
{
    /// <summary>
    /// Базовый абстрактный класс расширения для процесса отправки уведомления мобильного согласования.
    /// </summary>
    public abstract class MobileApprovalNotificationSendExtensionBase :
        NotificationSendExtensionBase
    {
        #region Constants

        /// <summary>
        /// Начало блока <b>MA</b>.
        /// </summary>
        protected const string StartMailAttachmentsBlock = "<!--.MA-->";

        /// <summary>
        /// Конец блока <b>MA</b>.
        /// </summary>
        protected const string EndMailAttachmentsBlock = "<!--.MAE-->";

        /// <summary>
        /// Метка, отмечающая начало блока, в котором текст должен быть преобразован в соответствии с правилами кодирования URL.
        /// </summary>
        /// <remarks>
        /// Пример: <see cref="StartUrlEncodeMark"/>&lt;data&gt;<see cref="EndUrlEncodeMark"/>
        /// </remarks>
        protected const string StartUrlEncodeMark = $"{StorageHelper.SystemKeyPrefix}StartUrlEncode";

        /// <summary>
        /// Метка, отмечающая конец блока, в котором текст должен быть преобразован в соответствии с правилами кодирования URL.
        /// </summary>
        protected const string EndUrlEncodeMark = $"{StorageHelper.SystemKeyPrefix}EndUrlEncode";

        /// <summary>
        /// Название группы в <see cref="urlEncodeRegexp"/>, которая содержит обрабатываемую строку.
        /// </summary>
        private const string UrlEncodeRegexpDataGroupName = "data";

        /// <summary>
        /// Регулярное выражение для обработки блока <see cref="StartUrlEncodeMark"/> - <see cref="EndUrlEncodeMark"/>.
        /// </summary>
        private static readonly Regex urlEncodeRegexp = new(
            $"{Regex.Escape(StartUrlEncodeMark)}(?<{UrlEncodeRegexpDataGroupName}>.*?){Regex.Escape(EndUrlEncodeMark)}",
            RegexOptions.Compiled
            | RegexOptions.CultureInvariant
            | RegexOptions.Singleline);

        /// <summary>
        /// Метка, отмечающая начало блока, в котором текст должен быть усечён до заданного значения.
        /// </summary>
        /// <remarks>
        /// Пример: <see cref="StartLimitMark"/>:&lt;maxLength&gt;:&lt;data&gt;<see cref="EndLimitMark"/>
        /// </remarks>
        protected const string StartLimitMark = $"{StorageHelper.SystemKeyPrefix}StartLimit";

        /// <summary>
        /// Метка, отмечающая конец блока, в котором текст должен быть усечён до заданного значения.
        /// </summary>
        protected const string EndLimitMark = $"{StorageHelper.SystemKeyPrefix}EndLimitMark";

        /// <summary>
        /// Название группы в <see cref="limitDataRegexp"/>, которая содержит максимальную длину строки.
        /// </summary>
        private const string LimitDataRegexpMaxLengthGroupName = "maxLength";

        /// <summary>
        /// Название группы в <see cref="limitDataRegexp"/>, которая содержит обрабатываемую строку.
        /// </summary>
        private const string LimitDataRegexpDataGroupName = "data";

        /// <summary>
        /// Регулярное выражение для обработки блока <see cref="StartLimitMark"/> - <see cref="EndLimitMark"/>.
        /// </summary>
        private static readonly Regex limitDataRegexp = new(
            @$"{Regex.Escape(StartLimitMark)}:(?<{LimitDataRegexpMaxLengthGroupName}>\d+?):(?<{LimitDataRegexpDataGroupName}>.*?){Regex.Escape(EndLimitMark)}",
            RegexOptions.Compiled
            | RegexOptions.CultureInvariant
            | RegexOptions.Singleline);

        /// <summary>
        /// Плейсхолдер по умолчанию, заменяемый блоком, содержащим обратные ссылки мобильного согласования.
        /// </summary>
        protected const string OptionsForMobileApprovalPlaceholder = "{optionsForMobileApproval}";

        /// <summary>
        /// Плейсхолдер по умолчанию, заменяемый блоком, содержащим файлы приложенные к письму мобильного согласования.
        /// </summary>
        protected const string FilesForMobileApprovalPlaceholder = "{filesForMobileApproval}";

        /// <summary>
        /// Блок, содержащий файлы приложенные к письму мобильного согласования.
        /// </summary>
        protected const string FilesForMobileApprovalBlock =
            $@"{StartMailAttachmentsBlock}
            <br/>
            <!-- .MISSED_FILES -->
            <!-- .OVERSIZED_FILES -->
            {EndMailAttachmentsBlock}";

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeFormingNotificationForUser(
            INotificationSendExtensionContext context)
        {
            if (context.Info.TryGet<CardTask>(PlaceholderHelper.TaskKey) is not { } task)
            {
                return;
            }

            await this.ReplaceOptionsForMobileApprovalAsync(
                context,
                task);

            await this.ReplaceFilesForMobileApprovalAsync(
                context,
                task);

            List<MailFile>? mailFiles = null;

            context.NotificationEmail.GetMailInfoFuncAsync = (user, card, _) =>
            {
                if (!user.HasMobileApproval)
                {
                    return new((MailInfo?) null);
                }

                if (mailFiles is null
                    && card.TryGetFiles() is { Count: > 0 } files)
                {
                    mailFiles = new List<MailFile>(files.Count);

                    foreach (var file in files)
                    {
                        if (file.RowID == KrVirtualFilesHelper.ApprovalListFileID)
                        {
                            mailFiles.Insert(0, file.ToMailFile());
                        }
                        else
                        {
                            mailFiles.Add(file.ToMailFile());
                        }
                    }
                }

                var mailInfo = new MailInfo
                {
                    CardID = card.ID,
                    CardTypeID = card.TypeID,
                    CardTypeName = card.TypeName,
                    LanguageCode = user.LanguageCode,
                    FormatName = user.FormatName,
                    UserID = user.UserID,
                    UserName = user.UserName,
                    TimeZoneUtcOffsetMinutes = user.TimeZoneUtcOffsetMinutes,
                    CalendarID = user.CalendarID
                };

                if (mailFiles is not null)
                {
                    mailInfo.Files.AddItemsByRef(mailFiles);
                }

                return new(mailInfo);
            };
        }

        /// <inheritdoc/>
        public override Task AfterFormingNotificationForUser(
            INotificationSendExtensionContext context)
        {
            if (!context.CurrentRecipient.HasMobileApproval
                || string.IsNullOrEmpty(context.Body))
            {
                return Task.CompletedTask;
            }

            // Преобразованная строка может быть длиннее исходной, но если обрезать преобразованную строку, то могут появиться артефакты.
            context.Body = LimitDataAllBlocksProcessing(context.Body);
            context.Body = UrlEncodeAllBlocksProcessing(context.Body);

            return Task.CompletedTask;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Заменяет плейсхолдер на варианты завершения письма мобильного согласования.
        /// </summary>
        /// <param name="context"><inheritdoc cref="INotificationSendExtensionContext" path="/summary"/></param>
        /// <param name="task">Задание, для которого формируется письмо мобильного согласования.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual ValueTask ReplaceOptionsForMobileApprovalAsync(
            INotificationSendExtensionContext context,
            CardTask task) => ValueTask.CompletedTask;

        /// <summary>
        /// Заменяет плейсхолдер блоком, содержащим приложенные к письму мобильного согласования файлы.
        /// </summary>
        /// <param name="context"><inheritdoc cref="INotificationSendExtensionContext" path="/summary"/></param>
        /// <param name="task">Задание, для которого формируется письмо мобильного согласования.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual ValueTask ReplaceFilesForMobileApprovalAsync(
            INotificationSendExtensionContext context,
            CardTask task)
        {
            context.NotificationEmail.BodyTemplate = context.NotificationEmail.BodyTemplate.Replace(
                FilesForMobileApprovalPlaceholder,
                FilesForMobileApprovalBlock,
                StringComparison.Ordinal);

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Создаёт ссылку (HTML тэг <c>a</c>).
        /// </summary>
        /// <param name="sb"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="createLinkAction">Метод, выполняющий построение адреса ссылки.</param>
        /// <param name="caption">Текст ссылки.</param>
        /// <param name="maxLinkLength">Максимальная длина адреса.</param>
        /// <remarks>В созданный адрес добавляются плейсхолдеры <see cref="StartLimitMark"/> и <see cref="EndLimitMark"/>, которые отмечают участки текста, требующие усечения до <paramref name="maxLinkLength"/>. Для усечения можно воспользоваться методом <see cref="LimitDataAllBlocksProcessing(string)"/>.</remarks>
        protected static void CreateLink(
            StringBuilder sb,
            Action<StringBuilder> createLinkAction,
            string caption,
            int maxLinkLength = 940)
        {
            ThrowIfNull(sb);
            ThrowIfNull(createLinkAction);

            sb
                .Append("<a href=\"")
                .Append($"{StartLimitMark}:{maxLinkLength}:");

            createLinkAction(sb);

            sb
                .Append(EndLimitMark)
                .Append("\">")
                .Append(caption)
                .Append("</a>");
        }

        /// <summary>
        /// Создаёт адрес со схемой mailto.
        /// </summary>
        /// <param name="sb"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="email">Адрес, на который должно быть отправлено созданное письмо.</param>
        /// <param name="subject">Тема создаваемого письма после перехода по ссылке.</param>
        /// <param name="body">Тело создаваемого письма после перехода по ссылке.</param>
        /// <remarks>В созданный адрес добавляются плейсхолдеры <see cref="StartUrlEncodeMark"/> и <see cref="EndUrlEncodeMark"/>, которые отмечают участки текста, требующие преобразования в соответствии с правилами кодирования URL. Для преобразования можно воспользоваться методом <see cref="UrlEncodeAllBlocksProcessing(string)"/>.</remarks>
        protected static void CreateMailtoLink(
            StringBuilder sb,
            string? email,
            string? subject,
            string? body)
        {
            ThrowIfNull(sb);

            sb
                .Append("mailto:")
                .Append(email);

            var hasFirstComponent = false;

            if (!string.IsNullOrEmpty(subject))
            {
                hasFirstComponent = true;

                sb
                    .Append("?subject=")
                    .Append(StartUrlEncodeMark)
                    .Append(subject)
                    .Append(EndUrlEncodeMark);
            }

            if (!string.IsNullOrEmpty(body))
            {
                AppendComponent(sb, hasFirstComponent);

                sb
                    .Append("body=")
                    .Append(StartUrlEncodeMark)
                    .Append(body)
                    .Append(EndUrlEncodeMark);
            }

            return;

            static void AppendComponent(StringBuilder sb, bool hasFirstComponent) =>
                sb.Append(hasFirstComponent ? '&' : '?');
        }

        /// <summary>
        /// Создаёт ссылку мобильного согласования с текстом "Завершить".
        /// </summary>
        /// <param name="taskID">Идентификатор задания.</param>
        /// <param name="mobileApprovalEmail">Адрес почты мобильного согласования.</param>
        /// <param name="caption">Текст ссылки.</param>
        /// <returns>Созданная ссылка.</returns>
        protected static string CreateMobileApprovalLink(
            Guid taskID,
            string? mobileApprovalEmail,
            string caption)
        {
            const string subject =
                "{text:$KrMessages_CompleteTaskResultMessage:#noencode}: {f:DocumentCommonInfo.FullNumber:#noencode}{f:DocumentCommonInfo.Subject format as (, [0]):#noencode}";

            var sb = StringBuilderHelper.Acquire(128);
            CreateLink(
                sb,
                sb2 =>
                    CreateMailtoLink(
                        sb2,
                        mobileApprovalEmail,
                        $"[tsk-1-{taskID:N}] {subject}",
                        "\n\n\n<{text:$KrMessages_CommentTextMessage:#noencode}>"),
                caption);

            return sb.ToStringAndRelease();
        }

        /// <summary>
        /// Создаёт ссылку мобильного согласования.
        /// </summary>
        /// <param name="isPositive"><see langword="true"/>, если необходимо создать ссылку для позитивного варианта завершения, иначе - <see langword="false"/>.</param>
        /// <param name="taskID">Идентификатор задания.</param>
        /// <param name="mobileApprovalEmail">Адрес почты мобильного согласования.</param>
        /// <param name="locStrSubject">Строка локализации, используемая в заголовке ответного письма.</param>
        /// <param name="caption">Текст ссылки.</param>
        /// <returns>Созданная ссылка.</returns>
        protected static string CreateMobileApprovalLink(
            bool isPositive,
            Guid taskID,
            string? mobileApprovalEmail,
            string locStrSubject,
            string caption)
        {
            var sb = StringBuilderHelper.Acquire(128);

            var positiveMark = isPositive ? "1" : "0";
            var subject = $"{{text:{locStrSubject}:#noencode}}: {{f:DocumentCommonInfo.FullNumber:#noencode}}{{f:DocumentCommonInfo.Subject format as (, [0]):#noencode}}";

            CreateLink(
                sb,
                sb2 =>
                    CreateMailtoLink(
                        sb2,
                        mobileApprovalEmail,
                        $"[apr-{positiveMark}-{taskID:N}] {subject}",
                        "\n\n\n<{text:$KrMessages_CommentTextMessage:#noencode}>"),
                caption);

            return sb.ToStringAndRelease();
        }

        /// <summary>
        /// Преобразует все участки текста, отмеченные плейсхолдерами <see cref="StartUrlEncodeMark"/> и <see cref="EndUrlEncodeMark"/> в соответствии с правилами кодирования URL.
        /// </summary>
        /// <param name="str">Преобразуемая строка.</param>
        /// <returns>Преобразованная строка.</returns>
        protected static string UrlEncodeAllBlocksProcessing(string str) =>
            urlEncodeRegexp.Replace(
                str,
                static match => TextHelper.UrlEncode(
                    match.Groups[UrlEncodeRegexpDataGroupName].Value));

        /// <summary>
        /// Усекает все участки текста, отмеченные плейсхолдерами <see cref="StartLimitMark"/> и <see cref="EndLimitMark"/> до заданной максимальной длины (см. описание плейсхолдера <see cref="StartLimitMark"/>).
        /// </summary>
        /// <param name="str">Преобразуемая строка.</param>
        /// <returns>Преобразованная строка.</returns>
        protected static string LimitDataAllBlocksProcessing(string str) =>
            limitDataRegexp.Replace(
                str,
                static match =>
                {
                    var maxLength = int.Parse(match.Groups[LimitDataRegexpMaxLengthGroupName].Value);
                    var data = match.Groups[LimitDataRegexpDataGroupName].Value;

                    // Нельзя использовать string.Limit, т.к. он добавляет многоточие в конце.
                    return string.IsNullOrEmpty(data) || data.Length <= maxLength
                        ? data
                        : data[..maxLength];
                });

        #endregion
    }
}
