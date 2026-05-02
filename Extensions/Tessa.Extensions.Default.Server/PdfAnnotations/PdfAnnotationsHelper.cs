#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.PdfAnnotations.Types;
using Tessa.Platform.Storage;
using System.Linq;
using Tessa.Platform.Collections;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public static class PdfAnnotationsHelper
    {
        /// <summary>
        /// Get pdf annotations from a request info.
        /// </summary>
        /// <param name="storage">Info.</param>
        /// <returns>Pdf annotations info</returns>
        public static PdfAnnotationsInfo? GetPdfAnnotationsInfo(CardInfoStorageObject storage) =>
            storage.Info.GetSerializedObject<PdfAnnotationsInfo>(PdfAnnotationsKeys.PdfAnnotationsKey);

        /// <summary>
        /// Get pdf annotations bar from a request info.
        /// </summary>
        /// <param name="storage">Info.</param>
        /// <returns>Pdf annotations info</returns>
        public static StampsBarInfo? GetStampsBarInfo(CardInfoStorageObject storage) =>
            storage.Info.GetSerializedObject<StampsBarInfo>(PdfAnnotationsKeys.PdfAnnotationsKey);

        /// <summary>
        /// Get pdf annotations qr from a request info.
        /// </summary>
        /// <param name="storage">Info.</param>
        /// <returns>Pdf annotations info</returns>
        public static StampsQRInfo? GetStampsQRInfo(CardInfoStorageObject storage) =>
            storage.Info.GetSerializedObject<StampsQRInfo>(PdfAnnotationsKeys.PdfAnnotationsKey);

        /// <summary>
        /// Get pdf annotations export png from a request info.
        /// </summary>
        /// <param name="storage">Info.</param>
        /// <returns>Pdf annotations info</returns>
        public static ExportInfo? GetExportInfo(CardInfoStorageObject storage) =>
            storage.Info.GetSerializedObject<ExportInfo>(PdfAnnotationsKeys.PdfAnnotationsKey);

        /// <summary>
        /// Get pdf annotations add stamp from a request info.
        /// </summary>
        /// <param name="storage">Info.</param>
        /// <returns>Pdf annotations info</returns>
        public static AddStampInfo? GetAddStampInfo(CardInfoStorageObject storage) =>
            storage.Info.GetSerializedObject<AddStampInfo>(PdfAnnotationsKeys.PdfAnnotationsKey);

        /// <summary>
        /// Get fileID-OldVersionID pairs for new versions of files that were installed by selecting old versions as current.
        /// </summary>
        /// <param name="storage">Info.</param>
        /// <returns>Dictionary of fileID-OldVersionID pairs/</returns>
        public static Dictionary<Guid, Guid>? GetOldNewVersionIDs(CardInfoStorageObject storage) =>
            storage.Info.TryGet<Dictionary<string, object>>(CardHelper.OldNewVersionIDKey)?.Aggregate(
                new Dictionary<Guid, Guid>(),
                (acc, x) =>
                {
                    acc[Guid.Parse(x.Key)] = (Guid)x.Value;
                    return acc;
                });

        /// <summary>
        /// Merge annotations list with a receiver update
        /// </summary>
        /// <param name="existed">Receiver annotaitons.</param>
        /// <param name="newAnns">Other annotations.</param>
        public static void MergeAnnotations(this IList<Annotation>? existed, IList<Annotation>? newAnns)
        {
            if (existed is null || newAnns is null)
            {
                return;
            }

            foreach (var ann in newAnns)
            {
                if (ann.State == AnnotationState.Inserted)
                {
                    existed.Add(ann);
                    continue;
                }

                var foundIndex = existed.IndexOf(x => x.ID.Equals(ann.ID));
                if (foundIndex == -1)
                {
                    continue;
                }

                if (ann.State == AnnotationState.Deleted)
                {
                    existed[foundIndex] = ann;
                    continue;
                }

                if (ann.State == AnnotationState.Modified || ann.State == AnnotationState.Stored)
                {
                    var founded = existed[foundIndex];
                    var mergedComments = new List<CommentAnnotation>(founded.Comments);
                    var mergedStatuses = new List<StatusComment>(founded.Statuses);

                    foreach (var comment in ann.Comments)
                    {
                        if (comment.State == AnnotationState.Inserted)
                        {
                            mergedComments.Add(comment);
                            continue;
                        }

                        var foundCommentIndex = mergedComments.IndexOf(x => x.ID.Equals(comment.ID));
                        if (foundCommentIndex == -1)
                        {
                            continue;
                        }

                        if (comment.State == AnnotationState.Deleted)
                        {
                            mergedComments[foundIndex] = comment;
                            continue;
                        }

                        if (comment.State == AnnotationState.Modified || comment.State == AnnotationState.Stored)
                        {
                            var foundedComment = mergedComments[foundCommentIndex];
                            var mergedCommentsStatuses = new List<StatusComment>(foundedComment.StatusesComments);

                            foreach (var statusObj in comment.StatusesComments)
                            {
                                var foundIndex_ = mergedCommentsStatuses.IndexOf(x => x.UserID?.Equals(statusObj.UserID, StringComparison.OrdinalIgnoreCase) ?? false);
                                if (foundIndex_ == -1)
                                {
                                    mergedCommentsStatuses.Add(statusObj);
                                    continue;
                                }

                                var found = mergedCommentsStatuses[foundIndex_];
                                foreach (var status in statusObj.Statuses)
                                {
                                    if (status.State != AnnotationState.Stored)
                                    {
                                        found.Statuses = [.. found.Statuses, status];
                                    }
                                }
                            }

                            if (comment.State == AnnotationState.Modified)
                            {
                                mergedComments[foundCommentIndex] = comment;
                            }

                            mergedComments[foundCommentIndex].StatusesComments = mergedCommentsStatuses;
                        }
                    }

                    foreach (var statusObj in ann.Statuses)
                    {
                        var foundIndex_ = mergedStatuses.IndexOf(x => x.UserID?.Equals(statusObj.UserID, StringComparison.OrdinalIgnoreCase) ?? false);
                        if (foundIndex_ == -1)
                        {
                            mergedStatuses.Add(statusObj);
                            continue;
                        }

                        var found = mergedStatuses[foundIndex_];
                        foreach (var status in statusObj.Statuses)
                        {
                            if (status.State != AnnotationState.Stored)
                            {
                                found.Statuses = [.. found.Statuses, status];
                            }
                        }
                    }

                    if (ann.State == AnnotationState.Modified)
                    {
                        existed[foundIndex] = ann;
                    }

                    existed[foundIndex].Comments = mergedComments;
                    existed[foundIndex].Statuses = mergedStatuses;
                }
            }
        }

        /// <summary>
        /// Checks that annotations is modified/inserted/deleted by user.
        /// </summary>
        /// <param name="info">Info.</param>
        /// <param name="modifiedByID">User.</param>
        /// <returns>True if valid</returns>
        public static bool IsValidAnnotations(this PdfAnnotationsData info, Guid modifiedByID)
        {
            if (info.Annotations is null)
            {
                return false;
            }

            bool valid = true;
            foreach (var ann in info.Annotations)
            {
                if (!IsValidInitialized(ann) || (IsModified(ann) && ann.UserID != modifiedByID && !IsImported(ann)))
                {
                    valid = false;
                    break;
                }

                foreach (var comment in ann.Comments)
                {
                    if (IsModified(comment) && ((comment.UserID != modifiedByID && !IsImported(ann)) || comment.State == AnnotationState.Deleted))
                    {
                        valid = false;
                        break;
                    }

                    foreach (var status in comment.StatusesComments.Select(x => x.Statuses).SelectMany(x => x))
                    {
                        if (IsModified(status) && ((status.UserID != modifiedByID && !IsImported(ann)) || status.State == AnnotationState.Deleted))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (!valid)
                    {
                        break;
                    }
                }

                if (!valid)
                {
                    break;
                }

                foreach (var status in ann.Statuses.Select(x => x.Statuses).SelectMany(x => x))
                {
                    if (IsModified(status) && ((status.UserID != modifiedByID && !IsImported(ann)) || status.State == AnnotationState.Deleted))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    break;
                }
            }

            return valid;
        }

        /// <summary>
        /// Verifies that an object is properly initialized
        /// </summary>
        /// <param name="ann">Annotation</param>
        /// <returns>True if valid</returns>
        private static bool IsValidInitialized(Annotation ann)
        {
            return Enum.IsDefined(ann.State)
                    && Enum.IsDefined(ann.AnnClass)
                    && ann.UserID is not null
                    && ann.Date is not null;
        }

        /// <summary>
        /// Change states to Stored, Remove Deleted.
        /// </summary>
        /// <param name="annotations">Annotations.</param>
        /// <returns>Prepared annotations.</returns>
        public static IList<Annotation>? PrepareForSaving(this IList<Annotation>? annotations)
        {
            if (annotations is null)
            {
                return annotations;
            }

            var anns = annotations
                .Where(x => x.State != AnnotationState.Deleted)
                .ToList();

            foreach (var ann in anns)
            {
                ann.State = AnnotationState.Stored;
                ann.IsImported = null;

                ann.Comments = [.. ann.Comments.Where(x => x.State != AnnotationState.Deleted)];
                foreach (var comment in ann.Comments)
                {
                    comment.State = AnnotationState.Stored;

                    foreach (
                        var statusComment in comment.StatusesComments.Select(x => x.Statuses).SelectMany(x => x))
                    {
                        statusComment.State = AnnotationState.Stored;
                    }
                }

                foreach (
                    var status in ann.Statuses.Select(x => x.Statuses).SelectMany(x => x))
                {
                    status.State = AnnotationState.Stored;
                }
            }

            return anns;
        }

        /// <summary>
        /// Returns an array of arrays of a target type.
        /// </summary>
        /// <param name="storage">Storage.</param>
        /// <param name="key">Key.</param>
        /// <returns>An array of arrays of a target type.</returns>
        public static IEnumerable<List<T>>? GetListInList<T>(this Dictionary<string, object?> storage, string key)
        {
            storage.TryGetValue(key, out object? obj);
            IEnumerable<List<T>>? s = null;
            if (obj is List<object> listObj)
            {
                if (listObj is null || listObj.Count == 0)
                {
                    s = [];
                }
                else
                {
                    s = listObj.Cast<List<T>>();
                }
            }
            else if (obj is List<List<T>> listListObj)
            {
                s = listListObj;
            }

            return s;
        }

        #region private

        private static bool IsModified(AnnotationBase x) =>
            x.State == AnnotationState.Deleted ||
            x.State == AnnotationState.Inserted ||
            x.State == AnnotationState.Modified;

        private static bool IsImported(AnnotationBase x) =>
            x.IsImported == true && x.State == AnnotationState.Inserted;

        #endregion
    }
}

