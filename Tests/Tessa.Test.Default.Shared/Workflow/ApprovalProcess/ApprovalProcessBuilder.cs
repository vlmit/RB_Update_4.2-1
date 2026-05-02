#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;
using Tessa.Workflow.ApprovalProcess;
using Tessa.Workflow.ApprovalProcess.Nodes;
using AP = Tessa.Workflow.ApprovalProcess.ApprovalProcess;

namespace Tessa.Test.Default.Shared.Workflow.ApprovalProcess
{
    /// <summary>
    /// Объект для построения процесса согласования.
    /// </summary>
    public class ApprovalProcessBuilder
    {
        #region Fields

        private readonly Guid id;
        private string title;

        private readonly ApprovalProcessNode startNode;
        private readonly ApprovalProcessNode finishNode;
        private readonly List<ApprovalProcessNode> nodes = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт новый экземпляр класса.
        /// </summary>
        /// <param name="process">
        /// Процесс согласования, на базе которого идёт построение процесса, или null, если создаётся новый процесс.
        /// Процесс согласования должен быть целостным.
        /// </param>
        public ApprovalProcessBuilder(AP? process = null)
        {
            if (process is null)
            {
                this.id = Guid.NewGuid();
                this.title = "Test approval process";

                this.startNode =
                    new()
                    {
                        ID = Guid.NewGuid(),
                        Type = NodeTypes.Start,
                    };

                this.finishNode =
                    new()
                    {
                        ID = Guid.NewGuid(),
                        Type = NodeTypes.Finish,
                    };
            }
            else
            {
                this.id = process.ID;
                this.title = process.Title;
                this.startNode = process.Nodes.First(x => x.Type == NodeTypes.Start).DeepClone();
                this.finishNode = process.Nodes.First(x => x.Type == NodeTypes.Finish).DeepClone();

                var currentNode = this.startNode;
                var index = 0;
                while (process.Edges.TryFirst(x => x.Source == currentNode.ID, out var edge))
                {
                    var nextNode = process.Nodes.First(x => x.ID == edge.Target);
                    if (nextNode.ID == this.finishNode.ID) // Защита от цикла
                    {
                        break;
                    }

                    if (index++ > process.Edges.Count)
                    {
                        throw new ArgumentException($"Approval process is broken");
                    }

                    currentNode = nextNode;
                    this.nodes.Add(nextNode.DeepClone());
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает заголовок процесса согласования.
        /// </summary>
        /// <param name="title">Заголовок процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        public ApprovalProcessBuilder SetTitle(string title)
        {
            this.title = NotEmptyOrThrow(title);
            return this;
        }

        /// <summary>
        /// Возвращает заголовок процесса согласования.
        /// </summary>
        /// <returns>Заголовок процесса согласования.</returns>
        public string? GetTitle() => this.title;

        /// <summary>
        /// Добавляет следующий узел с указанными согласующими.
        /// </summary>
        /// <param name="nodeID">Идентификатор добавляемого узла.</param>
        /// <param name="text">Текст задания для согласующего.</param>
        /// <param name="duration">Срок выполнения задания.</param>
        /// <param name="approvers">Список согласующих. Не должен быть пустым.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        public ApprovalProcessBuilder AddNextNode(
            out Guid nodeID,
            string? text = null,
            double duration = 1,
            params Guid[] approvers)
        {
            ThrowIfZero(approvers.Length, nameof(approvers));

            var nodeData = new ApproverNodeData()
            {
                Duration = duration,
                Text = text,
                Approvers = approvers.Select(static x => new ApproverNodeApprover()
                {
                    ID = x,
                    SkipOnCurrentCycle = false,
                    Role = new ApproverNodeRole()
                    {
                        ID = x,
                        Name = x.ToString()
                    }
                }).ToList()
            };

            nodeID = Guid.NewGuid();
            this.nodes.Add(new()
            {
                ID = nodeID,
                Type = NodeTypes.Approval,
                Data = nodeData.ToSerializedDictionary(),
            });

            return this;
        }

        /// <summary>
        /// Изменяет параметры узла с указанным идентификатором.
        /// </summary>
        /// <param name="nodeID">Идентификатор узла.</param>
        /// <param name="text">Текст задания для согласующего.</param>
        /// <param name="duration">Срок выполнения задания.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        public ApprovalProcessBuilder ModifyNode(
            Guid nodeID,
            string? text = null,
            double duration = 1)
        {
            var node = this.nodes.First(x => x.ID == nodeID);
            var nodeData = NotNullOrThrow(node.Data.FromSerializedDictionary<ApproverNodeData>());

            nodeData.Text = text;
            nodeData.Duration = duration;

            node.Data = nodeData.ToSerializedDictionary();

            return this;
        }

        /// <summary>
        /// Удаляет узел с указанным идентификатором.
        /// </summary>
        /// <param name="nodeID">Идентификатор удаляемого узла.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        public ApprovalProcessBuilder RemoveNode(Guid nodeID)
        {
            this.nodes.RemoveAll(x => x.ID == nodeID);

            return this;
        }

        /// <summary>
        /// Изменяет данные согласующего узла.
        /// </summary>
        /// <param name="nodeID">Идентификатор узла.</param>
        /// <param name="approverID">Идентификатор согласующего.</param>
        /// <param name="withTaskInfo">
        /// Определяет, что у согласующего должна быть задана информация о задании.
        /// Если передано значение <c>null</c>, то информация о задании не обновляется.</param>
        /// <param name="completionState">
        /// Указывает состояние завершения задания согласующего.
        /// Применяется только, если в параметре <paramref name="withTaskInfo"/> передано значение <c>true</c>.
        /// </param>
        /// <param name="skipOnCurrentCycle">
        /// Определяет значение флага <see cref="ApproverNodeApprover.SkipOnCurrentCycle"/>.
        /// Если передано значение <c>null</c>, то значение флага не обновляется.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        public ApprovalProcessBuilder ModifyApprover(
            Guid nodeID,
            Guid approverID,
            bool? withTaskInfo = null,
            ApproverNodeCompletionState? completionState = null,
            bool? skipOnCurrentCycle = null)
        {
            var node = this.nodes.First(x => x.ID == nodeID);
            var nodeData = NotNullOrThrow(node.Data.FromSerializedDictionary<ApproverNodeData>());
            var approver = nodeData.Approvers.First(x => x.ID == approverID);

            if (withTaskInfo is true)
            {
                approver.TaskInfo ??= new ApproverNodeTaskInfo
                {
                    Cycle = 1,
                    Created = DateTime.UtcNow,
                    TaskID = Guid.NewGuid(),
                };

                approver.TaskInfo.CompletionInfo = completionState is null
                    ? null
                    : new ApproverNodeCompletionInfo
                    {
                        Comment = "Test",
                        CompletedBy = approver.Role,
                        Completed = DateTime.UtcNow,
                        CompletionState = completionState.Value,
                    };
            }
            else if (withTaskInfo is false)
            {
                approver.TaskInfo = null;
            }

            if (skipOnCurrentCycle is not null)
            {
                approver.SkipOnCurrentCycle = skipOnCurrentCycle.Value;
            }

            node.Data = nodeData.ToSerializedDictionary();

            return this;
        }

