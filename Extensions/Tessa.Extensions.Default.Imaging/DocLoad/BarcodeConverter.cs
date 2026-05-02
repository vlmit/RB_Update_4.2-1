using Tessa.Imaging.DocLoad;
using Type = BarcodeStandard.Type;

namespace Tessa.Extensions.Default.Imaging.DocLoad
{
    /// <inheritdoc cref="IBarcodeConverter" />
    /// <remarks>
    /// Наследник класса может переопределять методы, возвращая идентификаторы для дополнительных штрих-кодов,
    /// например, доступных в рамках проекта с поддержкой другой библиотеки.
    /// </remarks>
    public class BarcodeConverter :
        IBarcodeConverter
    {
        #region IBarcodeConverter Members

        /// <inheritdoc />
        public virtual int GetBarcodeForRead(string? name) =>
            name switch
            {
                "AZTEC" => 1,
                "CODABAR" => 2,
                "CODE_39" => 4,
                "CODE_93" => 8,
                "CODE_128" => 16,
                "DATA_MATRIX" => 32,
                "EAN_8" => 64,
                "EAN_13" => 128,
                "ITF" => 256,
                "MAXICODE" => 512,
                "PDF_417" => 1024,
                "QR_CODE" => 2048,
                "RSS_14" => 4096,
                "RSS_EXPANDED" => 8192,
                "UPC_A" => 16384,
                "UPC_E" => 32768,
                "All_1D" => 61918,
                "UPC_EAN_EXTENSION" => 65536,
                "MSI" => 131072,
                "PLESSEY" => 262144,
                "IMB" => 524288,
                _ => -1,
            };


        /// <inheritdoc />
        public virtual Type GetBarcodeForWrite(string? name) =>
            name switch
            {
                "CODABAR" => Type.Codabar,
                "CODE_39" => Type.Code39,
                "CODE_93" => Type.Code93,
                "CODE_128" => Type.Code128,
                "EAN_13" => Type.Ean13,
                "EAN_8" => Type.Ean8,
                "ITF" => Type.Itf14,
                "UPC_A" => Type.UpcA,
                "UPC_E" => Type.UpcE,
                "MSI" => Type.MsiMod10,
                "PLESSEY" => Type.ModifiedPlessey,
                _ => Type.Unspecified
            };

        #endregion
    }
}
