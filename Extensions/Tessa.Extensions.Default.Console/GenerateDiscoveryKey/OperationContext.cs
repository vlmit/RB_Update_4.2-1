using System.IO;

namespace Tessa.Extensions.Default.Console.GenerateDiscoveryKey
{
    public class OperationContext
    {
        public OperationContext(
            string[] scopes,
            string password,
            string subject,
            TextWriter stdOut)
        {
            this.Scopes = NotNullOrThrow(scopes);
            this.Password = NotNullOrThrow(password);
            this.Subject = NotNullOrThrow(subject);
            this.StdOut = NotNullOrThrow(stdOut);
        }

        public string[] Scopes { get; }

        public string Password { get; }

        public string Subject { get; }

        public TextWriter StdOut { get; }

        public string? Parent { get; init; }

        public string? ParentPassword { get; init; }

        public string? Output { get; init; }

        public int ExpirationMonths { get; init; }

        public bool SelfSigned { get; init; }

        public Mode Mode { get; init; }
    }
}
