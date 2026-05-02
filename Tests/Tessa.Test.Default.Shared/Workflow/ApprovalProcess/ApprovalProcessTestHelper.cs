#nullable enable

using System;
using System.Linq;
using NUnit.Framework;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.ApprovalProcess;
using Tessa.Workflow.ApprovalProcess.Nodes;
using AP = Tessa.Workflow.ApprovalProcess.ApprovalProcess;

namespace Tessa.Test.Default.Shared.Workflow.ApprovalProcess
{
    /// <summary>
    /// Вспомогательные методы для тестирования процессов согласования.
    /// </summary>
    public static class ApprovalProcessTestHelper
    {
        #region Helper Methods

        /// <summary>
        /// Создаёт тестовый экземпляр процесса согласования со случайными параметрами.
        /// </summary>
        /// <param name="userID">Идентификатор сотрудника, который создаёт процесс согласования.</param>
        /// <returns><inheritdoc cref="ApprovalProcessInstance" path="/summary"/></returns>
        public static ApprovalProcessInstance CreateTestInstance(
            Guid userID)
        {
            return CreateTestInstance(userID, withRandomWorkflowData: true);
        }

        /// <summary>
        /// Создаёт тестовый экземпляр процесса согласования с заданными параметрами.
        /// </summary>
        /// <param name="userID">Идентификатор сотрудника, который создаёт процесс согласования.</param>
        /// <param name="cardID">Идентификатор карточки, к которой относится процесс согласования, или <c>null</c>, если используется случайный идентификатор..</param>
        /// <param name="process">Процесс согласования или <c>null</c>, если он создаётся случайным образом.</param>
        /// <param name="settings">Настройки процесса согласования или <c>null</c>, если они задаются случайным образом.</param>
        /// <param name="withRandomWorkflowData">Определяет, что настройки внешних бизнес-процессов будут созданы случайным образом.</param>
        /// <returns><inheritdoc cref="ApprovalProcessInstance" path="/summary"/></returns>
        public static ApprovalProcessInstance CreateTestInstance(
            Guid userID,
            Guid? cardID = null,
            AP? process = null,
            ApprovalProcessInstanceSettings? settings = null,
            bool withRandomWorkflowData = false)
        {
            if (process is null)
            {
                var approvalProcessBuilder = new ApprovalProcessBuilder()
                    .SetTitle($"Test process {TestHelper.GetPseudoRandomNumber()}");

                var nodesCount = TestHelper.GetPseudoRandomNumber(2, 5);
                for (var i = 0; i < nodesCount; i++)
                {
                    approvalProcessBuilder
                        .AddNextNode(
                            out _,
                            $"Test text {TestHelper.GetPseudoRandomNumber()}",
                            TestHelper.GetPseudoRandomNumber(1, 10),
                            Enumerable.Range(0, TestHelper.GetPseudoRandomNumber(1, 5)).Select(x => Guid.NewGuid()).ToArray());
                }

                process = approvalProcessBuilder.Build();
            }

            return new ApprovalProcessInstance()
            {
                CardID = cardID ?? Guid.NewGuid(),
                CreatedByID = userID,
                ExternalProcessID = withRandomWorkflowData ? Guid.NewGuid() : null,
                ExternalStartID = withRandomWorkflowData ? Guid.NewGuid() : null,
                ExternalWorkflowType = withRandomWorkflowData ? $"Test workflow type {TestHelper.GetPseudoRandomNumber()}" : null,
                ID = process.ID,
                IsNew = true,
                Settings = settings ?? new ApprovalProcessInstanceSettings()
                {
                    ChangeStateOnEnd = TestHelper.GetPseudoRandomNumber(0, 2) == 0,
                    ChangeStateOnStart = TestHelper.GetPseudoRandomNumber(0, 2) == 0,
                    Cycle = TestHelper.GetPseudoRandomNumber(1, 10),
                    HistoryGroupID = Guid.NewGuid(),
                    ReturnAfterDisapproval = TestHelper.GetPseudoRandomNumber(0, 2) == 0,
                    ShowRevokeButton = TestHelper.GetPseudoRandomNumber(0, 2) == 0,
                },
                Process = process,
            };
        }

