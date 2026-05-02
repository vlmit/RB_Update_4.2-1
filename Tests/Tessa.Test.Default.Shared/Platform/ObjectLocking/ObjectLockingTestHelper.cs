#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tessa.Platform.ObjectLocking;
using Tessa.Platform.Redis;

namespace Tessa.Test.Default.Shared.Platform.ObjectLocking
{
    /// <summary>
    /// Предоставляет методы для тестирования блокировок объектов.
    /// </summary>
    public static class ObjectLockingTestHelper
    {
        #region Nested Types

        /// <summary>
        /// Информация о блокировке.
        /// </summary>
        /// <param name="LockState"><inheritdoc cref="ObjectLockStates"/></param>
        /// <param name="ReadLockCount">Количество блокировок на чтение.</param>
        /// <param name="WriteLockCount">Количество блокировок на запись.</param>
        /// <param name="LockQueue">Описание очереди блокировок.</param>
        public record LockInfo(ObjectLockStates LockState, int ReadLockCount, int WriteLockCount, string LockQueue);

        #endregion
        
        #region Public Methods

        /// <summary>
        /// Возвращает информацию о блокировке объекта.
        /// </summary>
        /// <param name="serverCode">Код сервера.</param>
        /// <param name="redisConnectionProvider"><inheritdoc cref="IRedisConnectionProvider" path="/summary"/></param>
        /// <param name="objectID">Идентификатор объекта.</param>
        /// <param name="objectKind">Вид (разновидность) объектов.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Информация о блокировке.</returns>
        /// <remarks>Метод поддерживает работу только с блокировками, созданными с помощью <see cref="ObjectLockingStrategy"/>.</remarks>
        public static async Task<LockInfo> TryGetLockInfoAsync(
            string serverCode,
            IRedisConnectionProvider redisConnectionProvider,
            Guid objectID,
            string objectKind,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNullOrWhiteSpace(serverCode);
            ThrowIfNull(redisConnectionProvider);
            ThrowIfNullOrWhiteSpace(objectKind);

            var connection = await redisConnectionProvider.GetOpenedConnectionAsync(cancellationToken);
            var db = connection.GetDatabase();

            var locksKey = RedisHelper.GetObjectLockingKey(serverCode, objectKind);

            var entry = await db.HashGetAsync(locksKey, objectID.ToString());
            return entry.IsNullOrEmpty
                ? new(ObjectLockStates.None, 0, 0, "Empty")
                : GetLockStateDescription(entry!);
        }

        /// <summary>
        /// Проверяет наличие блокировки у объекта.
        /// </summary>
        /// <param name="serverCode">Код сервера.</param>
        /// <param name="redisConnectionProvider"><inheritdoc cref="IRedisConnectionProvider" path="/summary"/></param>
        /// <param name="objectID">Идентификатор объекта.</param>
        /// <param name="objectKind">Вид (разновидность) объектов.</param>
        /// <param name="expectedLockState">Ожидаемое состояние блокировки.</param>
        /// <param name="expectedReadLockCount">Ожидаемое число взятых блокировок на чтение или значение <see langword="null"/>, если валидация не требуется.</param>
        /// <param name="expectedWriteLockCount">Ожидаемое число взятых блокировок на запись или значение <see langword="null"/>, если валидация не требуется.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        /// <remarks>Метод поддерживает валидацию блокировок созданных только с помощью <see cref="ObjectLockingStrategy"/>.</remarks>
        public static async Task AssertObjectLockAsync(
            string serverCode,
            IRedisConnectionProvider redisConnectionProvider,
            Guid objectID,
            string objectKind,
            ObjectLockStates expectedLockState,
            int? expectedReadLockCount = null,
            int? expectedWriteLockCount = null,
            CancellationToken cancellationToken = default)
        {
            var (actualLockState, readLockCount, writeLockCount, lockQueue) = await TryGetLockInfoAsync(
                serverCode,
                redisConnectionProvider,
                objectID,
                objectKind,
                cancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(
                    actualLockState,
                    Is.EqualTo(expectedLockState),
                    GetExceptionMessage);

                if (expectedReadLockCount.HasValue)
                {
                    Assert.That(
                        readLockCount,
                        Is.EqualTo(expectedReadLockCount),
                        GetExceptionMessage);
                }

                if (expectedWriteLockCount.HasValue)
                {
                    Assert.That(
                        writeLockCount,
                        Is.EqualTo(expectedWriteLockCount),
                        GetExceptionMessage);
                }

                string GetExceptionMessage()
                {
                    return $"Lock state: {actualLockState}"
                        + $", Read lock count: {readLockCount}"
                        + $", Write lock count: {writeLockCount}"
                        + $", Lock queue: {lockQueue}";
                }
            });
        }

        #endregion

        #region Private Methods

        private static LockInfo GetLockStateDescription(
            string value)
        {
            var parts = value.Split(';');
            if (parts.Length != 2)
            {
                throw new FormatException($"Invalid format value. Lock state: \"{value}\".");
            }

            if (!int.TryParse(parts[0], out var lc))
            {
                throw new FormatException($"Invalid format LC value. Raw LC value: \"{parts[0]}\". Lock state: \"{value}\".");
            }

            if (!int.TryParse(parts[1], out var pc))
            {
                throw new FormatException($"Invalid format PC value. Raw PC value: \"{parts[1]}\". Lock state: \"{value}\".");
            }

            ObjectLockStates? lockState = null;
            var readLockCount = 0;
            var writeLockCount = 0;
            string? lockQueue = null;

            if (lc < 0)
            {
                lockState = ObjectLockStates.WriteLock;
                readLockCount = pc;
                writeLockCount = -lc;

                lockQueue = pc switch
                {
                    > 0 => $"Readers queue {pc}",
                    0 => "Empty",
                    _ => null
                };
            }
            else if (lc == 0)
            {
                if (pc == -1)
                {
                    lockState = ObjectLockStates.WritePending;
                    lockQueue = "Write Lock pending";
                }
            }
            else
            {
                lockState = ObjectLockStates.ReadLock;
                readLockCount = lc;

                lockQueue = pc switch
                {
                    -1 => "Write Lock pending",
                    0 => "Empty",
                    > 0 => $"Readers escalate queue of {pc} items",
                    _ => null
                };
            }

            if (lockState is null)
            {
                throw new FormatException($"Invalid lock state \"{value}\".");
            }

            if (lockQueue is null)
            {
                throw new FormatException($"Invalid queue of {pc} items. Lock state: \"{value}\".");
            }

            return new(lockState.Value, readLockCount, writeLockCount, lockQueue);
        }

        #endregion
    }
}
