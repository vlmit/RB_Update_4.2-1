#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Test.Default.Shared.Kr.Routes
{
    /// <summary>
    /// Предоставляет вспомогательные методы для тестирования этапов подсистемы маршрутов.
    /// </summary>
    public static class StageTypesTestHelper
    {
        #region Public Methods

        /// <summary>
        /// Возвращает список дескрипторов этапов, которые могут быть без тестов.
        /// </summary>
        /// <returns>Список дескрипторов этапов, которые могут быть без тестов.</returns>
        public static IReadOnlyList<StageTypeDescriptor> GetStaticDescriptorsWithoutTests() => [];

        /// <summary>
        /// Возвращает перечисление дескрипторов этапов, объявленных статическими полями в заданном классе.
        /// </summary>
        /// <param name="descriptorsType">Тип содержащий дескрипторы. Если значение не задано, то поиск выполняется в классе <see cref="StageTypeDescriptors"/>.</param>
        /// <param name="predicate">Условие фильтрации.</param>
        /// <returns>Список дескрипторов, объявленных статическими полями в классе <paramref name="descriptorsType"/>.</returns>
        public static IEnumerable<StageTypeDescriptor> StaticDescriptors(
            Type? descriptorsType = null,
            Func<StageTypeDescriptor, bool>? predicate = null)
        {
            descriptorsType ??= typeof(StageTypeDescriptors);

            foreach (var field in GetAllFields(descriptorsType))
            {
                if (field.FieldType == typeof(StageTypeDescriptor))
                {
                    var descriptor = (StageTypeDescriptor) field.GetValue(null)!;

                    if (predicate?.Invoke(descriptor) != false)
                    {
                        yield return descriptor;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private static List<FieldInfo> GetAllFields(Type type)
        {
            var fieldInfoList = new List<FieldInfo>();

            var currentType = type;
            var objectType = typeof(object);

            do
            {
                var fields = TypeExtensions.GetFields(
                    currentType,
                    BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);

                fieldInfoList.AddRange(fields);

                currentType = currentType.BaseType;
            } while (currentType is not null && currentType != objectType);

            return fieldInfoList;
        }

        #endregion
    }
}