        /// <summary>
        /// Создаёт экземпляр узла процесса согласования из узла процесса согласования.
        /// </summary>
        /// <param name="node"><inheritdoc cref="ApprovalProcessNode" path="/summary"/></param>
        /// <param name="processInstanceID">Идентификатор экземпляра процесса согласования.</param>
        /// <returns><inheritdoc cref="ApprovalProcessNodeInstance" path="/summary"/></returns>
        public static ApprovalProcessNodeInstance CreateNodeInstanceFromNode(ApprovalProcessNode node, Guid processInstanceID)
        {
            return new ApprovalProcessNodeInstance()
            {
                ID = Guid.NewGuid(),
                IsNew = true,
                NodeID = node.ID,
                Type = node.Type,
                Data = node.Data,
                ProcessID = processInstanceID,
            };
        }

        /// <summary>
        /// Создаёт объект с информацией о задании из экземпляра узла процесса согласования.
        /// </summary>
        /// <param name="nodeInstance"><inheritdoc cref="ApprovalProcessNodeInstance" path="/summary"/></param>
        /// <param name="taskID">Идентификатор задания или <c>null</c>, если он задаётся случайным идентификатором.</param>
        /// <returns><inheritdoc cref="ApprovalProcessTaskInfo" path="/summary"/></returns>
        public static ApprovalProcessTaskInfo CreateTaskInfoFromNodeInstance(ApprovalProcessNodeInstance nodeInstance, Guid? taskID = null)
        {
            return new ApprovalProcessTaskInfo
            {
                ID = Guid.NewGuid(),
                TaskID = taskID ?? Guid.NewGuid(),
                NodeID = nodeInstance.ID,
            };
        }

        /// <summary>
        /// Создаёт экземпляр запроса на обработку процесса согласования.
        /// </summary>
        /// <param name="testInstance">Экземпляр процесса согласования.</param>
        /// <returns><inheritdoc cref="ApprovalProcessExecutionRequest" path="/summary"/></returns>
        public static ApprovalProcessExecutionRequest CreateApprovalProcessRequest(
            ApprovalProcessInstance testInstance)
        {
            return new ApprovalProcessExecutionRequest
            {
                CardID = testInstance.CardID,
                Instance = testInstance,
                InstanceID = testInstance.ID,
                ValidationResult = new ValidationResultBuilder(),
                ExecutionDateTime = DateTime.UtcNow,
            };
        }

        #endregion

        #region Assert Methods

        /// <summary>
        /// Метод для валидации процесса согласования.
        /// </summary>
        /// <param name="actualResult">Проверяемый процесс согласования.</param>
        /// <param name="expectedResult">Ожидаемый процесс согласования.</param>
        public static void AssertApprovalProcess(
            AP actualResult,
            AP expectedResult)
        {
            ThrowIfNull(actualResult);
            ThrowIfNull(expectedResult);

            // Process Properties assert
            Assert.That(actualResult.Title, Is.EqualTo(expectedResult.Title), $"Actual result process has wrong {nameof(actualResult.Title)}");

            // Nodes assert
            Assert.That(actualResult.Nodes.Count, Is.EqualTo(expectedResult.Nodes.Count), $"Actual result process has wrong {nameof(actualResult.Nodes)}.{nameof(actualResult.Nodes.Count)}");

            foreach (var expectedNode in expectedResult.Nodes)
            {
                var actualNode = actualResult.Nodes.FirstOrDefault(x => x.ID == expectedNode.ID);
                Assert.That(actualNode, Is.Not.Null, $"Actual result process has no node with ID=\"{expectedNode.ID}\"");
                Assert.That(actualNode!.Type, Is.EqualTo(expectedNode.Type), $"Actual result node with ID=\"{actualNode.ID}\" has wrong type.");

                if (actualNode.Type == NodeTypes.Approval)
                {
                    var actualNodeData = actualNode.Data.FromSerializedDictionary<ApproverNodeData>();
                    var expectedNodeData = expectedNode.Data.FromSerializedDictionary<ApproverNodeData>();

                    Assert.That(actualNodeData, Is.Not.Null, $"Actual result node with ID=\"{actualNode.ID}\" has no data.");
                    Assert.That(expectedNodeData, Is.Not.Null, $"Expected result node with ID=\"{actualNode.ID}\" has no data.");
                    Assert.That(actualNodeData!.Duration, Is.EqualTo(expectedNodeData!.Duration), $"Actual result node with ID=\"{actualNode.ID}\" has wrong {nameof(ApproverNodeData.Duration)}.");
                    Assert.That(actualNodeData.Text, Is.EqualTo(expectedNodeData.Text), $"Actual result node with ID=\"{actualNode.ID}\" has wrong {nameof(ApproverNodeData.Text)}.");
                    Assert.That(actualNodeData.Approvers, Is.EquivalentTo(expectedNodeData.Approvers), $"Actual result node with ID=\"{actualNode.ID}\" has wrong {nameof(ApproverNodeData.Approvers)}.");
                }
            }

            // Edges assert
            Assert.That(actualResult.Edges.Count, Is.EqualTo(expectedResult.Edges.Count), $"Actual result process has wrong {nameof(actualResult.Edges)}.{nameof(actualResult.Edges.Count)}");

            foreach (var expectedEdge in expectedResult.Edges)
            {
                var actualEdge = actualResult.Edges.FirstOrDefault(x => x.Source == expectedEdge.Source && x.Target == expectedEdge.Target);
                Assert.That(actualEdge, Is.Not.Null, $"Actual result process has no edge with Target=\"{expectedEdge.Target}\" and Source=\"{expectedEdge.Source}\"");
            }
        }

