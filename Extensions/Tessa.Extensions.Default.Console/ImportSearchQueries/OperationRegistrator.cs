using Unity;

namespace Tessa.Extensions.Default.Console.ImportSearchQueries
{
    /// <summary>
    /// Регистратор для команды импорта поисковых запросов.
    /// </summary>
    [Registrator(Tag = RegistratorTag.ClientConsole)]
    public sealed class OperationRegistrator :
        RegistratorBase
    {
        /// <inheritdoc />
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<Operation>()
                ;
        }
    }
}
