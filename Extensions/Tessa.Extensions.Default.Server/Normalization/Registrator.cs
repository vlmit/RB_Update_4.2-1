#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Cards.Normalization;
using Tessa.Extensions.Default.Shared;
using Tessa.Normalization;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Data;
using Tessa.Platform.Redis;
using Tessa.Platform.Storage;
using Tessa.Roles;
using Tessa.Scheme;
using Unity;
using Sources = Tessa.Extensions.Default.Server.Normalization.DefaultNormalizationSources;
using PlatformSources = Tessa.Normalization.PlatformNormalizationSources;

namespace Tessa.Extensions.Default.Server.Normalization
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        private static readonly Logger logger = TessaLoggers.Normalization;

        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<DefaultNormalizationOptions>();

        public override void FinalizeRegistration()
        {
            if (this.UnityContainer.TryResolve<INormalizationDescriptorRegistry>() is not { } descriptorRegistry
                || this.UnityContainer.TryResolve<INormalizationInstanceRegistry>() is not { } instanceRegistry
                || this.UnityContainer.TryResolve<INormalizationInvalidatorRegistry>() is not { } invalidatorRegistry)
            {
                return;
            }

            var options = this.UnityContainer.TryResolve<DefaultNormalizationOptions>();
            var redisExpiry = options?.RedisExpiry;
            var inMemoryExpiry = options?.InMemoryExpiry;

            var serverSettings = this.UnityContainer.TryResolve<ITessaServerSettings>();
            var saasEnabled = serverSettings?.SaasSettings.Enabled is true;

            // Currencies
            descriptorRegistry.Register(new() { ID = Sources.Currencies, Name = nameof(Sources.Currencies), KeyType = NormalizationKeyType.Guid });
            instanceRegistry.Register(Sources.Currencies, (factory, descriptor) =>
                factory.Create(new InMemoryNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new RedisNormalizationOptions(descriptor)
                    {
                        Source = factory.Create(new DatabaseNormalizationOptions(descriptor)
                            { TableName = "Currencies", KeyColumnName = "ID", ValueColumnName = "Name" }),
                        PrefetchAll = true,
                        Expiry = redisExpiry
                    }),
                    PrefetchAll = true,
                    Expiry = inMemoryExpiry
                }));
            invalidatorRegistry.Register(
                new GlobalSourceNormalizationInvalidator([Sources.Currencies]),
                new CardNormalizationTrigger([DefaultCardTypes.CurrencyTypeID]) { ValueSectionName = "Currencies", ValueFieldName = "Name" });

            // DocumentCategories
            descriptorRegistry.Register(new() { ID = Sources.DocumentCategories, Name = nameof(Sources.DocumentCategories), KeyType = NormalizationKeyType.Guid });
            instanceRegistry.Register(Sources.DocumentCategories, (factory, descriptor) =>
                factory.Create(new InMemoryNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new RedisNormalizationOptions(descriptor)
                    {
                        Source = factory.Create(new DatabaseNormalizationOptions(descriptor)
                            { TableName = "DocumentCategories", KeyColumnName = "ID", ValueColumnName = "Name" }),
                        PrefetchAll = true,
                        Expiry = redisExpiry
                    }),
                    PrefetchAll = true,
                    Expiry = inMemoryExpiry
                }));
            invalidatorRegistry.Register(
                new GlobalSourceNormalizationInvalidator([Sources.DocumentCategories]),
                new CardNormalizationTrigger([DefaultCardTypes.DocumentCategoryTypeID]) { ValueSectionName = "DocumentCategories", ValueFieldName = "Name" });

            // KrDocStates
            descriptorRegistry.Register(new() { ID = Sources.KrDocStates, Name = nameof(Sources.KrDocStates), KeyType = NormalizationKeyType.Integer });
            instanceRegistry.Register(Sources.KrDocStates, static (factory, descriptor) =>
                factory.Create(new AggregateNormalizationOptions(descriptor)
                {
                    Sources =
                    [
                        factory.Create(new InMemoryNormalizationOptions(descriptor)
                        {
                            Source = factory.Create(new AggregateNormalizationOptions(descriptor)
                            {
                                Sources =
                                [
                                    // в приоритете всегда первый source (который получает данные из схемы)
                                    factory.Create(new SchemeNormalizationOptions(descriptor)
                                        { TableName = "KrDocState", KeyColumnName = "ID", ValueColumnName = "Name" }),
                                    // если в записях таблицы схемы отсутствует состояние Draft, то мы добавляем его из кода
                                    factory.Create(new ConstNormalizationOptions(descriptor)
                                    {
                                        Values = new Dictionary<NormalizationKey, NormalizationValue>
                                        {
                                            { new(0), new("$KrStates_Doc_Draft") }
                                        }
                                    })
                                ]
                            }),
                            PrefetchAll = true
                        }),
                        // это fallback для неизвестных состояний, значение возвращается, но не кэшируется в InMemory (который регистрируется выше)
                        new DelegateNormalizationSource(key => new("$KrStates_Doc_Unknown"))
                    ]
                }));
            invalidatorRegistry.Register(
                new AggregateNormalizationInvalidator([
                    new GlobalSourceNormalizationInvalidator([Sources.KrDocStates]),
                    new DelegateNormalizationInvalidator(static (result, deps) =>
                    {
                        // пример задания дополнительной логики при инвалидации
                        if (logger.IsTraceEnabled)
                        {
                            logger.Trace(
                                "Invalidating normalization source {0} in PID={1}, keys: {2}",
                                nameof(Sources.KrDocStates),
                                Environment.ProcessId,
                                result.InvalidateAll ? "<all>" : string.Join(", ", result.KeyPairs.Select(x => x.Key)));
                        }

                        return Task.CompletedTask;
                    })
                ]),
                SchemeInvalidatedTrigger.Instance);

            // KrTypes
            descriptorRegistry.Register(new() { ID = Sources.KrTypes, Name = nameof(Sources.KrTypes), KeyType = NormalizationKeyType.Guid });
            instanceRegistry.Register(Sources.KrTypes, (factory, descriptor) =>
                factory.Create(new InMemoryNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new AggregateNormalizationOptions(descriptor)
                    {
                        Sources =
                        [
                            factory.Create(new RedisNormalizationOptions(descriptor)
                            {
                                Source = factory.Create(new ViewNormalizationOptions(descriptor)
                                    { ViewAlias = "KrTypesEffective", KeyColumnName = "TypeID", ValueColumnName = "TypeCaption" }),
                                PrefetchAll = true,
                                Expiry = redisExpiry
                            }),
                            factory.Create(new CardTypeNormalizationOptions(descriptor))
                        ]
                    }),
                    PrefetchAll = true,
                    Expiry = inMemoryExpiry
                }));
            invalidatorRegistry.Register(
                new GlobalSourceNormalizationInvalidator([Sources.KrTypes]),
                [
                    // изменение имён типов документов связано с карточками "Тип документа"
                    new CardNormalizationTrigger([DefaultCardTypes.KrDocTypeTypeID]) { ValueSectionName = "KrDocType", ValueFieldName = "Title" },
                    // изменение имён типов карточек связано со сбросом метаинформации карточек
                    CardMetadataInvalidatedTrigger.Instance,
                    // добавление или изменение типов связано с изменением таблицы KrSettingsCardTypes в карточке KrSettings
                    new CardNormalizationTrigger([DefaultCardTypes.KrSettingsTypeID])
                    {
                        ShouldInvalidateOnStoreFunc = static card => card.StoreMode == CardStoreMode.Insert
                            || card.TryGetSections()?.TryGet("KrSettingsCardTypes")?.TryGetRows()?.Count > 0,
                    }
                ]);

            // Partners
            descriptorRegistry.Register(new() { ID = Sources.Partners, Name = nameof(Sources.Partners), KeyType = NormalizationKeyType.Guid });
            instanceRegistry.Register(Sources.Partners, (factory, descriptor) =>
                factory.Create(new RedisNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new DatabaseNormalizationOptions(descriptor)
                        { TableName = "Partners", KeyColumnName = "ID", ValueColumnName = "Name", PartialOnly = true }),
                    RedisFunc = () =>
                    {
                        // если в конфигурации указана дополнительная строка подключения "Redis.Partners", то перенаправляем кэш справочника в указанный Redis
                        if (this.UnityContainer.TryResolve<IConfigurationManager>() is { Errors.Count: 0 } configurationManager)
                        {
                            var settings = configurationManager.Configuration.Settings;
                            var connection = settings.TryGet<string>($"Redis.{nameof(Sources.Partners)}");
                            if (!string.IsNullOrEmpty(connection))
                            {
                                // если требуется Redis, разделяемый между справочниками, то зарегистрируйте его в Unity
                                // как именованный синглтон с указанием параметра initializer как null (и с другими параметрами из контейнера),
                                // а при использовании в свойстве RedisFunc укажите Resolve из контейнера, и в свойстве SkipRedisDisposal = true
                                return new(new RedisConnectionProvider(ct => new(connection), this.UnityContainer.TryResolve<IRedisConnectionStringCleaner>()));
                            }
                        }

                        return new((IRedisConnectionProvider?) null);
                    },
                    Expiry = redisExpiry
                }));
            invalidatorRegistry.Register(
                new SourceNormalizationInvalidator([Sources.Partners]),
                new CardNormalizationTrigger([DefaultCardTypes.PartnerTypeID]) { ValueSectionName = "Partners", ValueFieldName = "Name" });

            // Roles (platform)
            instanceRegistry.Register(PlatformSources.Roles, (factory, descriptor) =>
                factory.Create(new InMemoryNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new RedisNormalizationOptions(descriptor)
                    {
                        Source = factory.Create(new DatabaseNormalizationOptions(descriptor)
                        {
                            TableName = "Roles",
                            KeyColumnName = "ID",
                            ValueColumnName = "Name",
                            QueryFilterAction = static b => b.C("TypeID").NotIn((short) RoleType.Task, (short) RoleType.NestedRole),
                            PartialOnly = true
                        }),
                        Expiry = redisExpiry
                    }),
                    Expiry = inMemoryExpiry
                }));
            invalidatorRegistry
                .Register(
                    new GlobalSourceNormalizationInvalidator([PlatformSources.Roles]),
                    new CardNormalizationTrigger(
                    [
                        // все типы ролей, кроме TaskRole и NestedRole
                        RoleHelper.StaticRoleTypeID, RoleHelper.PersonalRoleTypeID, RoleHelper.DepartmentRoleTypeID, RoleHelper.DynamicRoleTypeID,
                        RoleHelper.ContextRoleTypeID, RoleHelper.MetaRoleTypeID, RoleHelper.SmartRoleTypeID
                    ]) { ValueSectionName = "Roles", ValueFieldName = "Name" });

            // TaskKinds
            descriptorRegistry.Register(new() { ID = Sources.TaskKinds, Name = nameof(Sources.TaskKinds), KeyType = NormalizationKeyType.Guid });
            instanceRegistry.Register(Sources.TaskKinds, (factory, descriptor) =>
                factory.Create(new InMemoryNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new RedisNormalizationOptions(descriptor)
                    {
                        Source = factory.Create(new DatabaseNormalizationOptions(descriptor)
                            { TableName = "TaskKinds", KeyColumnName = "ID", ValueColumnName = "Caption" }),
                        PrefetchAll = true,
                        Expiry = redisExpiry
                    }),
                    PrefetchAll = true,
                    Expiry = inMemoryExpiry
                }));
            invalidatorRegistry.Register(
                new GlobalSourceNormalizationInvalidator([Sources.TaskKinds]),
                new CardNormalizationTrigger([DefaultCardTypes.TaskKindTypeID]) { ValueSectionName = "TaskKinds", ValueFieldName = "Caption" });

            // Types (platform)
            instanceRegistry.Register(PlatformSources.Types, static (factory, descriptor) =>
                factory.Create(new InMemoryNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new CardTypeNormalizationOptions(descriptor)),
                    PrefetchAll = true
                }));
            invalidatorRegistry.Register(
                new GlobalSourceNormalizationInvalidator([PlatformSources.Types]),
                CardMetadataInvalidatedTrigger.Instance);

            // Users (platform)
            instanceRegistry.Register(PlatformSources.Users, (factory, descriptor) =>
                factory.Create(new InMemoryNormalizationOptions(descriptor)
                {
                    Source = factory.Create(new RedisNormalizationOptions(descriptor)
                    {
                        Source = factory.Create(new DatabaseNormalizationOptions(descriptor)
                            { TableName = "PersonalRoles", KeyColumnName = "ID", ValueColumnName = "Name", PartialOnly = saasEnabled }),
                        PrefetchAll = !saasEnabled,
                        Expiry = redisExpiry
                    }),
                    PrefetchAll = !saasEnabled,
                    Expiry = inMemoryExpiry
                }));
            invalidatorRegistry.Register(
                new GlobalSourceNormalizationInvalidator([PlatformSources.Users]),
                new CardNormalizationTrigger([RoleHelper.PersonalRoleTypeID]) { ValueSectionName = "PersonalRoles", ValueFieldName = "Name" });

            // пример задания дополнительной логики в локальном процессе при получении глобального события на инвалидацию кэша Users
            invalidatorRegistry.Register(
                new DelegateNormalizationInvalidator(static (result, deps) =>
                {
                    if (logger.IsTraceEnabled
                        && result is NormalizationSourceInvalidationPayload payload
                        && payload.Sources?.Contains(PlatformSources.Users) is true)
                    {
                        logger.Trace("Invalidating local cache for normalization source {0} in PID={1}", nameof(PlatformSources.Users), Environment.ProcessId);
                    }

                    return Task.CompletedTask;
                }),
                SourceInvalidatedGlobalTrigger.Instance);
        }
    }
}