        /// <summary>
        /// Метод для валидации настроек шаблона процесса согласования.
        /// </summary>
        /// <param name="actualResult">Проверяемые настройки шаблона процесса согласования.</param>
        /// <param name="expectedResult">Ожидаемый настройки шаблона процесса согласования.</param>
        public static void AssertApprovalProcessTemplateSettings(
            ApprovalProcessTemplateSettings actualResult,
            ApprovalProcessTemplateSettings expectedResult)
        {
            ThrowIfNull(actualResult);
            ThrowIfNull(expectedResult);

            Assert.That(actualResult.Description, Is.EqualTo(expectedResult.Description), $"Actual result has wrong {nameof(actualResult.Description)}.");
            Assert.That(actualResult.Types, Is.EquivalentTo(expectedResult.Types), $"Actual result has wrong {nameof(actualResult.Types)}.");
            Assert.That(actualResult.Users, Is.EquivalentTo(expectedResult.Users), $"Actual result has wrong {nameof(actualResult.Users)}.");
            Assert.That(actualResult.Writers, Is.EquivalentTo(expectedResult.Writers), $"Actual result has wrong {nameof(actualResult.Writers)}.");
        }

        /// <summary>
        /// Метод для валидации экземпляра процесса согласования.
        /// </summary>
        /// <param name="actualResult">Проверяемый экземпляр процесса согласования.</param>
        /// <param name="expectedResult">Ожидаемый экземпляр процесса согласования.</param>
        public static void AssertApprovalProcessInstance(
            ApprovalProcessInstance actualResult,
            ApprovalProcessInstance expectedResult)
        {
            ThrowIfNull(actualResult);
            ThrowIfNull(expectedResult);

            // Properties assert
            Assert.That(actualResult.CardID, Is.EqualTo(expectedResult.CardID), $"Actual result instance has wrong {nameof(actualResult.CardID)}");
            Assert.That(actualResult.CreatedByID, Is.EqualTo(expectedResult.CreatedByID), $"Actual result instance has wrong {nameof(actualResult.CreatedByID)}");
            Assert.That(actualResult.ExternalProcessID, Is.EqualTo(expectedResult.ExternalProcessID), $"Actual result instance has wrong {nameof(actualResult.ExternalProcessID)}");
            Assert.That(actualResult.ExternalStartID, Is.EqualTo(expectedResult.ExternalStartID), $"Actual result instance has wrong {nameof(actualResult.ExternalStartID)}");
            Assert.That(actualResult.ExternalWorkflowType, Is.EqualTo(expectedResult.ExternalWorkflowType), $"Actual result instance has wrong {nameof(actualResult.ExternalWorkflowType)}");
            Assert.That(actualResult.IsNew, Is.EqualTo(expectedResult.IsNew), $"Actual result instance has wrong {nameof(actualResult.IsNew)}");

            // Process Assert
            AssertApprovalProcess(actualResult.Process, expectedResult.Process);

            // Settings Assert
            Assert.That(actualResult.Settings.ChangeStateOnEnd, Is.EqualTo(expectedResult.Settings.ChangeStateOnEnd), $"Actual result instance settings has wrong {nameof(expectedResult.Settings.ChangeStateOnEnd)}");
            Assert.That(actualResult.Settings.ChangeStateOnStart, Is.EqualTo(expectedResult.Settings.ChangeStateOnStart), $"Actual result instance settings has wrong {nameof(expectedResult.Settings.ChangeStateOnStart)}");
            Assert.That(actualResult.Settings.Cycle, Is.EqualTo(expectedResult.Settings.Cycle), $"Actual result instance settings has wrong {nameof(expectedResult.Settings.Cycle)}");
            Assert.That(actualResult.Settings.HistoryGroupID, Is.EqualTo(expectedResult.Settings.HistoryGroupID), $"Actual result instance settings has wrong {nameof(expectedResult.Settings.HistoryGroupID)}");
            Assert.That(actualResult.Settings.LastProcessNodeID, Is.EqualTo(expectedResult.Settings.LastProcessNodeID), $"Actual result instance settings has wrong {nameof(expectedResult.Settings.LastProcessNodeID)}");
            Assert.That(actualResult.Settings.ReturnAfterDisapproval, Is.EqualTo(expectedResult.Settings.ReturnAfterDisapproval), $"Actual result instance settings has wrong {nameof(expectedResult.Settings.ReturnAfterDisapproval)}");
            Assert.That(actualResult.Settings.ShowRevokeButton, Is.EqualTo(expectedResult.Settings.ShowRevokeButton), $"Actual result instance settings has wrong {nameof(expectedResult.Settings.ShowRevokeButton)}");
        }

