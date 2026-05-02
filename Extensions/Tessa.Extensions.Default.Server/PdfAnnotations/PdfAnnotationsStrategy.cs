#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    /// <inheritdoc cref="IPdfAnnotationsStrategy"/>
    public class PdfAnnotationsStrategy :
        IPdfAnnotationsStrategy
    {
        #region Fields

        private readonly IDbScope dbScope;

        private static readonly string[] pdfAnnotationsTableColumns =
        {
            "ID",
            "CardID",
            "FileID",
            "FileVersionRowID",
            "Annotations",
            "Version",
            "ModifiedByID",
            "Modified"
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="PdfAnnotationsStrategy"/>.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        public PdfAnnotationsStrategy(IDbScope dbScope) =>
            this.dbScope = NotNullOrThrow(dbScope);

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public async Task<IList<PdfAnnotationsData>> TryGetInfoAsync(Card card, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);

            var fileVersionRowIDs = card
                .TryGetFiles()
                ?.Select(f => f.VersionRowID)
                .ToArray();

            if (fileVersionRowIDs is not { Length: > 0 })
            {
                return new List<PdfAnnotationsData>(0);
            }

            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;

            var command = this.dbScope.BuilderFactory
                .Select().C("pa", pdfAnnotationsTableColumns)
                .From("PdfAnnotations", "pa").NoLock()
                .Where().C("FileVersionRowID").InArray(fileVersionRowIDs, "RowIDs", out var rowIDsParameter)
                .Build();

            db
                .SetCommand(command, DataParameters.Get(rowIDsParameter))
                .LogCommand();

            return await GetResults(db, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<PdfAnnotationsData?> TryGetInfoAsync(PdfAnnotationsData info, CancellationToken cancellationToken = default)
        {
            var result = await this.TryGetInfoAsync(new[] { info }, cancellationToken);

            return result.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IList<PdfAnnotationsData>> TryGetInfoAsync(IList<PdfAnnotationsData> infos, CancellationToken cancellationToken = default)
        {
            var result = new List<PdfAnnotationsData>();

            if (infos.Count == 0)
            {
                return result;
            }

            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;

            var commandBuilder = this.dbScope.BuilderFactory.Create();

            var ids = new List<Guid>();
            var fileVersionRowIDs = new List<Guid>();

            foreach (var info in infos)
            {
                if (info.ID.HasValue)
                {
                    ids.Add(info.ID.Value);
                }
                else
                {
                    fileVersionRowIDs.Add(info.FileVersionRowID);
                }
            }

            commandBuilder
                .Select().C("pa", pdfAnnotationsTableColumns)
                .From("PdfAnnotations", "pa").NoLock()
                .InnerJoin().Table("FileVersions", "fv").NoLock()
                .On().C("pa", "FileVersionRowID").Equals().C("fv", "RowID")
                .Where();

            LinqToDB.Data.DataParameter? dpVersionIDs;

            if (ids.Count > 0 && fileVersionRowIDs.Count > 0)
            {
                throw new InvalidOperationException("You have to use ID or FileVersionRowID for all array elements");
            }
            else if (ids.Count > 0)
            {
                commandBuilder
                    .C("pa", "ID").InArray(ids, "Ids", out dpVersionIDs);
            }
            else
            {
                commandBuilder
                    .C("pa", "FileVersionRowID").InArray(fileVersionRowIDs, "Ids", out dpVersionIDs);
            }

            var command = commandBuilder.Build();

            db
                .SetCommand(command, DataParameters.Get(dpVersionIDs))
                .LogCommand();

            return await GetResults(db, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<PdfAnnotationsData> RequestMergedAnnsBeforeStoreAsync(PdfAnnotationsData info, Guid modifiedByID, CancellationToken cancellationToken = default)
        {
            var existedInfo = await this.TryGetInfoAsync(info, cancellationToken);

            if (!info.IsValidAnnotations(modifiedByID) ||
                existedInfo is not null &&
                    info.Annotations is not null &&
                        info.Annotations.Any(x => x.IsImported is not null))
            {
                throw new InvalidOperationException("Users can only edit their own annotations.");
            }

            if (existedInfo is null)
            {
                existedInfo = new PdfAnnotationsData
                {
                    ID = info.ID,
                    CardID = info.CardID,
                    FileID = info.FileID,
                    FileVersionRowID = info.FileVersionRowID,
                    Modified = info.Modified,
                    ModifiedByID = info.ModifiedByID,
                    Version = info.Version,
                    Annotations = new List<Types.Annotation>()
                };
            }

            existedInfo.Annotations.MergeAnnotations(info.Annotations);

            info.Annotations = existedInfo.Annotations;

            return info;
        }

        /// <inheritdoc/>
        public async Task<Guid> StoreAsync(PdfAnnotationsData info, Guid modifiedByID, CancellationToken cancellationToken = default)
        {
            if (!info.IsValidAnnotations(modifiedByID))
            {
                throw new InvalidOperationException("Users can only edit their own annotations.");
            }

            await using var _ = this.dbScope.Create();

            IQueryExecutor executor = this.dbScope.Executor;

            var existedInfo = await this.TryGetInfoAsync(info, cancellationToken);

            var annotations = info.Annotations.PrepareForSaving();

            Guid id = existedInfo?.ID ?? info.ID ?? Guid.NewGuid();

            if (existedInfo is null)
            {
                var version = 1;
                await executor.ExecuteNonQueryAsync(
                    this.dbScope.BuilderFactory
                        .InsertInto("PdfAnnotations", pdfAnnotationsTableColumns)
                        .Values(b => b.P(pdfAnnotationsTableColumns))
                        .Build(),
                    cancellationToken,
                    executor.Parameter("ID", id, DataType.Guid),
                    executor.Parameter("CardID", info.CardID, DataType.Guid),
                    executor.Parameter("FileID", info.FileID, DataType.Guid),
                    executor.Parameter("FileVersionRowID", info.FileVersionRowID, DataType.Guid),
                    executor.Parameter("Annotations",
                        StorageHelper.SerializeToTypedJson(
                            annotations?.Select(x => x.ToSerializedDictionary()).ToList() ??
                                Enumerable.Empty<Dictionary<string, object?>>().ToList()),
                         DataType.BinaryJson),
                    executor.Parameter("Version", version, DataType.Int32),
                    executor.Parameter("ModifiedByID", modifiedByID, DataType.Guid),
                    executor.Parameter("Modified", DateTime.UtcNow, DataType.DateTime)
                    );
            }
            else
            {
                var version = info.Version + 1;
                await executor.ExecuteNonQueryAsync(
                    this.dbScope.BuilderFactory
                        .Update("PdfAnnotations")
                            .C("FileVersionRowID").Assign().P("FileVersionRowID")
                            .C("Annotations").Assign().P("Annotations")
                            .C("Version").Assign().P("Version")
                            .C("ModifiedByID").Assign().P("ModifiedByID")
                            .C("Modified").Assign().P("Modified")
                        .Where().C("ID").Equals().P("ID")
                        .Build(),
                    cancellationToken,
                    executor.Parameter("ID", id, DataType.Guid),
                    executor.Parameter("FileVersionRowID", info.FileVersionRowID, DataType.Guid),
                    executor.Parameter("Annotations",
                        StorageHelper.SerializeToTypedJson(
                            annotations?.Select(x => x.ToSerializedDictionary()).ToList() ??
                                Enumerable.Empty<Dictionary<string, object?>>().ToList()),
                         DataType.BinaryJson),
                    executor.Parameter("Version", version, DataType.Int32),
                    executor.Parameter("ModifiedByID", modifiedByID, DataType.Guid),
                    executor.Parameter("Modified", DateTime.UtcNow, DataType.DateTime));
            }

            return id;
        }

        /// <inheritdoc/>
        public async Task DeleteCardsFilesAnnotationsAsync(IList<Guid> cardIDs, bool withBackup = false, CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();

            var builder = this.dbScope.BuilderFactory.Create();

            if (withBackup)
            {
                builder
                    .InsertInto("DeletedPdfAnnotations", pdfAnnotationsTableColumns)
                    .Select().C("pa", pdfAnnotationsTableColumns)
                    .From("PdfAnnotations", "pa").NoLock()
                    .Where().C("pa", "CardID").In(cardIDs.ToArray())
                    .Z();
            }
            else
            {
                builder
                    .DeleteFrom("DeletedPdfAnnotations")
                    .Where().C("CardID").In(cardIDs.ToArray())
                    .Z();
            }

            await this.dbScope.Executor.ExecuteNonQueryAsync(
                builder
                    .DeleteFrom("PdfAnnotations")
                    .Where().C("CardID").In(cardIDs.ToArray())
                    .Build(),
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RestoreCardsFilesAnnotationsAsync(IList<Guid> cardIDs, CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();

            var builder = this.dbScope.BuilderFactory
                .InsertInto("PdfAnnotations", pdfAnnotationsTableColumns)
                .Select().C("pa", pdfAnnotationsTableColumns)
                .From("DeletedPdfAnnotations", "pa").NoLock()
                .Where().C("pa", "CardID").In(cardIDs.ToArray())
                .Z();

            await this.dbScope.Executor.ExecuteNonQueryAsync(
                builder
                    .DeleteFrom("DeletedPdfAnnotations")
                    .Where().C("CardID").In(cardIDs.ToArray())
                    .Build(),
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteFileAnnotationsAsync(IList<Guid> fileIDs, bool withBackup = false, CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();

            var builder = this.dbScope.BuilderFactory.Create();

            if (withBackup)
            {
                builder
                    .InsertInto("DeletedPdfAnnotations", pdfAnnotationsTableColumns)
                    .Select().C("pa", pdfAnnotationsTableColumns)
                    .From("PdfAnnotations", "pa")
                    .Where().C("pa", "FileID").In(fileIDs.ToArray())
                    .Z();
            }
            else
            {
                builder
                    .DeleteFrom("DeletedPdfAnnotations")
                    .Where().C("FileID").In(fileIDs.ToArray())
                    .Z();
            }

            await this.dbScope.Executor.ExecuteNonQueryAsync(
                builder
                    .DeleteFrom("PdfAnnotations")
                    .Where().C("FileID").In(fileIDs.ToArray())
                    .Build(),
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RestoreFileAnnotationsAsync(IList<Guid> fileIDs, CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();

            var builder = this.dbScope.BuilderFactory
                .InsertInto("PdfAnnotations", pdfAnnotationsTableColumns)
                .Select().C("pa", pdfAnnotationsTableColumns)
                .From("DeletedPdfAnnotations", "pa").NoLock()
                .Where().C("pa", "FileID").In(fileIDs.ToArray())
                .Z();

            await this.dbScope.Executor.ExecuteNonQueryAsync(
                builder
                    .DeleteFrom("DeletedPdfAnnotations")
                    .Where().C("FileID").In(fileIDs.ToArray())
                    .Build(),
                cancellationToken);
        }

        #endregion

        #region Private Methods

        private static async Task<IList<PdfAnnotationsData>> GetResults(DbManager db, CancellationToken cancellationToken = default)
        {
            var result = new List<PdfAnnotationsData>();

            await using (var reader = await db.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(new PdfAnnotationsData
                    {
                        ID = reader.GetGuid(0),
                        CardID = reader.GetGuid(1),
                        FileID = reader.GetGuid(2),
                        FileVersionRowID = reader.GetGuid(3),
                        Annotations = PdfAnnotationsData
                            .GetAnnotations(
                                StorageHelper.DeserializeListFromTypedJson(
                                    await reader.GetSequentialStringAsync(4, db.Dbms, cancellationToken))),
                        Version = reader.GetInt32(5),
                        ModifiedByID = reader.GetGuid(6),
                        Modified = reader.GetDateTimeUtc(7)
                    });
                }
            }

            return result;
        }

        #endregion
    }
}
