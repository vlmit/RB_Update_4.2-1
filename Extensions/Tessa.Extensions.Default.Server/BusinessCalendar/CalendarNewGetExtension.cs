using System;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.BusinessCalendar
{
    /// <summary>
    /// Автоматически проставляем дату и время для вновь добавленных строк в таблицу исключений.
    /// Это нужно для удобства.
    /// </summary>
    public sealed class CalendarNewGetExtension :
        CardNewGetExtension
    {
        #region Base Overrides

        public override async Task AfterRequest(ICardNewExtensionContext context)
        {
            Card card;
            StringDictionaryStorage<CardSection> sections;
            if (!context.RequestIsSuccessful
                || (card = context.Response.TryGetCard()) is null
                || (sections = card.TryGetSections()) is null
                || !sections.TryGetValue(BusinessCalendarHelper.CalendarSettingsSection, out CardSection settings))
            {
                return;
            }

            var settingsFields = settings.Fields;
            await using (context.DbScope!.Create())
            {
                var db = context.DbScope.Db;

                var builderFactory = context.DbScope.BuilderFactory;

                var newCalendarNumericID = await db
                    .SetCommand(
                        builderFactory
                            .Select()
                                .Coalesce(q => q.Max("cs", "CalendarID").Add().V(1).V(0))
                            .From(BusinessCalendarHelper.CalendarSettingsSection, "cs").NoLock()
                            .Build())
                    .LogCommand()
                    .ExecuteAsync<int>(context.CancellationToken);

                settingsFields["CalendarID"] = newCalendarNumericID;
            }

            if (context.Method != CardNewMethod.Template)
            {
                if (settingsFields.ContainsKey("CalendarStart"))
                {
                    settingsFields["CalendarStart"] = new DateTime(DateTime.Now.Year, 1, 1).Date;
                }

                if (settingsFields.ContainsKey("CalendarEnd"))
                {
                    settingsFields["CalendarEnd"] =
                        new DateTime(DateTime.Now.Year, 12, 31).Date
                            .AddDays(1.0)
                            .AddSeconds(-1.0);
                }
            }
        }

        #endregion
    }
}
