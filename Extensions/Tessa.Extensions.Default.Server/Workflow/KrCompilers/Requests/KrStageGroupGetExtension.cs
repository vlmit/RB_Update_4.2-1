using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.Requests
{
    public sealed class KrStageGroupGetExtension : CardGetExtension
    {
        #region public

        public override Task AfterRequest(ICardGetExtensionContext context) => AfterRequestInternalAsync(context, context.Response.TryGetCard());

        #endregion

        #region private

        private static async Task AfterRequestInternalAsync(
            ICardExtensionContext context,
            Card card)
        {
            if (!context.ValidationResult.IsSuccessful()
                || card == null
                || !card.Sections.TryGetValue(KrConstants.KrStageGroupTemplatesVirtual.Name, out var tempSec))
            {
                return;
            }

            await using (context.DbScope.Create())
            {
                var db = context.DbScope.Db;
                var query = context.DbScope.BuilderFactory
                    .Select().C(null, KrConstants.KrStageTemplates.ID, KrConstants.KrStageTemplates.NameField)
                    .From(KrConstants.KrStageTemplates.Name).NoLock()
                    .Where().C(KrConstants.KrStageTemplates.StageGroupID).Equals().P("groupID")
                    .Build();
                db.SetCommand(query, db.Parameter("groupID", card.ID));
                await using var reader = await db.ExecuteReaderAsync(context.CancellationToken);
                while (await reader.ReadAsync(context.CancellationToken))
                {
                    var row = tempSec.Rows.Add();
                    var templateID = reader.GetGuid(0);
                    row.RowID = templateID;
                    row[KrConstants.KrStageGroupTemplatesVirtual.TemplateID] = templateID;
                    row[KrConstants.KrStageGroupTemplatesVirtual.TemplateName] = reader.GetString(1);
                }
            }
        }

        #endregion
    }
}