        /// <summary>
        /// Добавляет согласующих в узел.
        /// </summary>
        /// <param name="nodeID">Идентификатор узла.</param>
        /// <param name="approvers">Идентификаторы согласующих.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        public ApprovalProcessBuilder AddApprovers(
            Guid nodeID,
            params Guid[] approvers)
        {
            ThrowIfNull(approvers);

            var node = this.nodes.First(x => x.ID == nodeID);
            var nodeData = NotNullOrThrow(node.Data.FromSerializedDictionary<ApproverNodeData>());
            nodeData.Approvers.AddRange(
                approvers.Select(
                    static x => new ApproverNodeApprover()
                    {
                        ID = x,
                        SkipOnCurrentCycle = false,
                        Role = new ApproverNodeRole()
                        {
                            ID = x,
                            Name = x.ToString()
                        }
                    }));

            node.Data = nodeData.ToSerializedDictionary();

            return this;
        }

        /// <summary>
        /// Удаляет согласующих из узла.
        /// </summary>
        /// <param name="nodeID">Идентификатор узла.</param>
        /// <param name="approvers">Идентификаторы согласующих.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        public ApprovalProcessBuilder RemoveApprovers(
            Guid nodeID,
            params Guid[] approvers)
        {
            ThrowIfNull(approvers);

            var node = this.nodes.First(x => x.ID == nodeID);
            var nodeData = NotNullOrThrow(node.Data.FromSerializedDictionary<ApproverNodeData>());
            nodeData.Approvers.RemoveAll(x => approvers.Contains(x.ID));

            node.Data = nodeData.ToSerializedDictionary();

            return this;
        }

        /// <summary>
        /// Выполняет построение процесса согласования.
        /// </summary>
        /// <returns><inheritdoc cref="AP" path="/summary"/></returns>
        public AP Build()
        {
            var result = new AP()
            {
                Title = this.title,
                ID = this.id,
            };

            var currentNode = this.startNode;
            result.Nodes.Add(currentNode.DeepClone());

            foreach (var node in this.nodes)
            {
                result.Nodes.Add(node.DeepClone());
                result.Edges.Add(
                    new()
                    {
                        Source = currentNode.ID,
                        Target = node.ID,
                    });
                currentNode = node;
            }

            result.Nodes.Add(this.finishNode.DeepClone());
            result.Edges.Add(
                new()
                {
                    Source = currentNode.ID,
                    Target = this.finishNode.ID,
                });

            return result;
        }

        #endregion
    }
}
