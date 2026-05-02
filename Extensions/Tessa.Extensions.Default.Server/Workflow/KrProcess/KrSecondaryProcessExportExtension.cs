using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.SmartMerge;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Десериализует настройки в таблице с условиями так, чтобы они выгружались в файл как единый json вместо строки с json.
    /// </summary>
    public sealed class KrSecondaryProcessExportExtension :
        CardGetExtension
    {
        #region Base Overrides

        public override Task AfterRequest(ICardGetExtensionContext context)
        {
            Card card;
            StringDictionaryStorage<CardSection> sections;

            if (!context.ValidationResult.IsSuccessful()
                || (card = context.Response.TryGetCard()) is null
                || (sections = card.TryGetSections()) is null
                || !sections.TryGetValue(KrConstants.KrSecondaryProcesses.Name, out CardSection section))
            {
                return Task.CompletedTask;
            }

            Dictionary<string, object> fields = section.RawFields;

            var shouldExpandJson = context.Request.ShouldExpandJson();
            var isSmartMerge = context.Request.TryGetSmartMergeFlag();
            var conditionsObj = fields[KrConstants.KrSecondaryProcesses.Conditions];

            if (!shouldExpandJson && !isSmartMerge)
            {
                return Task.CompletedTask;
            }

            switch (conditionsObj)
            {
                case IList list:
                {
                    foreach (object item in list)
                    {
                        if (item is Dictionary<string, object> obj
                            && obj.TryGetValue("Settings", out object settingsObj)
                            && settingsObj is string settings
                            && settings.StartsWith('{'))
                        {
                            if (shouldExpandJson)
                            {
                                obj["Settings"] = StorageHelper.DeserializeFromTypedJson(settings);
                            }

                            obj["Description"] = null;
                        }
                    }

                    break;
                }
                case string str when str.StartsWith('['):
                {
                    // Десериализовать в List.
                    if (StorageHelper.DeserializeListFromTypedJson(str) is not { } conditionsList)
                    {
                        return Task.CompletedTask;
                    }

                    // Занулить Description.
                    foreach (var item in conditionsList.OfType<Dictionary<string, object>>())
                    {
                        item["Description"] = null;
                    }

                    // Сериализовать обратно в строку и присвоить.
                    var newStr = StorageHelper.SerializeToTypedJson(conditionsList);
                    fields[KrConstants.KrSecondaryProcesses.Conditions] = newStr;
                    break;
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
