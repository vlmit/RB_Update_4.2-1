#nullable enable

using System.Collections.Generic;
using System.Threading;

namespace Tessa.Test.Default.Shared.Notices
{
    public sealed class TestNotificationList
    {
        #region Fields

        private readonly List<TestNotificationListItem> items = [];

        private readonly Lock syncObject = new();

        #endregion

        #region Methods

        public void Add(TestNotificationListItem item)
        {
            ThrowIfNull(item);
            lock (this.syncObject)
            {
                this.items.Add(item);
            }
        }

        public IReadOnlyList<TestNotificationListItem> Get()
        {
            lock (this.syncObject)
            {
                return this.items.ToArray();
            }
        }

        public void Clear()
        {
            lock (this.syncObject)
            {
                this.items.Clear();
            }
        }

        #endregion
    }
}
