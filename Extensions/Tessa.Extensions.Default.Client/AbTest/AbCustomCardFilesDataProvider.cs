#nullable enable

using System.Collections.Generic;
using Tessa.Extensions.Default.Client.UI.CardFiles;
using Tessa.Scheme;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Files;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Custom data provider for files in view extension.
    /// </summary>
    /// <param name="viewMetadata"><inheritdoc cref="ViewMetadata" path="/summary"/></param>
    /// <param name="fileControl"><inheritdoc cref="FileControl" path="/summary"/></param>
    public sealed class AbCustomCardFilesDataProvider(IViewMetadata viewMetadata, IFileControl fileControl)
        : CardFilesDataProvider(viewMetadata, fileControl)
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override void AddColumns(IGetDataResponse response)
        {
            base.AddColumns(response);
            response.Columns.Add(new("Date", SchemeType.DateTime));
            response.Columns.Add(new("Description", SchemeType.NullableString));
        }

        /// <inheritdoc/>
        protected override Dictionary<string, object?> MapFileToRow(IFileViewModel file)
        {
            var row = base.MapFileToRow(file);
            row["Date"] = file.Model.Created;
            row["Description"] = (file.Model as AbExternalFile)?.Description;
            return row;
        }

        #endregion
    }
}
