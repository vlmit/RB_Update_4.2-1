#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    public interface IOnlyOfficeSettings
    {
        string? ConverterUrl { get; }

        string? DocumentBuilderPath { get; }

        string? WebApiBasePath { get; }

        int TokenLifetimePeriod { get; }

        TimeSpan LoadTimeout { get; }
        
        /// <summary>
        /// Signature key for a tessa's jwt
        /// </summary>
        /// <value></value>
        byte[] SignatureKey { get; }
        
        /// <summary>
        /// Tessa's instance server code
        /// </summary>
        /// <value></value>
        string ServerCode { get; }

        /// <summary>
        /// Signature key for a OO/Р7 jwt
        /// </summary>
        /// <value></value>
        byte[]? OnlyOfficeSignatureKey { get; }
    }
}
