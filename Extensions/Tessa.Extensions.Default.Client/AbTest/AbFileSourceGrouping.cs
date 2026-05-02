using Tessa.Localization;
using Tessa.UI.Files;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Группировка по источнику файла.
    /// </summary>
    /// <remarks>
    /// Класс-наследник может переопределить поведение класса, например,
    /// установить сортировку по умолчанию для данной группировки.
    /// </remarks>
    public sealed class AbFileSourceGrouping(string name, string caption, bool isCollapsed = false)
        : FileGrouping(name, caption, isCollapsed)
    {
        #region Constants

        public const string CardSourceGroupName = "AbCardSourceGroup";

        public const string ExternalSourceGroupName = "AbExternalSourceGroup";

        #endregion

        #region Base Overrides

        /// <doc path='info[@type="IFileGrouping" and @item="GetGroupInfo"]'/>
        protected override FileGroupInfo GetGroupInfoCore(IFileViewModel viewModel)
        {
            return viewModel.Model is AbExternalFile
                ? new FileGroupInfo(ExternalSourceGroupName, LocalizeName("AbTest_ExternalFilesGroup"))
                : new FileGroupInfo(CardSourceGroupName, LocalizeName("AbTest_MainFilesGroup"));
        }

        #endregion
    }
}
