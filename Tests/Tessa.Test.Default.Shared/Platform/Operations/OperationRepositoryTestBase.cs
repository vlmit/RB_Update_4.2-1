using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Tessa.Platform;
using Tessa.Platform.Operations;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Platform.Operations
{
    /// <summary>
    /// Базовый класс для тестов <see cref="IOperationRepository"/>.
    /// </summary>
    /// <typeparam name="T">Тип для которого этот класс является обёрткой.</typeparam>
    public abstract class OperationRepositoryTestBase<T>(T testBase) : TestBaseWrapper<T>(testBase)
        where T : class, ITestBase
    {
        #region Create Tests

        /// <summary>
        /// Проверяет создание операции в состоянии по умолчанию.
        /// </summary>
        [Test]
        public async Task CreateDefaultState()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.State, Is.EqualTo(OperationState.Created));
            Assert.That(operation.InProgress, Is.Null);
        }

        /// <summary>
        /// Проверяет создание операции в состоянии <see cref="OperationState.InProgress"/>.
        /// </summary>
        [Test]
        public async Task CreateInProgressState()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(operation.InProgress, Is.EqualTo(operation.Created));
        }

        /// <summary>
        /// Проверяет создание операции, сообщающей о ходе своего выполнения.
        /// </summary>
        [Test]
        public async Task CreateReportsProgress()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.ReportsProgress);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Progress.HasValue, Is.True);
            Assert.That(operation.Progress, Is.EqualTo(0.0));
        }

        /// <summary>
        /// Проверяет создание операции, не сообщающей о ходе своего выполнения.
        /// </summary>
        [Test]
        public async Task CreateWithoutProgress()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Progress.HasValue, Is.False);
            Assert.That(operation.Progress, Is.Null);
        }

        /// <summary>
        /// Проверяет создание операции вместе с кратким описанием.
        /// </summary>
        [Test]
        public async Task CreateWithDigest()
        {
            const string digest = "operation #42";

            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.None, digest);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Digest, Is.EqualTo(digest));
        }

        /// <summary>
        /// Проверяет создание операции вместе с запросом.
        /// </summary>
        [Test]
        public async Task CreateWithRequest()
        {
            var request = new OperationRequest();
            request.DynamicInfo.MagicNumber = 42;

            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.None, null, request);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Request, Is.Not.Null);
            Assert.That(operation.Request.DynamicInfo.MagicNumber == 42);
        }

        /// <summary>
        /// Проверяет сложное создание операции.
        /// </summary>
        [Test]
        public async Task CreateComplex([Values] bool clientSideOperationID)
        {
            // русские буквы для теста сериализации строк в REST-контроллерах при вызове с клиента
            const string digest = "operation #42 русские буквы";

            const string jsonStatus = "{\"Test\": \"Status\"}";

            var request = new OperationRequest();
            request.DynamicInfo.MagicNumber = 42;

            var clientOperationID = clientSideOperationID ? (Guid?) Guid.NewGuid() : null;
            var objectID = Guid.NewGuid();
            var id = await this.OperationRepository.CreateAsync(
                OperationTypes.ConvertingFile,
                OperationCreationFlags.CreateInProgress
                | OperationCreationFlags.ReportsProgress,
                digest,
                request,
                id: clientOperationID,
                objectID: objectID,
                jsonStatus: jsonStatus);

            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.ID, Is.EqualTo(id));
            Assert.That(operation.ObjectID, Is.EqualTo(objectID));
            Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(operation.Progress.HasValue, Is.True);
            Assert.That(operation.Progress, Is.EqualTo(0.0));
            Assert.That(operation.Digest, Is.EqualTo(digest));
            Assert.That(operation.Request, Is.Not.Null);
            Assert.That(operation.Request.DynamicInfo.MagicNumber == 42);
            if (clientOperationID.HasValue)
            {
                Assert.That(id, Is.EqualTo(clientOperationID));
            }

            Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));
        }

        /// <summary>
        /// Проверяет, что дата создания операции отличается от текущей даты менее, чем на минуту.
        /// </summary>
        [Test]
        public async Task Created()
        {
            var created = DateTime.UtcNow;
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(ComparisonHelper.FuzzyEquals(operation.Created, created, new TimeSpan(0, 1, 0)), Is.True);
        }

        /// <summary>
        /// Проверяет создание отложенной операции.
        /// Проверяет, что состояние операции "Отложено".
        /// Проверяет, что дата откладывания отличается меньше чем на секунду (погрешность округления в DATETIME ~3 мс).
        /// </summary>
        [Test]
        public async Task CreatePostponed()
        {
            var postponed = DateTime.UtcNow.AddMinutes(1);
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, postponed: postponed);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Postponed, Is.Not.Null);
            Assert.That(operation.State, Is.EqualTo(OperationState.Postponed));
            Assert.That(ComparisonHelper.FuzzyEquals(operation.Postponed, postponed), Is.True);
        }

        /// <summary>
        /// Проверяет, что при создании операции устаналивается идентификатор сессии, полученный из самой сессии.
        /// </summary>
        [Test]
        public async Task SessionID()
        {
            var operationID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);
            var operation = await this.OperationRepository.TryGetAsync(operationID);
            await this.OperationRepository.DeleteAsync(operationID, OperationTypes.SavingCard);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.SessionID, Is.EqualTo(this.Session.ID));
        }

        #endregion

        #region Get Tests

        /// <summary>
        /// Проверяет свойства операции, возвращённые в TryGet и не проверенные тестами на создание карточки.
        /// </summary>
        [Test]
        public async Task TryGet()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.ID, Is.EqualTo(id));
            Assert.That(operation.TypeID, Is.EqualTo(OperationTypes.ConvertingFile));
            Assert.That(operation.Completed, Is.Null);
            Assert.That(operation.Digest, Is.Null);
            Assert.That(operation.Request, Is.Null);
            Assert.That(operation.Response, Is.Null);
            Assert.That(operation.JsonStatus, Is.Null);
        }

        /// <summary>
        /// Проверяет наличие свойств Digest, JsonStatus, Request и Response для загруженной операции с loadEverything: true.
        /// </summary>
        [Test]
        public async Task TryGetWithEverything()
        {
            const string digest = "operation #42";
            const string jsonStatus = "{\"Test\": \"Some text\"}";

            var request = new OperationRequest();
            request.DynamicInfo.MagicNumber = 42;

            var response = new OperationResponse();
            response.DynamicInfo.MagicString = "42";

            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress, digest, request, jsonStatus: jsonStatus);
            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile, response);

            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Digest, Is.EqualTo(digest));
            Assert.That(operation.Request, Is.Not.Null);
            Assert.That(operation.Request.DynamicInfo.MagicNumber == 42);
            Assert.That(operation.Response, Is.Not.Null);
            Assert.That(operation.Response.DynamicInfo.MagicString == "42");
            Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));
        }

        /// <summary>
        /// Проверяет отсутствие свойств JsonStatus, Request и Response, но наличие Digest для загруженной операции с loadEverything: false.
        /// </summary>
        [Test]
        public async Task TryGetWithoutEverything()
        {
            const string digest = "operation #42";
            const string jsonStatus = "{\"Test\": \"Some text\"}";

            var request = new OperationRequest();
            request.DynamicInfo.MagicNumber = 42;

            var response = new OperationResponse();
            response.DynamicInfo.MagicString = "42";

            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress, digest, request, jsonStatus: jsonStatus);
            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile, response);

            var operation = await this.OperationRepository.TryGetAsync(id, loadEverything: false);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Digest, Is.EqualTo(digest));
            Assert.That(operation.Request, Is.Null);
            Assert.That(operation.Response, Is.Null);
            Assert.That(operation.JsonStatus, Is.Null);
        }

        /// <summary>
        /// Проверяет состояние операции на различных этапах.
        /// </summary>
        [Test]
        public async Task GetState()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var createdState = await this.OperationRepository.GetStateAsync(id);
            var createdStateAndProgress = await this.OperationRepository.GetStateAndProgressAsync(id);

            await this.OperationRepository.StartAsync(id, OperationTypes.ConvertingFile);
            var inProgressState = await this.OperationRepository.GetStateAsync(id);
            var inProgressStateAndProgress = await this.OperationRepository.GetStateAndProgressAsync(id);

            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile);
            var completedState = await this.OperationRepository.GetStateAsync(id);
            var completedStateAndProgress = await this.OperationRepository.GetStateAndProgressAsync(id);

            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            var deletedState = await this.OperationRepository.GetStateAsync(id);
            var deletedStateAndProgress = await this.OperationRepository.GetStateAndProgressAsync(id);

            Assert.That(createdState, Is.EqualTo(OperationState.Created));
            Assert.That(createdStateAndProgress?.State, Is.EqualTo(OperationState.Created));
            Assert.That(inProgressState, Is.EqualTo(OperationState.InProgress));
            Assert.That(inProgressStateAndProgress?.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(completedState, Is.EqualTo(OperationState.Completed));
            Assert.That(completedStateAndProgress?.State, Is.EqualTo(OperationState.Completed));
            Assert.That(deletedState, Is.Null);
            Assert.That(deletedStateAndProgress, Is.Null);
        }

        /// <summary>
        /// Проверяет наличие операции.
        /// </summary>
        [Test]
        public async Task IsAlive()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var isAliveWhenAlive = await this.OperationRepository.IsAliveAsync(id);

            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            var isAliveWhenDeleted = await this.OperationRepository.IsAliveAsync(id);

            Assert.That(isAliveWhenAlive, Is.True);
            Assert.That(isAliveWhenDeleted, Is.False);
        }

        #endregion

        #region Start Tests

        /// <summary>
        /// Начинает выполнение операции и проверяет, что дата начала выполнения
        /// отличается от текущей даты менее, чем на минуту.
        /// </summary>
        [Test]
        public async Task Start()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);

            var inProgress = DateTime.UtcNow;
            await this.OperationRepository.StartAsync(id, OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, operation?.TypeID);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(operation.InProgress, Is.Not.Null);
            Assert.That(ComparisonHelper.FuzzyEquals(operation.InProgress.Value, inProgress, new TimeSpan(0, 1, 0)), Is.True);
        }

        /// <summary>
        /// Проверяет, что повторное начало операции не влияет на дату её начала.
        /// </summary>
        [Test]
        public async Task StartWhenCreatedInProgress()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress);
            var beforeStartOperation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.StartAsync(id, OperationTypes.ConvertingFile);
            var afterStartOperation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(beforeStartOperation, Is.Not.Null);
            Assert.That(afterStartOperation, Is.Not.Null);
            Assert.That(afterStartOperation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(afterStartOperation.InProgress, Is.EqualTo(beforeStartOperation.InProgress));
        }

        /// <summary>
        /// Проверяет, что два начала операции подряд не влияют на дату её первого начала.
        /// </summary>
        [Test]
        public async Task StartTwice()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            await this.OperationRepository.StartAsync(id, OperationTypes.ConvertingFile);
            var beforeStartOperation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.StartAsync(id, OperationTypes.ConvertingFile);
            var afterStartOperation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(beforeStartOperation, Is.Not.Null);
            Assert.That(afterStartOperation, Is.Not.Null);
            Assert.That(afterStartOperation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(afterStartOperation.InProgress, Is.EqualTo(beforeStartOperation.InProgress));
        }

        /// <summary>
        /// Проверяет начало операции для типа, когда любые операции отсутствуют.
        /// </summary>
        [Test]
        public async Task StartFirstNothing()
        {
            var started = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);

            Assert.That(started, Is.Null);
        }

        /// <summary>
        /// Проверяет начало операции для типа, когда подходящие операции заданного типа отсутствуют.
        /// </summary>
        [Test]
        public async Task StartFirstWrongType()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var started = await this.OperationRepository.StartFirstAsync([OperationTypes.SavingCard]);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(started, Is.Null);
        }

        /// <summary>
        /// Проверяет начало операции для типа, когда подходящие операции в состоянии
        /// <see cref="OperationState.Created"/> отсутствуют.
        /// </summary>
        [Test]
        public async Task StartFirstWrongState()
        {
            var firstID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress);
            var secondID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile,
                OperationCreationFlags.CreateInProgress);
            await this.OperationRepository.CompleteAsync(secondID, OperationTypes.ConvertingFile);

            var started = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            await this.OperationRepository.DeleteAsync(firstID, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(secondID, OperationTypes.ConvertingFile);

            Assert.That(started, Is.Null);
        }

        /// <summary>
        /// Проверяет начало операции для типа, когда имеется единственная подходящая операция этого типа.
        /// Проверяет, что дата начала операции отличается от текущей меньше, чем на минуту.
        /// Проверяет, что повторный вызов метода не влияет на дату начала первой операции.
        /// </summary>
        [Test]
        public async Task StartFirstSingle()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var firstOtherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);
            var secondOtherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);

            var inProgress = DateTime.UtcNow;
            var firstStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var operation = await this.OperationRepository.TryGetAsync(id);
            var secondStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var operationAfterSecondStarted = await this.OperationRepository.TryGetAsync(id);

            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(firstOtherID, OperationTypes.SavingCard);
            await this.OperationRepository.DeleteAsync(secondOtherID, OperationTypes.SavingCard);

            Assert.That(firstStarted, Is.EqualTo(id));
            Assert.That(secondStarted, Is.Null);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.ID, Is.EqualTo(id));
            Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(operation.InProgress, Is.Not.Null);
            Assert.That(ComparisonHelper.FuzzyEquals(operation.InProgress.Value, inProgress, new TimeSpan(0, 1, 0)), Is.True);

            Assert.That(operationAfterSecondStarted, Is.Not.Null);
            Assert.That(operationAfterSecondStarted.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(operationAfterSecondStarted.InProgress, Is.EqualTo(operationAfterSecondStarted.InProgress));
        }

        /// <summary>
        /// Проверяет начало двух операций для типа.
        /// </summary>
        [Test]
        public async Task StartFirstTwice()
        {
            var firstID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            await Task.Delay(50);
            var secondID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            await Task.Delay(50);
            var thirdID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var otherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);

            var firstStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var secondStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var firstOperation = await this.OperationRepository.TryGetAsync(firstID);
            var secondOperation = await this.OperationRepository.TryGetAsync(secondID);
            var thirdOperation = await this.OperationRepository.TryGetAsync(thirdID);
            var otherOperation = await this.OperationRepository.TryGetAsync(otherID);

            await this.OperationRepository.DeleteAsync(firstID, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(secondID, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(thirdID, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(otherID, OperationTypes.SavingCard);

            Assert.That(firstStarted, Is.Not.Null);
            Assert.That(secondStarted, Is.Not.Null);
            Assert.That(secondStarted, Is.Not.EqualTo(firstStarted));

            var createdOperationIDs = new[]
            {
                firstID,
                secondID,
                thirdID
            };

            var startedOperationsIDs = new[]
            {
                firstStarted.Value,
                secondStarted.Value
            };

            Assert.That(startedOperationsIDs.All(so => createdOperationIDs.Contains(so)), Is.EqualTo(true));

            Assert.That(firstOperation, Is.Not.Null);
            Assert.That(secondOperation, Is.Not.Null);
            Assert.That(thirdOperation, Is.Not.Null);

            var operations = new[]
            {
                firstOperation,
                secondOperation,
                thirdOperation
            };

            Assert.That(operations.Count(o => o.State == OperationState.InProgress), Is.EqualTo(2));
            Assert.That(operations.Count(o => o.State == OperationState.Created), Is.EqualTo(1));

            Assert.That(otherOperation, Is.Not.Null);
            Assert.That(otherOperation.State, Is.EqualTo(OperationState.Created));
        }

        /// <summary>
        /// Проверяет начало операции для типа, когда имеется единственная подходящая операция этого типа, и время, до которого она отложена, ещё не настало.
        /// Проверяет, что операция не может быть запущена, пока она отложена.
        /// </summary>
        [Test]
        public async Task StartFirstPostponed()
        {
            DateTime? postponed = DateTime.UtcNow.AddMinutes(1);
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, postponed: postponed);
            var firstOtherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);
            var secondOtherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);

            var firstStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);

            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(firstOtherID, OperationTypes.SavingCard);
            await this.OperationRepository.DeleteAsync(secondOtherID, OperationTypes.SavingCard);

            Assert.That(firstStarted, Is.Null);
        }

        /// <summary>
        /// Проверяет начало операции для типа, когда имеется единственная подходящая операция этого типа и время, до которого она отложена, уже настало.
        /// Проверяет, что дата начала операции отличается от текущей меньше, чем на минуту.
        /// Проверяет, что повторный вызов метода не влияет на дату начала первой операции.
        /// </summary>
        [Test]
        public async Task StartFirstReadyPostponed()
        {
            DateTime? postponed = DateTime.UtcNow.AddMinutes(-1);
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, postponed: postponed);
            var firstOtherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);
            var secondOtherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);

            var inProgress = DateTime.UtcNow;
            var firstStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var operation = await this.OperationRepository.TryGetAsync(id);
            var secondStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var operationAfterSecondStarted = await this.OperationRepository.TryGetAsync(id);

            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(firstOtherID, OperationTypes.SavingCard);
            await this.OperationRepository.DeleteAsync(secondOtherID, OperationTypes.SavingCard);

            Assert.That(firstStarted, Is.EqualTo(id));
            Assert.That(secondStarted, Is.Null);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.ID, Is.EqualTo(id));
            Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(operation.InProgress, Is.Not.Null);
            Assert.That(ComparisonHelper.FuzzyEquals(operation.InProgress.Value, inProgress, new TimeSpan(0, 1, 0)), Is.True);

            Assert.That(operationAfterSecondStarted, Is.Not.Null);
            Assert.That(operationAfterSecondStarted.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(operationAfterSecondStarted.InProgress, Is.EqualTo(operationAfterSecondStarted.InProgress));
        }

        /// <summary>
        /// Проверяет начало двух операций для типа при наличии одной отложенной, одной отложенной, но уже готовой к запуску, и одной новой операции.
        /// </summary>
        [Test]
        public async Task StartFirstPostponedAndCreated()
        {
            DateTime? postponed1 = DateTime.UtcNow.AddMinutes(1);
            DateTime? postponed2 = DateTime.UtcNow.AddMinutes(-1);
            var firstID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, postponed: postponed1);
            await Task.Delay(50);
            var secondID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, postponed: postponed2);
            await Task.Delay(50);
            var thirdID = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            var otherID = await this.OperationRepository.CreateAsync(OperationTypes.SavingCard);

            var firstStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var secondStarted = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            var firstOperation = await this.OperationRepository.TryGetAsync(firstID);
            var secondOperation = await this.OperationRepository.TryGetAsync(secondID);
            var thirdOperation = await this.OperationRepository.TryGetAsync(thirdID);
            var otherOperation = await this.OperationRepository.TryGetAsync(otherID);

            await this.OperationRepository.DeleteAsync(firstID, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(secondID, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(thirdID, OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(otherID, OperationTypes.SavingCard);

            Assert.That(firstStarted, Is.EqualTo(secondID));
            Assert.That(secondStarted, Is.EqualTo(thirdID));

            Assert.That(firstOperation, Is.Not.Null);
            Assert.That(firstOperation.State, Is.EqualTo(OperationState.Postponed));
            Assert.That(secondOperation, Is.Not.Null);
            Assert.That(secondOperation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(thirdOperation, Is.Not.Null);
            Assert.That(thirdOperation.State, Is.EqualTo(OperationState.InProgress));
            Assert.That(otherOperation, Is.Not.Null);
            Assert.That(otherOperation.State, Is.EqualTo(OperationState.Created));
        }

        [Test]
        public async Task StartPostponedWithOffset()
        {
            var postponed = DateTime.UtcNow.AddHours(1);
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, postponed: postponed);

            var started = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            Assert.That(started, Is.Null);

            this.SetServerUtcNow(postponed.AddMinutes(-30));

            started = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            Assert.That(started, Is.Null);

            this.SetServerUtcNow(postponed.AddMinutes(30));

            started = await this.OperationRepository.StartFirstAsync([OperationTypes.ConvertingFile]);
            Assert.That(started, Is.EqualTo(id));

            await this.OperationRepository.DeleteAsync(started!.Value, OperationTypes.ConvertingFile);
        }

        #endregion

        #region ReturnToCreated Tests

        /// <summary>
        /// Создаёт операцию без взятия в работу, возвращает в состояние <see cref="OperationState.Created"/>, после чего берёт в работу.
        /// Двойное взятие в работу невозможно.
        /// </summary>
        [Test]
        public async Task ReturnCreatedToCreated([Values] bool hasJsonStatus)
        {
            // create operation
            var operationType = OperationTypes.ConvertingFile;
            var id = await this.OperationRepository.CreateAsync(operationType, jsonStatus: hasJsonStatus ? "{\"Number\":42}" : null);
            try
            {
                var operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.Created));
                Assert.That(operation.InProgress, Is.Null);
                var jsonStatus = operation.JsonStatus; // Postgres can re-serialize the value

                // return it to Created state
                var returned = await this.OperationRepository.ReturnToCreatedAsync(id, operationType);
                Assert.That(returned, Is.True);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.Created));
                Assert.That(operation.InProgress, Is.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // take in progress
                var started = await this.OperationRepository.StartAsync(id);
                Assert.That(started, Is.True);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
                Assert.That(operation.InProgress, Is.Not.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // second attempt to take in progress fails
                started = await this.OperationRepository.StartAsync(id);
                Assert.That(started, Is.False);
            }
            finally
            {
                await this.OperationRepository.DeleteAsync(id, operationType);
            }
        }

        /// <summary>
        /// Создаёт взятую в работу операцию, возвращает в состояние <see cref="OperationState.Created"/>, после чего берёт заново в работу.
        /// Двойное взятие в работу невозможно.
        /// </summary>
        [Test]
        public async Task ReturnInProgressToCreated([Values] bool hasJsonStatus, [Values] bool reportProgress)
        {
            // create operation in progress
            var operationType = OperationTypes.ConvertingFile;
            var flags = OperationCreationFlags.CreateInProgress;
            if (reportProgress)
            {
                flags |= OperationCreationFlags.ReportsProgress;
            }

            var id = await this.OperationRepository.CreateAsync(operationType, flags, jsonStatus: hasJsonStatus ? "{\"Number\":42}" : null);
            try
            {
                var operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
                Assert.That(operation.InProgress, Is.Not.Null);
                Assert.That(operation.Progress, reportProgress ? Is.Zero : Is.Null);
                var jsonStatus = operation.JsonStatus; // Postgres can re-serialize the value

                // report 50% progress, check that its reported
                if (reportProgress)
                {
                    var reported = await this.OperationRepository.ReportProgressAsync(id, 50.0);
                    Assert.That(reported, Is.True);

                    operation = await this.OperationRepository.TryGetAsync(id);
                    Assert.That(operation, Is.Not.Null);
                    Assert.That(operation.Progress, Is.EqualTo(50.0));
                }

                // return the operation to Created state, progress is back to default
                var returned = await this.OperationRepository.ReturnToCreatedAsync(id, operationType);
                Assert.That(returned, Is.True);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.Created));
                Assert.That(operation.InProgress, Is.Null);
                Assert.That(operation.Progress, reportProgress ? Is.Zero : Is.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // take in progress
                var started = await this.OperationRepository.StartAsync(id, operationType);
                Assert.That(started, Is.True);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
                Assert.That(operation.InProgress, Is.Not.Null);
                Assert.That(operation.Progress, reportProgress ? Is.Zero : Is.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // second attempt to take in progress fails
                started = await this.OperationRepository.StartAsync(id, operationType);
                Assert.That(started, Is.False);
            }
            finally
            {
                await this.OperationRepository.DeleteAsync(id, operationType);
            }
        }

        /// <summary>
        /// Создаёт отложенную операцию, возвращает в состояние <see cref="OperationState.Created"/>, после чего берёт в работу.
        /// Двойное взятие в работу невозможно.
        /// </summary>
        [Test]
        public async Task ReturnPostponedToCreated([Values] bool hasJsonStatus)
        {
            // create operation
            var operationType = OperationTypes.ConvertingFile;
            var postponed = DateTime.UtcNow.AddMonths(1);
            var id = await this.OperationRepository.CreateAsync(operationType, postponed: postponed, jsonStatus: hasJsonStatus ? "{\"Number\":42}" : null);
            try
            {
                var operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.Postponed));
                Assert.That(operation.InProgress, Is.Null);
                Assert.That(ComparisonHelper.FuzzyEquals(operation.Postponed, postponed, new TimeSpan(0, 1, 0)), Is.True);
                var jsonStatus = operation.JsonStatus; // Postgres can re-serialize the value

                // return it to Created state
                var returned = await this.OperationRepository.ReturnToCreatedAsync(id, operationType);
                Assert.That(returned, Is.True);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.Created));
                Assert.That(operation.InProgress, Is.Null);
                Assert.That(operation.Postponed, Is.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // take in progress
                var started = await this.OperationRepository.StartAsync(id, operationType);
                Assert.That(started, Is.True);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.InProgress));
                Assert.That(operation.InProgress, Is.Not.Null);
                Assert.That(operation.Postponed, Is.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // second attempt to take in progress fails
                started = await this.OperationRepository.StartAsync(id, operationType);
                Assert.That(started, Is.False);
            }
            finally
            {
                await this.OperationRepository.DeleteAsync(id, operationType);
            }
        }

        /// <summary>
        /// Создаёт и завершает операцию, неуспешно пытается вернуть в состояние <see cref="OperationState.Created"/>.
        /// После этого взятие в работу также невозможно.
        /// </summary>
        [Test]
        public async Task ReturnCompletedToCreated([Values] bool hasJsonStatus, [Values] bool createInProgress)
        {
            // create operation (in progress or not)
            var operationType = OperationTypes.ConvertingFile;
            var id = await this.OperationRepository.CreateAsync(operationType, createInProgress ? OperationCreationFlags.CreateInProgress : OperationCreationFlags.None,
                jsonStatus: hasJsonStatus ? "{\"Number\":42}" : null);
            try
            {
                var operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(createInProgress ? OperationState.InProgress : OperationState.Created));
                Assert.That(operation.InProgress, createInProgress ? Is.Not.Null : Is.Null);
                var jsonStatus = operation.JsonStatus; // Postgres can re-serialize the value

                // complete the operation
                await this.OperationRepository.CompleteAsync(id, operationType);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.Completed));
                Assert.That(operation.Completed, Is.Not.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // fail to return it to Created state
                var returned = await this.OperationRepository.ReturnToCreatedAsync(id, operationType);
                Assert.That(returned, Is.False);

                operation = await this.OperationRepository.TryGetAsync(id);
                Assert.That(operation, Is.Not.Null);
                Assert.That(operation.State, Is.EqualTo(OperationState.Completed));
                Assert.That(operation.Completed, Is.Not.Null);
                Assert.That(operation.JsonStatus, Is.EqualTo(jsonStatus));

                // attempt to take in progress fails
                var started = await this.OperationRepository.StartAsync(id, operationType);
                Assert.That(started, Is.False);
            }
            finally
            {
                await this.OperationRepository.DeleteAsync(id, operationType);
            }
        }

        /// <summary>
        /// Неуспешно пытается вернуть в состояние <see cref="OperationState.Created"/> несуществующую операцию.
        /// После этого взятие в работу также невозможно.
        /// </summary>
        [Test]
        public async Task ReturnUnknownToCreated()
        {
            var operationType = OperationTypes.ConvertingFile;
            var id = Guid.NewGuid();

            // fail to return unknown operation to Created state
            var returned = await this.OperationRepository.ReturnToCreatedAsync(id, operationType);
            Assert.That(returned, Is.False);

            // fail to take it in progress afterward
            var started = await this.OperationRepository.StartAsync(id, operationType);
            Assert.That(started, Is.False);
        }

        #endregion

        #region ReportProgress Tests

        /// <summary>
        /// Проверяет корректные сообщения о прогрессе операции.
        /// </summary>
        [Test]
        public async Task ReportProgress()
        {
            var id = await this.OperationRepository.CreateAsync(
                OperationTypes.ConvertingFile,
                OperationCreationFlags.ReportsProgress);

            await this.OperationRepository.StartAsync(id, OperationTypes.ConvertingFile);

            var minReportResult = await this.OperationRepository.ReportProgressAsync(id, 0);
            var minOperation = await this.OperationRepository.TryGetAsync(id);

            var mediumReportResult = await this.OperationRepository.ReportProgressAsync(id, 50);
            var mediumOperation = await this.OperationRepository.TryGetAsync(id);

            var maxReportResult = await this.OperationRepository.ReportProgressAsync(id, 100);
            var maxOperation = await this.OperationRepository.TryGetAsync(id);

            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(minReportResult, Is.True);
            Assert.That(minOperation.Progress, Is.EqualTo(0.0));
            Assert.That(mediumReportResult, Is.True);
            Assert.That(mediumOperation.Progress, Is.EqualTo(50.0));
            Assert.That(maxReportResult, Is.True);
            Assert.That(maxOperation.Progress, Is.EqualTo(100.0));
        }

        /// <summary>
        /// Проверяет сообщения о прогрессе для удалённой операции.
        /// </summary>
        [Test]
        public async Task ReportProgressToUnknown()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.ReportsProgress);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            var reportResult = await this.OperationRepository.ReportProgressAsync(id, 10);

            Assert.That(reportResult, Is.False);
        }

        /// <summary>
        /// Проверяет, что сообщение о прогрессе операции не выполняется,
        /// если операция создавалась как не сообщающая о прогрессе.
        /// </summary>
        [Test]
        public async Task ReportProgressWhenForbidden()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress);
            var reportResult = await this.OperationRepository.ReportProgressAsync(id, 10);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(reportResult, Is.False);
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Progress, Is.Null);
            Assert.That(operation.Progress.HasValue, Is.False);
        }

        /// <summary>
        /// Проверяет некорректные значение в сообщениях о прогрессе.
        /// </summary>
        [Test]
        public async Task ReportInvalidProgress()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.ReportsProgress);

            try
            {
                Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await this.OperationRepository.ReportProgressAsync(id, -1));
                Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await this.OperationRepository.ReportProgressAsync(id, 101));
            }
            finally
            {
                await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            }
        }

        #endregion

        #region UpdateStatus Tests

        /// <summary>
        /// Проверяет обновление и получение статуса операции.
        /// </summary>
        [Test]
        public async Task UpdateStatus()
        {
            const string startStatus = "{\"Test\": \"Start\"}";
            const string endStatus = "{\"Test\": \"End\"}";

            var id = await this.OperationRepository.CreateAsync(
                OperationTypes.ConvertingFile,
                OperationCreationFlags.ReportsProgress);

            await this.OperationRepository.StartAsync(id, OperationTypes.ConvertingFile);

            var startReportResult = await this.OperationRepository.UpdateStatusAsync(id, startStatus);
            var startOperation = await this.OperationRepository.TryGetAsync(id);
            var startOperationStatus = await this.OperationRepository.GetStateAndStatusAsync(id);

            var endReportResult = await this.OperationRepository.UpdateStatusAsync(id, endStatus);
            var endOperation = await this.OperationRepository.TryGetAsync(id);
            var endOperationStatus = await this.OperationRepository.GetStateAndStatusAsync(id);

            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(startReportResult, Is.True);
            Assert.That(startOperationStatus.HasValue, Is.True);
            Assert.That(startOperation!.JsonStatus, Is.EqualTo(startStatus));
            Assert.That(startOperationStatus!.Value.State, Is.EqualTo(startOperation.State));
            Assert.That(startOperationStatus.Value.JsonStatus, Is.EqualTo(startOperation.JsonStatus));

            Assert.That(endReportResult, Is.True);
            Assert.That(endOperationStatus.HasValue, Is.True);
            Assert.That(endOperation!.JsonStatus, Is.EqualTo(endStatus));
            Assert.That(endOperationStatus!.Value.State, Is.EqualTo(endOperation.State));
            Assert.That(endOperationStatus.Value.JsonStatus, Is.EqualTo(endOperation.JsonStatus));
        }

        /// <summary>
        /// Проверяет обновление статуса несуществующей операции.
        /// </summary>
        [Test]
        public async Task UpdateStatusToUnknown()
        {
            const string jsonStatus = "{\"Test\": \"Status\"}";

            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.ReportsProgress);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            var reportResult = await this.OperationRepository.UpdateStatusAsync(id, jsonStatus);
            var stateAndStatus = await this.OperationRepository.GetStateAndStatusAsync(id);

            Assert.That(reportResult, Is.False);
            Assert.That(stateAndStatus.HasValue, Is.False);
        }

        #endregion

        #region Complete Tests

        /// <summary>
        /// Завершает выполнение операции со свойством Response и проверяет его содержимое.
        /// </summary>
        [Test]
        public async Task CompleteWithResponse()
        {
            var response = new OperationResponse();
            response.DynamicInfo.MagicNumber = 42;
            response.ValidationResult.AddError(null, "Error text");

            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress);
            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile, response);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Response, Is.Not.Null);
            Assert.That(operation.Response.DynamicInfo.MagicNumber == 42);

            var result = operation.Response.ValidationResult.Build();
            Assert.That(result.Items.Count, Is.EqualTo(1));
            Assert.That(result.Items[0].Message, Is.EqualTo("Error text"));
        }

        /// <summary>
        /// Завершает выполнение операции без свойства Response и проверяет его содержимое.
        /// </summary>
        [Test]
        public async Task CompleteWithoutResponse()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress);
            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Response, Is.Null);
        }

        /// <summary>
        /// Проверяет, что два завершения операции подряд не влияют на дату её первого завершения.
        /// </summary>
        [Test]
        public async Task CompleteTwice()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile, OperationCreationFlags.CreateInProgress);
            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile);
            var beforeStartOperation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile);
            var afterStartOperation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(beforeStartOperation, Is.Not.Null);
            Assert.That(afterStartOperation, Is.Not.Null);
            Assert.That(afterStartOperation.State, Is.EqualTo(OperationState.Completed));
            Assert.That(afterStartOperation.Completed, Is.EqualTo(beforeStartOperation.Completed));
        }

        /// <summary>
        /// Проверяет, что корректно выполняется завершение операции из состояния <see cref="OperationState.Created"/>.
        /// </summary>
        [Test]
        public async Task CompleteFromCreated()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            await this.OperationRepository.CompleteAsync(id, OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Response, Is.Null);
        }

        #endregion

        #region Delete Tests

        /// <summary>
        /// Проверяет, что операция была удалена.
        /// </summary>
        [Test]
        public async Task Delete()
        {
            var id = await this.OperationRepository.CreateAsync(OperationTypes.ConvertingFile);
            await this.OperationRepository.DeleteAsync(id, OperationTypes.ConvertingFile);
            var operation = await this.OperationRepository.TryGetAsync(id);

            Assert.That(operation, Is.Null);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Создаёт репозиторий <see cref="IOperationRepository"/>, который используется для тестирования.
        /// </summary>
        /// <returns>Репозиторий, используемый для тестирования.</returns>
        protected abstract IOperationRepository ResolveOperationRepository();

        #endregion

        #region Properties

        private IOperationRepository operationRepository;

        /// <summary>
        /// Репозиторий, используемый для тестирования.
        /// </summary>
        public IOperationRepository OperationRepository => this.operationRepository ??= this.ResolveOperationRepository();

        #endregion
    }
}
