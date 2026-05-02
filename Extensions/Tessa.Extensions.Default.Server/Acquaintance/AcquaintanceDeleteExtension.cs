using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Server.Acquaintance
{
    /// <summary>
    /// Расширение, удаляющее записи об ознакомлении при удалении карточки.
    /// </summary>
    public sealed class AcquaintanceDeleteExtension : CardDeleteExtension
    {
        #region Constructors

        public AcquaintanceDeleteExtension(IKrTypesCache typesCache)
        {
            this.typesCache = typesCache;
        }

        #endregion

        #region Fields

        private readonly IKrTypesCache typesCache;

        #endregion

        #region Private Methods

        private static async Task RemoveDataAsync(IDbScope dbScope, Guid cardID, CancellationToken cancellationToken = default)
        {
            await using (dbScope.Create())
            {
                DbManager db = dbScope.Db;

                // удаляем комментарии по ознакомлению
                await db
                    .SetCommand(
                        dbScope.BuilderFactory
                            .DeleteFrom("AcquaintanceComments")
                            .Where().C("ID")
                            .StartIn()
                            .Select().C("CommentID")
                            .From("AcquaintanceRows").NoLock()
                            .Where().C("CardID").Equals().P("CardID")
                            .EndIn()
                            .Build(),
                        db.Parameter("CardID", cardID, DataType.Guid))
                    .LogCommand()
                    .ExecuteNonQueryAsync(cancellationToken);

                // удаляем запись об ознакомлении
                await db
                    .SetCommand(
                        dbScope.BuilderFactory
                            .DeleteFrom("AcquaintanceRows")
                            .Where().C("CardID").Equals().P("CardID")
                            .Build(),
                        db.Parameter("CardID", cardID, DataType.Guid))
                    .LogCommand()
                    .ExecuteNonQueryAsync(cancellationToken);
            }
        }

        #endregion

        #region Base Overrides

        public override async Task BeforeCommitTransaction(ICardDeleteExtensionContext context)
        {
            // если карточка удаляется из корзины
            if (context.CardTypeIs(CardHelper.DeletedTypeID))
            {
                Card card;

                if (!context.Request.GetRestoreMode()
                    && (card = context.TryGetCardToDelete()) != null
                    // Проверка на случай если при восстановлении типа карточки не существует или же она сломана
                    && (await context.CardMetadata.GetCardTypesAsync(context.CancellationToken))
                        .Contains(card.TypeID)
                    && await KrComponentsHelper.HasBaseAsync(card.TypeID, this.typesCache, context.CancellationToken))
                {
                    await RemoveDataAsync(context.DbScope, card.ID, context.CancellationToken);
                }

                return;
            }

            // если карточка удаляется сразу, минуя корзину
            Guid? cardID;
            if (context.Request.DeletionMode == CardDeletionMode.WithoutBackup
                && (cardID = context.Request.CardID).HasValue
                && context.CardType != null
                && await KrComponentsHelper.HasBaseAsync(context.CardType.ID, this.typesCache, context.CancellationToken))
            {
                await RemoveDataAsync(context.DbScope, cardID.Value, context.CancellationToken);
            }
        }

        #endregion
    }
}
