#nullable enable
using System;

namespace Tessa.Extensions.Default.Shared.AbTest
{
    /// <summary>
    /// Идентификаторы и имена типов карточек, задействованных в тестовом решении <c>AbTest</c>.
    /// </summary>
    public static class AbCardTypes
    {
        /// <summary>
        /// Card type identifier for "AbCar": {D0006E40-A342-4797-8D77-6501C4B7C4AC}.
        /// </summary>
        public static readonly Guid AbCarTypeID = new(0xd0006e40, 0xa342, 0x4797, 0x8d, 0x77, 0x65, 0x01, 0xc4, 0xb7, 0xc4, 0xac);

        /// <summary>
        /// Card type name for "AbCar".
        /// </summary>
        public const string AbCarTypeName = "AbCar";

        /// <summary>
        /// Card type identifier for "AbExampleDialogSatellite": {7CFE67A4-0B8E-423B-8C15-8E2C584B429B}.
        /// </summary>
        public static readonly Guid AbExampleDialogSatelliteTypeID = new(0x7cfe67a4, 0x0b8e, 0x423b, 0x8c, 0x15, 0x8e, 0x2c, 0x58, 0x4b, 0x42, 0x9b);

        /// <summary>
        /// Card type name for "AbExampleDialogSatellite".
        /// </summary>
        public const string AbExampleDialogSatelliteTypeName = "AbExampleDialogSatellite";

        /// <summary>
        /// Card type identifier for "AbResolution": {21DAB866-F8D9-4027-9BBF-246A1D72AF47}.
        /// </summary>
        public static readonly Guid AbResolutionTypeID = new(0x21dab866, 0xf8d9, 0x4027, 0x9b, 0xbf, 0x24, 0x6a, 0x1d, 0x72, 0xaf, 0x47);

        /// <summary>
        /// Card type name for "AbResolution".
        /// </summary>
        public const string AbResolutionTypeName = "AbResolution";
    }
}
