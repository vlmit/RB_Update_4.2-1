#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Tessa.Compilation;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <inheritdoc cref="IKrStageTemplateCompilationCache"/>
    public sealed class KrStageTemplateCompilationCache :
        KrCompilationCacheWithLoadDependenciesBase,
        IKrStageTemplateCompilationCache
    {
        #region Fields

        private readonly IKrProcessCache krProcessCache;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="tessaCompilationInvalidationReceiver"><inheritdoc cref="TessaCompilationInvalidationReceiver" path="/summary"/></param>
        /// <param name="compiler"><inheritdoc cref="IKrCompiler" path="/summary"/></param>
        /// <param name="tessaCompilationRepository"><inheritdoc cref="ITessaCompilationRepository" path="/summary"/></param>
        /// <param name="typeProvider"><inheritdoc cref="ITypeProvider" path="/summary"/></param>
        /// <param name="typeIdentifierProvider"><inheritdoc cref="ITypeIdentifierProvider{T}" path="/summary"/></param>
        /// <param name="instanceCreationStrategy"><inheritdoc cref="IInstanceCreationStrategy" path="/summary"/></param>
        /// <param name="krProcessCache"><inheritdoc cref="IKrProcessCache" path="/summary"/></param>
        /// <param name="commonMethodCompilationCache"><inheritdoc cref="IKrCommonMethodCompilationCache" path="/summary"/></param>
        /// <param name="unityDisposableContainer"><inheritdoc cref="IUnityDisposableContainer" path="/summary"/></param>
        public KrStageTemplateCompilationCache(
            TessaCompilationInvalidationReceiver tessaCompilationInvalidationReceiver,
            IKrCompiler compiler,
            ITessaCompilationRepository tessaCompilationRepository,
            ITypeProvider typeProvider,
            ITypeIdentifierProvider<string> typeIdentifierProvider,
            IInstanceCreationStrategy instanceCreationStrategy,
            IKrProcessCache krProcessCache,
            IKrCommonMethodCompilationCache commonMethodCompilationCache,
            [OptionalDependency] IUnityDisposableContainer? unityDisposableContainer = null)
            : base(
                DefaultCompilationCacheNames.KrStageTemplate,
                tessaCompilationInvalidationReceiver,
                compiler,
                tessaCompilationRepository,
                typeProvider,
                typeIdentifierProvider,
                instanceCreationStrategy,
                commonMethodCompilationCache,
                unityDisposableContainer)
        {
            this.krProcessCache = NotNullOrThrow(krProcessCache);

            this.GetDependentInvalidationActions(DefaultCompilationCacheNames.KrCommonMethod)
                .Add(this.KrCommonMethodInvalidationActionAsync);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async Task<TessaCompilationContext<IKrCompilationContext>> GetCompilerContextAsync(
            Guid id,
            Func<IKrCompilationContext> getCompilerContextFunc,
            CancellationToken cancellationToken = default)
        {
            var stageTemplates = await this.krProcessCache.GetAllStageTemplatesAsync(cancellationToken);
            if (!stageTemplates.TryGetValue(id, out var stageTemplate))
            {
                return new(id) { ValidationResult = this.CreateSourceObjectNotFoundValidationResult(id) };
            }

            IKrCompilationContext? context = null;

            if (stageTemplate.HasSource)
            {
                context = getCompilerContextFunc();
                context.StageTemplates.Add(stageTemplate);
            }

            var stages = (await this.krProcessCache.GetAllStagesByTemplatesAsync(cancellationToken))
                .GetValueOrDefault(id, Array.Empty<IKrRuntimeStage>());

            if (context is not null || stages.Any(static i => i.HasSource))
            {
                context ??= getCompilerContextFunc();
                context.Stages.AddRange(stages);
                context.SimpleAssemblyName = CompilationHelper.GetSimpleAssemblyName(this.CategoryID, id);

                var commonMethods = await this.krProcessCache.GetAllCommonMethodsAsync(cancellationToken);
                var commonMethod = commonMethods.MinBy(static i => i.ID);
                if (commonMethod is null)
                {
                    context.CommonMethods.Add(KrCommonMethod.FakeObject);
                }
                else
                {
                    var (result, reference) = await CompilationHelper.CreateReferenceToCompilationObjectAsync(
                        this.CommonMethodCompilationCache,
                        commonMethod.ID,
                        cancellationToken);

                    if (reference is null)
                    {
                        return new(id) { ValidationResult = result };
                    }

                    context.MetadataReferences.Add(reference);
                }
            }

            return new(id) { CompilerContext = context };
        }

        /// <inheritdoc/>
        protected override async Task<IList<TessaCompilationContext<IKrCompilationContext>>> GetCompilerContextAsync(
            Func<IKrCompilationContext> getCompilerContextFunc,
            CancellationToken cancellationToken = default)
        {
            var commonMethods = await this.krProcessCache.GetAllCommonMethodsAsync(cancellationToken);
            var stageTemplates = await this.krProcessCache.GetAllStageTemplatesAsync(cancellationToken);
            var contexts = new List<TessaCompilationContext<IKrCompilationContext>>(stageTemplates.Count);
            var commonMethod = commonMethods.MinBy(static i => i.ID);

            var result = ValidationResult.Empty;
            PortableExecutableReference? reference = null;

            if (commonMethod is not null)
            {
                (result, reference) = await CompilationHelper.CreateReferenceToCompilationObjectAsync(
                    this.CommonMethodCompilationCache,
                    commonMethod.ID,
                    cancellationToken);
            }

            foreach (var stageTemplate in stageTemplates)
            {
                if (!result.IsSuccessful)
                {
                    contexts.Add(new(stageTemplate.Key) { ValidationResult = result });
                    continue;
                }

                IKrCompilationContext? context = null;

                if (stageTemplate.Value.HasSource)
                {
                    context = getCompilerContextFunc();
                    context.StageTemplates.Add(stageTemplate.Value);
                }

                var stages = (await this.krProcessCache.GetAllStagesByTemplatesAsync(cancellationToken))
                    .GetValueOrDefault(stageTemplate.Key, Array.Empty<IKrRuntimeStage>());

                if (context is not null || stages.Any(static i => i.HasSource))
                {
                    context ??= getCompilerContextFunc();
                    context.Stages.AddRange(stages);
                    context.SimpleAssemblyName = CompilationHelper.GetSimpleAssemblyName(this.CategoryID, stageTemplate.Key);

                    if (reference is null)
                    {
                        context.CommonMethods.Add(KrCommonMethod.FakeObject);
                    }
                    else
                    {
                        context.MetadataReferences.Add(reference);
                    }
                }

                contexts.Add(new(stageTemplate.Key) { CompilerContext = context });
            }

            return contexts;
        }

        #endregion

        #region Private Methods

        private ValueTask KrCommonMethodInvalidationActionAsync(
            TessaCompilationInvalidationPayload payload) =>
            this.InvalidateLocalAsync(this.CreateInvalidationPayloadForRelatedCache(payload, null));

        #endregion
    }
}
