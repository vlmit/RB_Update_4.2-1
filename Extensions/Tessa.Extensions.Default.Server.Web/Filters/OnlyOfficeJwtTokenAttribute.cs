using System;

namespace Tessa.Extensions.Default.Server.Web.Filters
{
    /// <summary>
    /// Указывает, что параметр типа <see cref="string"/> содержит JWT-токен для авторизации OnlyOffice.
    /// </summary>
    /// <remarks>
    /// Атрибут следует задать для параметра метода контроллера в ASP.NET Core, который используется
    /// совместно с авторизацией посредством <see cref="OnlyOfficeJwtAuthorizationAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class OnlyOfficeJwtTokenAttribute :
        Attribute
    {
    }
}
