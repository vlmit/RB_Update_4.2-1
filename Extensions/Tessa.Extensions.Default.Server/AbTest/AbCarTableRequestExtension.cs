#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.AbTest
{
    /// <summary>
    /// Создание тестовой информации при нажатии на кнопку тулбара "Получить таблицу" в карточке автомобиля.
    /// </summary>
    public sealed class AbCarTableRequestExtension :
        CardRequestExtension
    {
        #region Consts

        /// <summary>
        /// Ключ в <see cref="CardInfoStorageObject.Info"/> для сохранения результата запроса.
        /// </summary>
        private const string CarTableRequestResultKey = "CarTableRequestResult";

        /// <summary>
        /// Ключ в <see cref="CarTableRequestResultKey"/> для сохранения идентификатора.
        /// </summary>
        private const string GuidKey = "Guid";

        /// <summary>
        /// Ключ в <see cref="CarTableRequestResultKey"/> для сохранения строки.
        /// </summary>
        private const string StringKey = "String";

        /// <summary>
        /// Ключ в <see cref="CarTableRequestResultKey"/> для сохранения даты и времени.
        /// </summary>
        private const string DateTimeKey = "DateTime";

        /// <summary>
        /// Ключ в <see cref="CarTableRequestResultKey"/> для сохранения числа.
        /// </summary>
        private const string NumberKey = "Number";

        /// <summary>
        /// Ключ в <see cref="CarTableRequestResultKey"/> для сохранения идентификатора ссылки.
        /// </summary>
        private const string LinkIDKey = "LinkID";

        /// <summary>
        /// Ключ в <see cref="CarTableRequestResultKey"/> для сохранения текстового представления ссылки.
        /// </summary>
        private const string LinkNameKey = "LinkName";

        #endregion

        #region Private Fields

        /// <inheritdoc cref="ISlugsGenerator" path="/summary"/>
        private readonly ISlugsGenerator slugsGenerator;

        #endregion

        #region Constructor

        /// <summary>
        /// Создает экземпляр класса <see cref="AbCarTableRequestExtension"/>.
        /// </summary>
        /// <param name="slugsGenerator"><inheritdoc cref="ISlugsGenerator" path="/summary"/></param>
        public AbCarTableRequestExtension(ISlugsGenerator slugsGenerator) => this.slugsGenerator = NotNullOrThrow(slugsGenerator);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var user = context.Session.User;
            if (!user.IsAdministrator())
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .Error(ValidationKeys.UserIsNotAdmin)
                    .End();

                return;
            }

            var count = Random.Shared.Next(1, 10);

            var result = new List<Dictionary<string, object>>(count)
            {
                new()
                {
                    [GuidKey] = user.ID,
                    [StringKey] = user.Name,
                    [DateTimeKey] = DateTime.UtcNow,
                    [NumberKey] = Random.Shared.Next(),
                    [LinkIDKey] = user.ID,
                    [LinkNameKey] = user.Name
                }
            };

            for (var i = 1; i < count; i++)
            {
                result.Add(new()
                {
                    [GuidKey] = Guid.NewGuid(),
                    [StringKey] = await this.slugsGenerator.GenerateSlugsAsync(cancellationToken: context.CancellationToken),
                    [DateTimeKey] = DateTime.UtcNow.AddDays(i),
                    [NumberKey] = Random.Shared.Next(),
                    [LinkIDKey] = Guid.NewGuid(),
                    [LinkNameKey] = await this.slugsGenerator.GenerateSlugsAsync("{n}", cancellationToken: context.CancellationToken)
                });
            }

            context.Response!.Info[CarTableRequestResultKey] = result;
        }

        #endregion
    }
}
