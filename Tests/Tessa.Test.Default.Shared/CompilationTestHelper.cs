#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Tessa.Platform;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Вспомогательные методы для работы с компиляцией.
    /// </summary>
    public static class CompilationTestHelper
    {
        #region Constants And Static Fields

        /// <summary>
        /// Параметр <c>this</c>.
        /// </summary>
        public const string ThisParameterSingle = "this";

        /// <summary>
        /// Параметр <c>this</c>.
        /// </summary>
        public static readonly IReadOnlyList<string> ThisParameter = new[] { ThisParameterSingle };

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет ссылки на сборки, от которых зависит тип <paramref name="type"/>, и директиву <c>using</c> для пространства имён, в котором расположен данный тип.
        /// </summary>
        /// <param name="sb"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="type">Тип, для которого необходимо сформировать ссылки.</param>
        /// <param name="additionalReferences">Дополнительные ссылки <c>reference</c>.</param>
        /// <param name="additionalUsings">Дополнительные директивы <c>using</c>.</param>
        public static void AppendReferencesAndUsings(
            StringBuilder sb,
            Type type,
            IEnumerable<string>? additionalReferences = null,
            IEnumerable<string>? additionalUsings = null)
        {
            ThrowIfNull(sb);
            ThrowIfNull(type);

            var references = new HashSet<string>();

            var currentType = type;
            var objectType = typeof(object);

            do
            {
                var reference = currentType.Module.Name;
                if (references.Add(reference))
                {
                    sb.AppendLine($"#reference {reference}");
                }

                currentType = currentType.BaseType;
            } while (currentType is not null && currentType != objectType);

            sb.AppendLine($"#using {type.Namespace}");

            if (additionalReferences is not null)
            {
                foreach (var additionalReference in additionalReferences)
                {
                    sb.AppendLine($"#reference {additionalReference}");
                }
            }

            if (additionalUsings is not null)
            {
                foreach (var additionalUsing in additionalUsings)
                {
                    sb.AppendLine($"#using {additionalUsing}");
                }
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Добавляет текст для вызова метода <paramref name="methodName"/>, расположенного в <paramref name="type"/>.
        /// </summary>
        /// <param name="sb"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="type">Тип, содержащий вызываемый метод.</param>
        /// <param name="methodName">Название метода.</param>
        /// <param name="isAsync">Значение <see langword="true"/>, если метод асинхронный, иначе - <see langword="false"/>.</param>
        /// <param name="parameters">Параметры метода.</param>
        public static void AppendCallMethod(
            StringBuilder sb,
            Type type,
            string methodName,
            bool isAsync = false,
            IEnumerable<string?>? parameters = null)
        {
            ThrowIfNull(sb);
            ThrowIfNull(type);
            ThrowIfNullOrEmpty(methodName);

            if (isAsync)
            {
                sb.Append("await ");
            }

            TestHelper.AppendMemberNameWithParameters(
                sb,
                type,
                methodName,
                parameters);

            sb.AppendLine(";");
        }

        /// <summary>
        /// Возвращает сценарий, вызывающий метод <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">Тип, содержащий метод.</param>
        /// <param name="methodName">Название метода.</param>
        /// <param name="isAsync">Значение <see langword="true"/>, если метод асинхронный, иначе - <see langword="false"/>.</param>
        /// <param name="parameters">Параметры метода.</param>
        /// <param name="additionalReferences"><inheritdoc cref="AppendReferencesAndUsings(StringBuilder, Type, IEnumerable{string}?, IEnumerable{string}?)" path="/param[@name='additionalReferences']"/></param>
        /// <param name="additionalUsings"><inheritdoc cref="AppendReferencesAndUsings(StringBuilder, Type, IEnumerable{string}?, IEnumerable{string}?)" path="/param[@name='additionalUsings']"/></param>
        /// <returns></returns>
        public static string GetCallMethodScript(
            Type type,
            string methodName,
            bool isAsync = false,
            IEnumerable<string?>? parameters = null,
            IEnumerable<string>? additionalReferences = null,
            IEnumerable<string>? additionalUsings = null)
        {
            ThrowIfNull(type);
            ThrowIfNullOrEmpty(methodName);

            var sb = StringBuilderHelper.Acquire();

            AppendReferencesAndUsings(
                sb,
                type,
                additionalReferences,
                additionalUsings);

            AppendCallMethod(
                sb,
                type,
                methodName,
                isAsync,
                parameters);

            return sb.ToStringAndRelease();
        }

        #endregion
    }
}
