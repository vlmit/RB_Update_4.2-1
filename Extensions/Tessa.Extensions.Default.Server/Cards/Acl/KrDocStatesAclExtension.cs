#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions.Templates;
using Tessa.Extensions.Default.Server.RefGroups;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Platform.Storage;
using Tessa.Roles.Acl;
using Tessa.Roles.Acl.Extensions;
using Tessa.Roles.Queries;
using Tessa.Roles.Queries.Parts;
using Tessa.Roles.Triggers;

namespace Tessa.Extensions.Default.Server.Cards.Acl
{
    /// <summary>
    /// Расширение, дополнительно проверяющее состояние карточки при расчёте ACL.
    /// </summary>
    public sealed class KrDocStatesAclExtension(IDbScope dbScope, IRefGroupsManager refGroupsManager) : AclGenerationRuleExtensionBase
    {
        #region Constants

        private const string AclExtensionsStates = "AclExtensions_States";
        private const string StatesKey = StorageHelper.SystemKeyPrefix + "states";
        private const string StateGroupsKey = StorageHelper.SystemKeyPrefix + "stateGroups";

        #endregion

        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IRefGroupsManager refGroupsManager = NotNullOrThrow(refGroupsManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override AclGenerationRuleExtensionDescriptor Descriptor => AclExtensionDescriptors.KrDocStates;

        /// <inheritdoc/>
        public override async ValueTask ModifyGenerationRuleDataAsync(
            IAclGenerationRuleData ruleData,
            CancellationToken cancellationToken = default)
        {
            if (!ruleData.ExtensionsData.TryGetValue(AclExtensionsStates, out var statesDataObj)
                || statesDataObj is not Dictionary<string, object?> statesData)
            {
                return;
            }
            var statesSection = new CardSection(AclExtensionsStates, statesData);
            var allStates = statesSection.Rows.Select(x => x.TryGet<int>("StateID"));
            var allStateGroups = await this.refGroupsManager.GetGroupsByTypeAsync(KrRefGroupsHelper.KrStateRefGroupTypeID, cancellationToken);
            List<int>? states = null;
            List<int>? stateGroups = null;
            foreach (var state in allStates)
            {
                if (allStateGroups.Any(x => x.IntID == state))
                {
                    stateGroups ??= [];
                    stateGroups.Add(state);
                }
                else
                {
                    states ??= [];
                    states.Add(state);
                }
            }

            ruleData.ExtensionsData[StatesKey] = states;
            ruleData.ExtensionsData[StateGroupsKey] = stateGroups;
            ruleData.TriggerOnTypes.Add(DefaultCardTypes.KrSatelliteTypeID);
        }

        /// <inheritdoc/>
        public override ValueTask ModifyGetCardsQueryAsync(
            IComplexQueryBuilder queryBuilder,
            AclGenerationRuleDataSource ruleData,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(queryBuilder);
            ThrowIfNull(ruleData);

            if (!TryGetStates(ruleData.ExtensionsData, out var states, out var stateGroups))
            {
                return ValueTask.CompletedTask;
            }

            queryBuilder.AddTableJoin(
                nameof(KrDocStatesAclExtension),
                KrConstants.KrApprovalCommonInfo.Name,
                KrConstants.KrProcessCommonInfo.MainCardID,
                true,
                new FuncQueryPart(async (builder, dataParameters, ct) =>
                {
                    List<int>? allStates;
                    if (stateGroups is null)
                    {
                        allStates = states!;
                    }
                    else
                    {
                        allStates = states is null
                            ? []
                            : [..states];

                        foreach (var stateGroupID in stateGroups)
                        {
                            var stateGroup = await this.refGroupsManager.GetGroupAsync(stateGroupID, KrRefGroupsHelper.KrStateRefGroupTypeID, ct);
                            
                            if (stateGroup is { CalculatedValues.Count: > 0 })
                            {
                                allStates.AddRange(stateGroup.CalculatedValues.Select(x => (int) x.ID));
                            }
                        }
                    }

                    if (allStates.Count == 0)
                    {
                        builder.V(1).Equals().V(0);
                    }
                    else
                    {
                        builder.Coalesce(b => b.C(nameof(KrDocStatesAclExtension), KrConstants.KrApprovalCommonInfo.StateID).V(KrState.Draft.ID)).InArray(allStates, "States", out var parameter);
                        dataParameters.Add(parameter);
                    }
                }),
                new FuncQueryPart((builder, _) => builder.Coalesce(b => b.C(nameof(KrDocStatesAclExtension), KrConstants.KrApprovalCommonInfo.StateID).V(KrState.Draft.ID))));

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override async ValueTask ModifyGetUpdateCardsByTriggersAsync(
            CheckTriggersResult result,
            CheckTriggersRequest request,
            Dictionary<string, object?> extensionsData,
            CancellationToken cancellationToken = default)
        {
            if (request.TriggerCard is not null
                && request.TriggerCard.TypeID == DefaultCardTypes.KrSatelliteTypeID
                && request.TriggerCard.Sections.TryGetValue("KrApprovalCommonInfo", out var krSection)
                && krSection.RawFields.TryGetValue("StateID", out var stateObj)
                && stateObj is int stateID)
            {
                var mainCardID = await CardSatelliteHelper.TryGetMainCardIDFromSatelliteIDAsync(
                    this.dbScope,
                    request.TriggerCard.ID,
                    "KrApprovalCommonInfo",
                    "MainCardID",
                    cancellationToken);

                if (mainCardID.HasValue)
                {
                    result.CardIDsWithData[mainCardID.Value] = new Dictionary<string, object?> { ["StateID"] = stateID };
                }
            }
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> AllowedForCardAsync(
            Guid cardID,
            Dictionary<string, object?>? additionalData,
            Dictionary<string, object?> extensionsData,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetStates(extensionsData, out var states, out var stateGroups))
            {
                return false;
            }

            int stateID;
            if (additionalData is not null
                && additionalData.TryGetValue("StateID", out var stateIDObj))
            {
                stateID = stateIDObj is null
                    ? KrState.Draft.ID
                    : (int) stateIDObj;
            }
            else
            {
                await using var s = this.dbScope.Create();
                var db = this.dbScope.Db;
                var builder = this.dbScope.BuilderFactory;

                stateID = await db.SetCommand(
                    builder
                        .Select().C("StateID")
                        .From("KrApprovalCommonInfo").NoLock()
                        .Where().C("MainCardID").Equals().P("CardID")
                        .Build(),
                    db.Parameter("CardID", cardID))
                    .LogCommand()
                    .ExecuteAsync<int?>(cancellationToken) ?? KrState.Draft.ID;
            }

            if (states?.Contains(stateID) == true)
            {
                return true;
            }
            
            if (stateGroups is not null)
            {
                foreach (var stateGroupID in stateGroups)
                {
                    var stateGroup = await this.refGroupsManager.GetGroupAsync(stateGroupID, KrRefGroupsHelper.KrStateRefGroupTypeID, cancellationToken);
                    if (stateGroup?.CalculatedValues?.Any(x => (int) x.ID == stateID) == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        private static bool TryGetStates(
            Dictionary<string, object?> extensionsData,
            out List<int>? states,
            out List<int>? stateGroups)
        {
            bool result = false;

            if (extensionsData.TryGetValue(StatesKey, out var statesObj)
                && statesObj is List<int> { Count: > 0 } statesResult)
            {
                states = statesResult;
                result = true;
            }
            else
            {
                states = null;
            }

            if (extensionsData.TryGetValue(StateGroupsKey, out var stateGroupsObj)
                && stateGroupsObj is List<int> { Count: > 0 } stateGroupsResult)
            {
                stateGroups = stateGroupsResult;
                result = true;
            }
            else
            {
                stateGroups = null;
            }

            return result;
        }

        #endregion
    }
}
