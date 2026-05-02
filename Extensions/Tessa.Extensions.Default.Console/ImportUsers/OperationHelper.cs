using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Tessa.Extensions.Default.Console.ImportUsers
{
    public static class OperationHelper
    {
        #region Fields

        public static readonly Regex TimeZoneOffsetRegex = new("^(?:Z|[+-](?:2[0-3]|[01][0-9]):[0-5][0-9])$");

        #endregion

        public static string? GetDataFromCell(this IEnumerable<Cell> cells, int column, int row, SharedStringTable sst)
        {
            var c = cells.FirstOrDefault(x => x.CellReference == $"{(char)(65 + column)}{row}");
            if (c is null)
            {
                return string.Empty;
            }
            if (c.DataType is not null && c.DataType == CellValues.SharedString)
            {
                int ssid = int.Parse(c.CellValue?.Text ?? string.Empty);
                return sst.ChildElements[ssid].InnerText;
            }

            return c.CellValue?.Text;
        }

        public static string GetUniqueKey(int maxSize)
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = RandomNumberGenerator.GetBytes(maxSize);

            StringBuilder result = new(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % chars.Length]);
            }
            return result.ToString();
        }
    }
}