        /// <summary>
        /// Метод для валидации экземпляра узла процесса согласования.
        /// </summary>
        /// <param name="actualResult">Проверяемый экземпляр узла процесса согласования.</param>
        /// <param name="expectedResult">Ожидаемый экземпляр узла процесса согласования.</param>
        public static void AssertApprovalNodeInstance(
            ApprovalProcessNodeInstance actualResult,
            ApprovalProcessNodeInstance expectedResult)
        {
            ThrowIfNull(actualResult);
            ThrowIfNull(expectedResult);

            // Properties Assert
            Assert.That(actualResult.IsNew, Is.EqualTo(expectedResult.IsNew), $"Actual result has wrong {nameof(expectedResult.IsNew)}");
            Assert.That(actualResult.NodeID, Is.EqualTo(expectedResult.NodeID), $"Actual result has wrong {nameof(expectedResult.NodeID)}");
            Assert.That(actualResult.ProcessID, Is.EqualTo(expectedResult.ProcessID), $"Actual result has wrong {nameof(expectedResult.ProcessID)}");
            Assert.That(actualResult.Type, Is.EqualTo(expectedResult.Type), $"Actual result has wrong {nameof(expectedResult.Type)}");

            if (actualResult.Type == NodeTypes.Approval)
            {
                var actualNodeData = actualResult.Data.FromSerializedDictionary<ApproverNodeData>();
                var expectedNodeData = expectedResult.Data.FromSerializedDictionary<ApproverNodeData>();

                Assert.That(actualNodeData, Is.Not.Null, $"Actual result has no data.");
                Assert.That(expectedNodeData, Is.Not.Null, $"Expected result has no data.");
                Assert.That(actualNodeData!.Duration, Is.EqualTo(expectedNodeData!.Duration), $"Actual result has wrong {nameof(ApproverNodeData.Duration)}.");
                Assert.That(actualNodeData.Text, Is.EqualTo(expectedNodeData.Text), $"Actual result has wrong {nameof(ApproverNodeData.Text)}.");
                Assert.That(actualNodeData.Approvers, Is.EquivalentTo(expectedNodeData.Approvers), $"Actual result has wrong {nameof(ApproverNodeData.Approvers)}.");
            }
        }

        /// <summary>
        /// Метод для валидации информации о задании экземпляра узла процесса согласования.
        /// </summary>
        /// <param name="actualResult">Проверяемый объект с информацией о задании экземпляра узла процесса согласования.</param>
        /// <param name="expectedResult">Ожидаемый объект с информацией о задании экземпляра узла процесса согласования.</param>
        public static void AssertApprovalProcessTaskInfo(
            ApprovalProcessTaskInfo actualResult,
            ApprovalProcessTaskInfo expectedResult)
        {
            ThrowIfNull(actualResult);
            ThrowIfNull(expectedResult);

            // Properties Assert
            Assert.That(actualResult.NodeID, Is.EqualTo(expectedResult.NodeID), $"Actual result has wrong {nameof(expectedResult.NodeID)}");
            Assert.That(actualResult.TaskID, Is.EqualTo(expectedResult.TaskID), $"Actual result has wrong {nameof(expectedResult.TaskID)}");
        }

        #endregion
    }
}
