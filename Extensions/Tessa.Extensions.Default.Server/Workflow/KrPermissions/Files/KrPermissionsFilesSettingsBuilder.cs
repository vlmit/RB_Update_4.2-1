#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Collections;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    /// <summary>
    /// Билдер для построения оптимального списка настроек доступа новых файлов по общим настройкам доступа к файлам <see cref="KrPermissionsFileRule"/>.
    /// </summary>
    public sealed class KrPermissionsFilesSettingsBuilder
    {
        #region Nested Types

        private class FileExtensionsAccessSettingsMerger
        {
            #region Fields

            private bool isNew = true;
            private bool isFinished;

            private HashSet<Guid>? allowedCategories;
            private HashSet<Guid>? disallowedCategories;

            public bool? IsAllowed;
            public HashSet<Guid>? LockedAllowedCategories;
            public HashSet<Guid>? LockedDisallowedCategories;

            #endregion

            #region Public Methods

            public void AddCategories(
                bool isAllowed,
                ICollection<Guid> categories)
            {
                if (this.isFinished)
                {
                    return;
                }

                if (categories is null or { Count: 0 })
                {
                    this.IsAllowed = isAllowed
                        ? this.IsAllowed ?? isAllowed
                        : isAllowed;

                    if (isAllowed)
                    {
                        this.allowedCategories = null;
                    }
                    else
                    {
                        this.disallowedCategories = null;
                        this.allowedCategories = null;
                    }
                }
                else if (!isAllowed)
                {
                    this.disallowedCategories ??= [];
                    this.disallowedCategories.UnionWith(categories);
                    this.allowedCategories?.ExceptWith(categories);
                }
                else if (this.IsAllowed is null)
                {
                    var categoriesToAdd =
                        this.disallowedCategories is null
                            ? categories
                            : [.. categories.Where(x => !this.disallowedCategories.Contains(x))];

                    this.allowedCategories ??= [];
                    this.allowedCategories.UnionWith(categoriesToAdd);
                }
            }

            public void LockCurrentSettings(FileExtensionsAccessSettingsMerger? withoutExtensionMerger)
            {
                if (this.isFinished)
                {
                    return;
                }

                if (withoutExtensionMerger is not null)
                {
                    if (withoutExtensionMerger.IsAllowed == false)
                    {
                        // Если у билдера для всех расширений стоит полный запрет, то для текущего правила удаляем все текущие настройки и ставим полный запрет.
                        // Уже рассчитанные разрешения (если таковы имеются) останутся и будут предоставлять доступ.
                        this.IsAllowed = false;
                        this.allowedCategories = null;
                        this.disallowedCategories = null;
                        this.isFinished = true;
                    }
                    else if (withoutExtensionMerger.IsAllowed == true)
                    {
                        // Если у билдера для всех расширений стоит полное разрешение, то для текущего правила удаляем все текущие настройки разрешения и ставим полное разрешение, если не стоит полный запрет.
                        // Уже рассчитанные разрешения (если таковы имеются) останутся и будут предоставлять доступ.
                        this.IsAllowed ??= true;
                        this.allowedCategories = null;
                        this.isFinished = true;
                    }
                    else
                    {
                        // Если у билдера для всех расширений нет полной настройки разрешения, то просто очищаем списки с текущими настройками по условиям глобальных настроек.
                        // Если текущий билдер переходит в режим полного разрешения, то все актуальные глобальные запреты нужно перенести в текущий билдер.
                        // Если текущий билдер переходит в режим полного запрета, то все актуальные глобальные разрешения нужно перенести в текущий билдер.
                        if (withoutExtensionMerger.LockedDisallowedCategories is not null)
                        {
                            this.allowedCategories?.ExceptWith(withoutExtensionMerger.LockedDisallowedCategories);
                            if (this.IsAllowed == true)
                            {
                                this.disallowedCategories ??= [];
                                this.disallowedCategories.UnionWith(withoutExtensionMerger.LockedDisallowedCategories);
                            }
                        }

                        if (withoutExtensionMerger.LockedAllowedCategories is not null)
                        {
                            this.disallowedCategories?.ExceptWith(withoutExtensionMerger.LockedAllowedCategories);
                            if (this.IsAllowed == false)
                            {
                                this.allowedCategories ??= [];
                                this.allowedCategories.UnionWith(withoutExtensionMerger.LockedAllowedCategories);
                            }
                        }
                    }
                }

                if (this.isNew)
                {
                    this.isNew = false;
                    this.LockedAllowedCategories = this.allowedCategories;
                    this.LockedDisallowedCategories = this.disallowedCategories;
                    if (this.IsAllowed.HasValue)
                    {
                        this.isFinished = true;
                    }
                }
                else if (this.IsAllowed is null)
                {
                    if (this.disallowedCategories is not null)
                    {
                        this.LockedDisallowedCategories ??= new HashSet<Guid>(this.disallowedCategories.Count);
                        this.LockedDisallowedCategories.UnionWith(
                            this.LockedAllowedCategories is null
                                ? this.disallowedCategories
                                : this.disallowedCategories.Where(x => !this.LockedAllowedCategories.Contains(x)));
                    }

                    if (this.allowedCategories is not null)
                    {
                        this.LockedAllowedCategories ??= new HashSet<Guid>(this.allowedCategories.Count);
                        this.LockedAllowedCategories.UnionWith(
                            this.LockedDisallowedCategories is null
                                ? this.allowedCategories
                                : this.allowedCategories.Where(x => !this.LockedDisallowedCategories.Contains(x)));
                    }
                }
                else if (this.IsAllowed.Value)
                {
                    if (this.disallowedCategories is not null)
                    {
                        this.LockedDisallowedCategories?.UnionWith(
                            this.LockedAllowedCategories is null
                                ? this.disallowedCategories
                                : this.disallowedCategories.Where(x => !this.LockedAllowedCategories.Contains(x)));
                    }

                    this.LockedAllowedCategories = null;
                    this.isFinished = true;
                }
                else
                {
                    this.LockedDisallowedCategories = null;
                    this.isFinished = true;
                }

                this.allowedCategories = null;
                this.disallowedCategories = null;
            }

            #endregion
        }

        private class FileExtensionSettingsBuilder
        {
            #region Fields

            private FileExtensionsAccessSettingsMerger? addMerger;
            private FileExtensionsAccessSettingsMerger? signMerger;

            #endregion

            #region Constructors

            public FileExtensionSettingsBuilder(
                string extension)
            {
                this.Extension = extension;
            }

            #endregion

            #region Properties

            public string Extension { get; }

            #endregion

            #region Public Methods

            public void AddCategories(
                bool? addAllowed,
                bool? signAllowed,
                ICollection<Guid> categories)
            {
                if (addAllowed is not null)
                {
                    this.addMerger ??= new();
                    this.addMerger.AddCategories(
                        addAllowed.Value,
                        categories);
                }

                if (signAllowed is not null)
                {
                    this.signMerger ??= new();
                    this.signMerger.AddCategories(
                        signAllowed.Value,
                        categories);
                }
            }

            public void LockCurrentSettings(FileExtensionSettingsBuilder? withoutExtensionBuilder)
            {
                this.addMerger?.LockCurrentSettings(withoutExtensionBuilder?.addMerger);
                this.signMerger?.LockCurrentSettings(withoutExtensionBuilder?.signMerger);
            }

            public KrPermissionsFileExtensionSettings BuildTo(KrPermissionsFilesSettings settings)
            {
                KrPermissionsFileExtensionSettings result;
                var flag = KrPermissionsFilesAccessFlag.None;

                if (this.Extension == WithoutExtension)
                {
                    settings.GlobalSettings = result = new KrPermissionsFileExtensionSettings(string.Empty);
                }
                else
                {
                    result = settings.ExtensionSettings.Add(this.Extension.ToLowerInvariant());
                }

                if (this.addMerger is not null)
                {
                    if (this.addMerger.LockedAllowedCategories is { Count: > 0 })
                    {
                        result.AllowedCategories = this.addMerger.LockedAllowedCategories.ToArray();
                    }

                    if (this.addMerger.LockedDisallowedCategories is { Count: > 0 })
                    {
                        result.DisallowedCategories = this.addMerger.LockedDisallowedCategories.ToArray();
                    }

                    switch (this.addMerger.IsAllowed)
                    {
                        case true:
                            flag = KrPermissionsFilesAccessFlag.AddAllowed;
                            break;

                        case false:
                            flag = KrPermissionsFilesAccessFlag.AddProhibited;
                            break;
                    }
                }

                if (this.signMerger is not null)
                {
                    if (this.signMerger.LockedAllowedCategories is { Count: > 0 })
                    {
                        var signAllowedCategories = this.signMerger.LockedAllowedCategories.ToArray();

                        if (signAllowedCategories.Length > 0)
                        {
                            result.SignAllowedCategories = signAllowedCategories;
                        }
                    }

                    if (this.signMerger.LockedDisallowedCategories is { Count: > 0 })
                    {
                        var signDisallowedCategories = this.signMerger.LockedDisallowedCategories.ToArray();

                        if (signDisallowedCategories.Length > 0)
                        {
                            result.SignDisallowedCategories = signDisallowedCategories;
                        }
                    }

                    if (this.signMerger.IsAllowed == true)
                    {
                        flag |= KrPermissionsFilesAccessFlag.SignAllowed;
                    }
                    else if (this.signMerger.IsAllowed == false)
                    {
                        flag |= KrPermissionsFilesAccessFlag.SignProhibited;
                    }
                }

                result.Flag = flag;

                return result;
            }

            #endregion
        }

        #endregion

        #region Fields and Constants

        private Dictionary<int, List<KrPermissionsFileRule>>? rulesByPriority;

        private const string WithoutExtension = "*";

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет правило проверки доступа к файлам для построения настроек новых файлов.
        /// </summary>
        /// <param name="fileRule"><inheritdoc cref="KrPermissionsFileRule" path="/summary"/></param>
        public void Add(KrPermissionsFileRule fileRule)
        {
            ThrowIfNull(fileRule);

            if (fileRule.AddAccessSetting is null
                && fileRule.SignAccessSetting is null
                && fileRule.FileSizeLimit is null
                && fileRule.MaxCount is null
                && fileRule.Mandatory is false)
            {
                return;
            }

            this.rulesByPriority ??= [];
            if (this.rulesByPriority.TryGetValue(fileRule.Priority, out var rules))
            {
                rules.Add(fileRule);
            }
            else
            {
                this.rulesByPriority[fileRule.Priority] = [fileRule];
            }
        }

        /// <summary>
        /// Выполняет построение настроек доступа на добавление файлов по добавленным правилам.
        /// </summary>
        /// <returns>Настройки доступа на добавление файлов или <c>null</c>, если в правилах проверки доступа файлов нет настроек доступа на добавление файлов.</returns>
        public KrPermissionsFilesSettings? Build()
        {
            if (this.rulesByPriority is null)
            {
                return null;
            }

            var extensionsBuilders = new HashSet<string, FileExtensionSettingsBuilder>(x => x.Extension, StringComparer.OrdinalIgnoreCase);
            foreach (var (_, rules) in this.rulesByPriority.OrderByDescending(x => x.Key))
            {
                foreach (var rule in rules)
                {
                    var addAllowed = rule.AddAccessSetting is null
                        ? (bool?) null
                        : rule.AddAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Allowed;
                    var signAllowed = rule.SignAccessSetting is null
                        ? (bool?) null
                        : rule.SignAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Allowed;

                    if (rule.Extensions is null or { Count: 0 })
                    {
                        AddExtension(
                            addAllowed,
                            signAllowed,
                            extensionsBuilders,
                            WithoutExtension,
                            rule.Categories);
                    }
                    else
                    {
                        foreach (var extension in rule.Extensions)
                        {
                            AddExtension(
                                addAllowed,
                                signAllowed,
                                extensionsBuilders,
                                extension,
                                rule.Categories);
                        }
                    }
                }

                if (extensionsBuilders.TryGetItem(WithoutExtension, out var withoutExtensionBuilder))
                {
                    withoutExtensionBuilder.LockCurrentSettings(null);
                }

                foreach (var builder in extensionsBuilders)
                {
                    if (builder != withoutExtensionBuilder)
                    {
                        builder.LockCurrentSettings(withoutExtensionBuilder);
                    }
                }
            }

            var result = new KrPermissionsFilesSettings();
            foreach (var builder in extensionsBuilders)
            {
                _ = builder.BuildTo(result);
            }

            return result;
        }

        #endregion

        #region Private Methods

        private static void AddExtension(
            bool? addAllowed,
            bool? signAllowed,
            HashSet<string, FileExtensionSettingsBuilder> extensionBuilders,
            string extension,
            ICollection<Guid> categories)
        {
            if (!extensionBuilders.TryGetItem(extension, out var builder))
            {
                extensionBuilders[extension] = builder = new FileExtensionSettingsBuilder(extension);
            }

            builder.AddCategories(addAllowed, signAllowed, categories);
        }

        #endregion
    }
}
