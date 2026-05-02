using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.WebUtilities;
using Unity;
using Ganss.Xss;
using Tessa.EDS;
using Tessa.Files;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Platform;
using Tessa.Platform.EDS;
using Tessa.Platform.Storage;
using Tessa.Platform.Runtime;
using Tessa.Platform.Operations;
using Tessa.Platform.Validation;
using Tessa.Extensions.Default.Server.Web.DeskiMobile.Models;
using ISession = Tessa.Platform.Runtime.ISession;
using FileSignatureResponse = Tessa.Extensions.Default.Server.Web.DeskiMobile.Models.FileSignatureResponse;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile
{
    public class DeskiMobileManager: IDeskiMobileManager
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="cardStreamServerRepository">Репозиторий для потокового управления карточками на сервере.</param>
        /// <param name="permissionsProvider">Объект, предоставляющий права доступа в соответствии с активной системой прав.</param>
        /// <param name="deskiMobileTokenManager">Manager, управляющий созданием и проверкой Jwt токенов для DeskiMobile.</param>
        /// <param name="operationRepository">Репозиторий, управляющий операциями.</param>
        /// <param name="cardRepository">Репозиторий для управления карточками.</param>
        /// <param name="edsProvider">Объект, обеспечивающий низкоуровневые функции по работе с электронной подписью.</param>
        /// <param name="cardCache">Кэш карточек настроек.</param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="cAdESManager">Manager, управляющий подписанием файлов.</param>
        /// <param name="htmlSanitizer">Объект, выполняющий санитайзинг HTML-документов.</param>
        public DeskiMobileManager(
            ICardStreamServerRepository cardStreamServerRepository,
            ICardServerPermissionsProvider permissionsProvider,
            IDeskiMobileTokenManager deskiMobileTokenManager,
            IOperationRepository operationRepository,
            ICardRepository cardRepository,
            IEDSProvider edsProvider,
            ICardCache cardCache,
            ISession session,
            ICAdESManager cAdESManager,
            [OptionalDependency] IHtmlSanitizer? htmlSanitizer = null)
        {
            this.cardStreamRepository = NotNullOrThrow(cardStreamServerRepository);
            this.permissionsProvider = NotNullOrThrow(permissionsProvider);
            this.deskiMobileTokenManager = NotNullOrThrow(deskiMobileTokenManager);
            this.operationRepository = NotNullOrThrow(operationRepository);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.edsProvider = NotNullOrThrow(edsProvider);
            this.cardCache = NotNullOrThrow(cardCache);
            this.session = NotNullOrThrow(session);
            this.cAdESManager = NotNullOrThrow(cAdESManager);
            this.htmlSanitizer = NotNullOrThrow(htmlSanitizer);
        }

        #endregion

        #region Fields
        
        private readonly ICardStreamServerRepository cardStreamRepository;
        
        private readonly ICardServerPermissionsProvider permissionsProvider;

        private readonly IDeskiMobileTokenManager deskiMobileTokenManager;
        
        private readonly IOperationRepository operationRepository;

        private readonly ICardRepository cardRepository;

        private readonly IEDSProvider edsProvider;

        private readonly ICardCache cardCache;
        
        private readonly ISession session;

        private readonly ICAdESManager cAdESManager;

        private readonly IHtmlSanitizer? htmlSanitizer;

        #endregion

        #region Public methods

        /// <summary>
        /// Генерация ссылки для выполнения операции с TESSA Assistant.
        /// </summary>
        /// <param name="files">Массив файлов над которыми будет выполняться операция.</param>
        /// <param name="operation">Тип выполняемой операции.</param>
        /// <param name="url">Url для выполнения запроса с мобильного приложения для подтверждения своего наличия на устройстве пользователя.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Cсылка для выполнения операции. Запрос выполняется с TESSA Assistant.</returns>
        public async ValueTask<string> GenerateLinkAsync(
            DeskiMobileFile[] files,
            string operation,
            string url,
            CancellationToken cancellationToken = default)
        {   
            // валидация url
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new InvalidOperationException("Url is incorrent.");
            }

            var filesRequest = new List<Dictionary<string, object?>>();
            foreach (var file in files)
            {
                var storage = new Dictionary<string, object?>();
                file.Serialize(storage);
                filesRequest.Add(storage);
            }

            // Создание операции
            var operationRequest = new OperationRequest
            {
                Info =
                {
                    ["FilesRequest"] = filesRequest,
                    ["OperationType"] = operation
                }
            };

            var operationID = await this.operationRepository.CreateAsync(
                OperationTypes.DeskiMobile,
                OperationCreationFlags.ReportsProgress,
                "DeskiMobile init",
                operationRequest,
                cancellationToken: cancellationToken);
            var base64Url = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(Uri.EscapeDataString(url)));

            var jwtLifeTime = await this.GetDeskiMobileJwtLifeTimeAsync(cancellationToken);
            var timespanJwtLifeTime = TimeSpan.FromMinutes((int) jwtLifeTime.TimeOfDay.TotalMinutes);

            var flags = this.GetTokenPermissionFlags(operation);
            var tokenJWT = this.deskiMobileTokenManager.CreateToken(
                timespanJwtLifeTime, this.session.User, operationID, flags);

            return $"{LinkHelper.DeskiMobileProtocol}://{operation}?version=2&token={tokenJWT}&url={base64Url}";
        }

        /// <summary>
        /// Запуск операции.
        /// Требуется вызвать при выполнении первого запроса с мобильного приложения TESSA Assistant.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        public async ValueTask StartOperationAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            if (operation.State != OperationState.Created)
            {
                throw new InvalidOperationException($"Can't start operation. Operation has invalid state={operation.State}.");
            }

            await this.operationRepository.StartAsync(operationID, operation.TypeID, cancellationToken);
        }

        /// <summary>
        /// Удаление операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        public async ValueTask DeleteOperationAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);

            ValidateOperation(operation);
            await this.operationRepository.DeleteAsync(operationID, operation.TypeID, cancellationToken);
        }

        /// <summary>
        /// Завершение операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="response">Результат выполнения операции.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        public async ValueTask CompleteOperationAsync(TokenInfo tokenInfo, OperationResponse response, CancellationToken cancellationToken = default)
        {
            var operationID = tokenInfo.OperationID;

            await this.operationRepository.CompleteAsync(operationID, response: response, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Получение статуса операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Статус операции.</returns>
        public async ValueTask<OperationState> GetOperationStateAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            return operation.State;
        }

        /// <summary>
        /// Получение содержимого операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Содержимое операции.</returns>
        public async ValueTask<OperationResponse?> TryGetOperationResponseAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);
            
            return operation.Response;
        }

        /// <summary>
        /// Получение идентификаторов (CacheID) и имён файлов из операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Объект с идентификаторами (CacheID) и именами файлов</returns>
        public async ValueTask<Dictionary<string, string?>> GetOperationFilesInfoAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            var files = operation.Request?.Info.TryGet<List<Dictionary<string, object?>>>("FilesRequest");
            if (files is null)
            {
                throw new InvalidOperationException("Can't get files from OperationRequest.");
            }

            Dictionary<string, string?> filesInfo = new Dictionary<string, string?>();
            foreach (var file in files)
            {
                var cacheId = file.TryGet<string>("CacheID");
                var fileName = file.TryGet<string>("FileName");
                if (cacheId is not null)
                {
                    filesInfo.Add(cacheId, fileName);
                }
            }

            return filesInfo;
        }

        /// <summary>
        /// Получение имени файла из операции по его идентификатору cacheID.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cacheID">Идентификатор файла в операции.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Имя файла из операции.</returns>
        public async ValueTask<string?> GetFileNameAsync(TokenInfo tokenInfo, string cacheID, CancellationToken cancellationToken = default)
        {
            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            var files = operation.Request?.Info.TryGet<List<Dictionary<string, object?>>>("FilesRequest");
            if (files is null)
            {
                throw new InvalidOperationException("Can't get files from OperationRequest.");
            }

            var fileStorage = files.Find(x => x.TryGet<string>("CacheID") == cacheID);
            if (fileStorage is null)
            {
                throw new InvalidOperationException($"Can't find file with CacheID={cacheID}.");
            }
            
            var file = new DeskiMobileFile();
            file.Deserialize(fileStorage);

            return file.FileName;
        }

        /// <summary>
        /// Получение содержимого файла в виде двоичного потока.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cacheID">Идентификатор файла в операции.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Содержимого файла в виде двоичного потока.</returns>
        public async ValueTask <Stream> GetFileContentAsync(
            TokenInfo tokenInfo, 
            string cacheID,
            CancellationToken cancellationToken = default)
        {
            if (!tokenInfo.Access.HasFlag(DeskiMobileTokenPermissionFlags.GetContent))
            {
                throw new InvalidOperationException("The JWT token does not have permission to get file content.");
            }

            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            var files = operation.Request?.Info.TryGet<List<Dictionary<string, object?>>>("FilesRequest");
            if (files is null)
            {
                throw new InvalidOperationException("Can't get files from OperationRequest.");
            }

            var fileStorage = files.Find(x => x.TryGet<string>("CacheID") == cacheID);
            if (fileStorage is null)
            {
                throw new InvalidOperationException($"Can't find file with CacheID={cacheID}.");
            }
            
            var file = new DeskiMobileFile();
            file.Deserialize(fileStorage);

            await using (SessionContext.Create(this.session.CreateNestedSessionToken(tokenInfo.UserID, tokenInfo.UserName)))
            {
                var contentRequest = new CardGetFileContentRequest
                {
                    ServiceType = CardServiceType.Client,
                    CardID = file.CardID,
                    FileID = file.FileID,
                    FileName = file.FileName,
                    VersionRowID = file.VersionRowID
                };

                // Вместо установки полных прав через SetFullPermissions, используем права текущего пользователя (того, кто указан в токене).
                // Для этого достаточно не вызывать SetFullPermissions вообще — тогда права будут определяться по текущей сессии пользователя, которую мы уже создаем через SessionContext.Create.
                //this.permissionsProvider.SetFullPermissions(contentRequest);
                var contentResult = await this.cardStreamRepository.GetFileContentAsync(contentRequest, cancellationToken);
                var contentResponse = contentResult.Response;

                var contentValidationResult = contentResponse.ValidationResult.Build();
                if (!contentValidationResult.IsSuccessful)
                {
                    throw new ValidationException(contentValidationResult);
                }

                return await contentResult.GetContentOrThrowAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Получение сигнатур подписи для всех файлов из операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Сигнатуры подписи всех файлов из операции.</returns>
        public async ValueTask<Dictionary<string, List<FileSignatureResponse>>> GetSignaturesAsync(
            TokenInfo tokenInfo,
            CancellationToken cancellationToken = default)
        {
            if (!tokenInfo.Access.HasFlag(DeskiMobileTokenPermissionFlags.GetSignatures))
            {
                throw new InvalidOperationException("The JWT token does not have permission to get signatures.");
            }

            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            var files = operation.Request?.Info.TryGet<List<Dictionary<string, object?>>>("FilesRequest");
            if (files is null)
            {
                throw new InvalidOperationException("Can't get files from OperationRequest.");
            }
            
            var result = new Dictionary<string, List<FileSignatureResponse>>();
            await using (SessionContext.Create(this.session.CreateNestedSessionToken(tokenInfo.UserID, tokenInfo.UserName)))
            {
                foreach (var fileStorage in files)
                {
                    var fileInfo = new DeskiMobileFile();
                    fileInfo.Deserialize(fileStorage);

                    var cardRequest = new CardRequest
                    {
                        CardID = fileInfo.CardID,
                        FileID = fileInfo.FileID,
                        FileVersionID = fileInfo.VersionRowID,
                        RequestType = CardRequestTypes.GetVersionSignatures
                    };
                    cardRequest.SetLoadData(true);

                    var response = await this.cardRepository.RequestAsync(cardRequest, cancellationToken).ConfigureAwait(false);
                    ThrowIfNull(response, "Failed to get file signatures.");
                    if (!response.ValidationResult.IsSuccessful())
                    {
                        throw new ValidationException(response.ValidationResult.Build());
                    }

                    ICollection<CardRow>? resultingSignatureRows = response.TryGetRows();
                    ThrowIfNull(resultingSignatureRows, $"Can't find signatures for file with fileID={fileInfo.FileID}.");
                    if (resultingSignatureRows.Count == 0)
                    {
                        throw new InvalidOperationException($"Can't find signatures for file with fileID={fileInfo.FileID}.");
                    }

                    var fileSignatureList = new List<FileSignatureResponse>();
                    foreach (CardRow signatureRow in resultingSignatureRows)
                    {
                        var versionID = signatureRow.Get<Guid>("VersionRowID");
                        if (versionID != fileInfo.VersionRowID)
                        {
                            continue;
                        }

                        var fileSignature = signatureRow.Get<byte[]?>("Data");
                        if (fileSignature is null)
                        {
                            continue;
                        }

                        string base64Signature = Convert.ToBase64String(fileSignature, 0, fileSignature.Length);
                        fileSignatureList.Add(new FileSignatureResponse
                        {
                            Signature = base64Signature,
                            ID = signatureRow.RowID,
                            SubjectName = signatureRow.Get<string>("SubjectName"),
                            Company = signatureRow.Get<string>("Company"),
                            SignedDate = signatureRow.Get<DateTime>("Signed"),
                            EventType = (FileSignatureEventType) signatureRow.Get<int>("EventID"),
                            SignatureProfile = (SignatureProfile) signatureRow.Get<int>("SignatureProfileID"),
                            SignatureType = (SignatureType) signatureRow.Get<int>("SignatureTypeID"),
                            UserName = signatureRow.Get<string>("UserName")
                        });
                    }

                    result.Add(fileInfo.CacheID, fileSignatureList);
                }
            }
        
            return result;
        }

        /// <summary>
        /// Формирование OperationResponse c результатами обогащения подписи всех файлов в операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="parameters">Объект, в котором содержится информация о сигнатурах подписи для обогащения.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>OperationResponse c результатами обогащения подписи всех файлов в операции.</returns>
        public async ValueTask<OperationResponse> GetOperationResponseForEnhanceAsync(
            TokenInfo tokenInfo, 
            DeskiMobileEnhanceRequest parameters,
            CancellationToken cancellationToken = default)
        {
            if (!tokenInfo.Access.HasFlag(DeskiMobileTokenPermissionFlags.Enhance))
            {
                throw new InvalidOperationException("The JWT token does not have permission to enhance signatures.");
            }

            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            var files = operation.Request?.Info.TryGet<List<Dictionary<string, object?>>>("FilesRequest");
            if (files is null)
            {
                throw new InvalidOperationException("Can't get files from OperationRequest.");
            }

            var items = new Dictionary<string, object>();
            await using (SessionContext.Create(this.session.CreateNestedSessionToken(tokenInfo.UserID, tokenInfo.UserName)))
            {
                foreach ((string key, string value) in parameters.Values)
                {
                    var fileStorage = files.Find(x => x.TryGet<string>("CacheID") == key);
                    if (fileStorage is null)
                    {
                        throw new InvalidOperationException($"Can't find file with CacheID={key}.");
                    }

                    var signature = Convert.FromBase64String(value);
                    var signAttributes = CAdESSignatureServerHelper.GetSignatureAttributesFromSignature(signature);
                    ThrowIfNull(signAttributes.Certificate, "Failed to get certificate from signed data.");
                    var cmsSignature = new SignatureData(signature);
                    var cadesSignature = await this.cAdESManager.ExtendSignatureAsync(signAttributes.Certificate, cmsSignature, cancellationToken);
                    var b64Signature = Convert.ToBase64String(NotNullOrThrow(cadesSignature?.Signature));
                    SignatureAttributes signAttrs = await this.edsProvider.GetSignatureAttributesFromSignatureAsync(b64Signature, cancellationToken: cancellationToken);
                    ThrowIfNull(signAttrs.Certificate, "Signing certificate not found in signed data.");

                    using (var cert = SignatureHelper.LoadCertificate(signAttrs.Certificate))
                    {
                        var subjectName = EDSCertificateHelper.GetSubjectNameAdvanced(cert.Subject);
                        var issuerName = EDSCertificateHelper.ParseSubject(cert.Issuer, EDSCertificateHelper.IssuerNameFindString);
                        var company = EDSCertificateHelper.ParseSubject(cert.Subject, EDSCertificateHelper.CompanyFindString);

                        var certData = new CertDataInOperationResponse
                        {
                            Company = company,
                            SubjectName = subjectName,
                            SerialNumber = cert.SerialNumber,
                            IssuerName = issuerName,
                            ValidFrom = GetUnixTimeSeconds(cert.NotBefore),
                            ValidTo = GetUnixTimeSeconds(cert.NotAfter),
                            Certificate = Convert.ToBase64String(signAttrs.Certificate, 0, signAttrs.Certificate.Length),
                            Thumbprint = cert.Thumbprint,
                            Comment = null
                        };

                        items[key] = new Dictionary<string, object> {
                            ["CertData"] = certData,
                            ["SignedData"] = new SignedDataResult
                            {
                                Signature = b64Signature,
                                Type = cadesSignature.SignatureType,
                                Profile = cadesSignature.SignatureProfile
                            }.ToSerializedDictionary()
                        };
                    }
                }
            }

            return new OperationResponse
            {
                Info =
                {
                    ["Type"] = "Enhance",
                    ["Items"] = items
                }
            };
        }

        /// <summary>
        /// Формирование OperationResponse c результатами проверки подписи всех файлов в операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="parameters">Объект, в котором содержится информация о сигнатурах подписи для обогащения.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>OperationResponse c результатами проверки подписи всех файлов в операции.</returns>
        public async ValueTask<OperationResponse> GetOperationResponseForVerifyAsync(
            TokenInfo tokenInfo, 
            DeskiMobileVerifyRequest parameters,
            CancellationToken cancellationToken = default)
        {
            if (!tokenInfo.Access.HasFlag(DeskiMobileTokenPermissionFlags.Verify))
            {
                throw new InvalidOperationException("The JWT token does not have permission to verify signatures.");
            }

            var operationID = tokenInfo.OperationID;
            var operation = await this.operationRepository.TryGetAsync(operationID, cancellationToken: cancellationToken);
            ValidateOperation(operation);

            var files = operation.Request?.Info.TryGet<List<Dictionary<string, object?>>>("FilesRequest");
            if (files is null)
            {
                throw new InvalidOperationException("Can't get files from OperationRequest.");
            }
            
            var items = new Dictionary<string, object>();
            await using (SessionContext.Create(this.session.CreateNestedSessionToken(tokenInfo.UserID, tokenInfo.UserName)))
            {
                foreach ((string key, Dictionary<string, string> value) in parameters.Values)
                {
                    var fileStorage = files.Find(x => x.TryGet<string>("CacheID") == key);
                    if (fileStorage is null)
                    {
                        throw new InvalidOperationException($"Can't find file with CacheID={key}.");
                    }

                    var infos = new List<SignatureValidationInfoItemsResult>();
                    foreach ((string keyID, string signature) in value)
                    {
                        ThrowIfNullOrEmpty(signature);

                        var validateResult = await this.edsProvider.ValidateDocumentAsync(
                            signature, SignatureType.None, SignatureProfile.Bes, null, cancellationToken: cancellationToken);

                        // TODO #2097 добавить html представление подписи для отображения в TESSA Assistant.
                        const string htmlText = "";
                        string sanitizedText = string.IsNullOrEmpty(htmlText) ? string.Empty : (this.htmlSanitizer?.SanitizeDocument(htmlText) ?? htmlText);

                        infos.Add(
                            new SignatureValidationInfoItemsResult
                            {
                                ID = keyID,
                                Html = sanitizedText,
                                ValidationInfo = validateResult.Select(v => v.ToSerializedDictionary()).ToList()
                            });
                    }

                    items[key] = new Dictionary<string, object> {
                        ["SignatureValidationInfo"] = infos,
                        ["SignatureValidationData"] = new SignatureValidationDataResult { ShowValidationDialog = true }.ToSerializedDictionary()
                    };
                }
            }

            return new OperationResponse
            {
                Info =
                {
                    ["Type"] = "Verify",
                    ["Items"] = items
                }
            };
        }

        /// <summary>
        /// Формирование OperationResponse при отмене операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        public OperationResponse GetOperationResponseForCancel(
            TokenInfo tokenInfo)
        {

            if (!tokenInfo.Access.HasFlag(DeskiMobileTokenPermissionFlags.CancelOperation))
            {
                throw new InvalidOperationException("The JWT token does not have permission to cancel operation.");
            }
            
            return new OperationResponse
            {
                Info =
                {
                    ["Type"] = "Cancel",
                    ["StatusData"] = new StatusDataResult { Canceled = true }.ToSerializedDictionary()
                }
            };
        }             

        #endregion

        #region Private methods

        /// <summary>
        /// Формирование доступных операций для операции.
        /// </summary>
        /// <param name="operation">Тип выполняемой операции.</param>
        /// <returns>Флаг с доступными методами для входной операции.</returns>
        private DeskiMobileTokenPermissionFlags GetTokenPermissionFlags(string operation)
        {
            var flags = DeskiMobileTokenPermissionFlags.None;
            switch (operation.ToLowerInvariant())
            {
                case "sign":
                    flags = DeskiMobileTokenPermissionFlags.GetContent |
                        DeskiMobileTokenPermissionFlags.Enhance |
                        DeskiMobileTokenPermissionFlags.CancelOperation;
                    break;

                case "verify":
                    flags = DeskiMobileTokenPermissionFlags.GetContent |
                        DeskiMobileTokenPermissionFlags.GetSignatures |
                        DeskiMobileTokenPermissionFlags.Verify |
                        DeskiMobileTokenPermissionFlags.CancelOperation;
                    break;

                case "preview":
                    flags = DeskiMobileTokenPermissionFlags.GetContent;
                    break;
            }

            return flags;
        }

        /// <summary>
        /// Получение всех полей из карточки с настройками сервера.
        /// </summary>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Настройки сервера.</returns>
        private async ValueTask<Dictionary<string, object?>> GetServerInstancesFieldsAsync(CancellationToken cancellationToken = default)
        {
            var card = await this.cardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, cancellationToken);
            return card.GetValue().Sections["ServerInstances"].RawFields;
        }

        /// <summary>
        /// Получение времени жизни JWT токена, используемого для взаимодействия с мобильным приложением, из карточки с настройками сервера.
        /// </summary>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Время жизни JWT токена.</returns>
        private async ValueTask<DateTime> GetDeskiMobileJwtLifeTimeAsync(CancellationToken cancellationToken = default)
        {
            var fields = await this.GetServerInstancesFieldsAsync(cancellationToken);
            return fields.Get<DateTime>("DeskiMobileJwtLifeTime");
        }

        /// <summary>
        /// Валидация операции.
        /// </summary>
        /// <param name="operation">Содержимое операции.</param>
        private void ValidateOperation([NotNull] IOperation? operation)
        {
            if (operation is null)
            {
                throw new InvalidOperationException("Operation is null. Server may have canceled the operation earlier. Please, restart the operation.");
            }

            ThrowIfNull(operation.Request);
        }

        private static string GetUnixTimeSeconds(DateTime dt) => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)).ToUnixTimeSeconds().ToString();
        
        #endregion
    }
}
