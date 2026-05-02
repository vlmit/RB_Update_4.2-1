#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ActionHistory;

namespace Tessa.Test.Default.Shared.Platform.ActionHistory
{
    /// <summary>
    /// Стратегия работы с историей действий, которая ничего не выполняет.
    /// За счет неё снижается нагрузка на базу данных при запуске тестов.
    /// </summary>
    public sealed class FakeActionHistoryStrategy :
        IActionHistoryStrategy
    {
        public ValueTask DeleteAsync(Guid cardID) =>
            ValueTask.CompletedTask;

        public ValueTask InsertAsync(ActionHistoryRecord actionHistoryRecord)
        {
            actionHistoryRecord.RowID = Guid.NewGuid();
            return ValueTask.CompletedTask;
        }

        public ValueTask<ActionHistoryRecord?> TryGetAsync(Guid rowID, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(default(ActionHistoryRecord?));
    }
}
