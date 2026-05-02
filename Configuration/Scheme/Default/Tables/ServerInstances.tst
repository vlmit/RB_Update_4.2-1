<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c3d76e97-459f-41e0-8d45-56fb19b5e07e" Name="ServerInstances" Group="System" InstanceType="Cards" ContentType="Entries">
	<Description>Таблица с настройками базы данных.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c3d76e97-459f-00e0-2000-06fb19b5e07e" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c3d76e97-459f-01e0-4000-06fb19b5e07e" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="de844fc3-e8b7-4061-bc4e-7e4cf78826ed" Name="Name" Type="String(128) Not Null">
		<Description>Название экземпляра сервера.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c27c3bdf-005b-4e10-a8b9-6d8e32e29243" Name="Description" Type="String(Max) Null">
		<Description>Текстовое описание экземпляра сервера. Необязательно для заполнения, произвольные комментарии.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="34058de0-0e1f-4b84-b150-9e0a8442701a" Name="DefaultFileSource" Type="Reference(Typified) Null" ReferencedTable="e8300fe5-3b24-4c27-a45a-6cd8575bfcd5" WithForeignKey="false">
		<Description>Источник файлов, используемый в системе по умолчанию.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="34058de0-0e1f-0084-4000-0e0a8442701a" Name="DefaultFileSourceID" Type="Int16 Null" ReferencedColumn="983cbcc4-c185-43fd-a57c-edef94a23551">
			<Description>Идентификатор способа хранения файлов.</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="c65e5e97-e89e-454d-9d39-1cbeef09883f" Name="WebAddress" Type="String(128) Null">
		<Description>Базовый адрес веб-клиента.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="751c0cf4-1497-467a-b559-6a2cd7c1c920" Name="MobileApprovalEmail" Type="String(128) Not Null">
		<Description>Адрес электронной почты, на который пользователи отправляют ответы с действиями для мобильного согласования.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="9ccba90b-0ce2-4d8d-be03-3719bc00c9dc" Name="df_ServerInstances_MobileApprovalEmail" Value="mobile-approval@domain.name.com" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c5265247-c244-46e0-92c3-21efac34e528" Name="ViewGetDataCommandTimeout" Type="Int32 Not Null">
		<Description>Таймаут выполнения представлений по умолчанию, в миллисекундах.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="0b416a41-cf46-4040-bc12-4a66b4aa6a74" Name="df_ServerInstances_ViewGetDataCommandTimeout" Value="300" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="8ef7d7b4-b517-4d36-92b0-55fd576d4433" Name="FileExtensionsWithoutPreview" Type="String(Max) Not Null">
		<Description>Расширения файлов, предпросмотр которых не выполняется. Несколько расширений указывается без точки и разделяется пробелом.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="55b4778d-b8c3-49c5-b7fe-d6ea99405cb3" Name="df_ServerInstances_FileExtensionsWithoutPreview" Value="7z cab com dll exe gz iso jar rar tar wim zip zipx" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e334f45d-1880-4eb9-b280-ac4f2a96c641" Name="FileExtensionsConvertablePreview" Type="String(Max) Null">
		<Description>Расширения файлов, предпросмотр которых выполняется с конвертацией. Несколько расширений указывается без точки и разделяется пробелом.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="cb59bd94-4e97-4b35-9618-5f4bc2361faa" Name="DenyMobileFileDownload" Type="Boolean Not Null">
		<Description>Возможность запретить скачивание файлов на мобильных устройствах</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="ad3c6d17-0e3d-4580-8ab7-afd35531a0ce" Name="df_ServerInstances_DenyMobileFileDownload" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f917b22c-48f2-4efe-9967-083b71e0c15b" Name="BlockWindowsAndLdapUsers" Type="Boolean Not Null">
		<Description>Выполнять блокировку сотрудников с типом входа Windows и LDAP</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="d12a343e-7c89-4d60-a34f-1574bba720ca" Name="df_ServerInstances_BlockWindowsAndLdapUsers" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="95330ccf-68b3-45b2-894b-1a5a9a51fc62" Name="CsvEncoding" Type="String(64) Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="e2bee3a6-39a3-4c44-b0cd-912bbb1dfece" Name="df_ServerInstances_CsvEncoding" Value="windows-1251" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="86e7487b-e508-443c-aad0-2ad2fd5bf58c" Name="CsvSeparator" Type="String(6) Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="aab9cc64-4dc3-4e89-b120-4d428c1b6877" Name="df_ServerInstances_CsvSeparator" Value=";" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c42dc4cb-4fe9-452f-97f1-1d8a9002f07e" Name="MaxFailedLoginAttemptsBeforeBlocked" Type="Int32 Not Null">
		<Description>Максимальное число разрешённых неудачных попыток входа до того, как произойдёт блокировка пользователя (поля Blocked, BlockedDueDate в карточке сотрудника). По умолчанию 0 - проверка отключена.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="a4354ad9-efb9-499b-9153-a195b24a5c58" Name="df_ServerInstances_MaxFailedLoginAttemptsBeforeBlocked" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e788ed15-1844-47d1-866a-607276bb7d2b" Name="MaxFailedLoginAttemptsInSeries" Type="Int32 Not Null">
		<Description>Максимальное число разрешённых неудачных попыток входа в серии (промежуток времени между попытками меньше заданного) до того, как произойдёт блокировка пользователя (поля Blocked, BlockedDueDate в карточке сотрудника). По умолчанию 0 - проверка отключена.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="bd40c5b0-6e26-4735-baaa-7871ff7ebe59" Name="df_ServerInstances_MaxFailedLoginAttemptsInSeries" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="9c9d17be-9183-4ee8-8dc7-a56c252b8252" Name="BlockedSeriesDueDateHours" Type="Double Not Null">
		<Description>Количество часов, на которое выполняется блокировка при превышении неудачных попыток MaxFailedLoginAttemptsInSeries. По умолчанию 15 минут: 0.25.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="a031c051-2466-4c9f-8811-180cfc745ddc" Name="df_ServerInstances_BlockedSeriesDueDateHours" Value="0.25" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="8a145670-c472-4e02-b1b3-04f5978841ce" Name="FailedLoginAttemptsSeriesTime" Type="Time Not Null">
		<Description>Максимальное время между неудачными попытками, чтобы считать их частью серии. По умолчанию 5 минут.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="b77df637-e201-405d-87cf-356aaa2615d6" Name="df_ServerInstances_FailedLoginAttemptsSeriesTime" Value="PT5M" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="fdacb0b3-143b-4aa5-82f6-34f6fb0bc194" Name="SessionInactivityHours" Type="Double Null">
		<Description>Время неактивности сессии в часах, проверяемое каждый раз при выполнении запроса, связанного с сессией.
Если с даты последней активности до текущей даты прошло время больше заданного, то возвращается исключение.
По умолчанию Null - время неактивности неограничено.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="5046f2ce-897e-4a19-a664-d59a77fc673b" Name="MinPasswordLength" Type="Int32 Not Null">
		<Description>Минимальная длина пароля, вводимого пользователем. Проверяется при изменении пароля пользователем (не администратором). По умолчанию: 4.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="c313b7d8-5f0c-4007-b0e8-89757d6f1ebd" Name="df_ServerInstances_MinPasswordLength" Value="4" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="663cb1c7-0edb-48a0-acf4-24964dd5a6c4" Name="EnforceStrongPasswords" Type="Boolean Not Null">
		<Description>Признак того, что пароль, вводимый пользователем, должен содержать спец. символы, цифры и разные регистры символов.
Проверяется при изменении пароля пользователем (не администратором). По умолчанию False.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="49cb3382-e190-4cab-b85b-370aee201238" Name="df_ServerInstances_EnforceStrongPasswords" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e085ca56-09bc-479d-89a0-2999318242af" Name="PasswordExpirationDays" Type="Double Null">
		<Description>Количество дней, в течение которых пароль действует с момента установки пароля (поле PersonalRoles.PasswordChanged).
Если пароль прекращает действовать, то при входе в систему возвращается исключение. По умолчанию Null - проверка отключена.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="8f72d369-f41f-4910-af41-fdd3a7c7b2d5" Name="PasswordExpirationNotificationDays" Type="Double Null">
		<Description>Количество дней, оставшихся до окончания срока действия паролей у пользователей, которым отправляются уведомления с рекомендацией сменить пароль.
По умолчанию Null - уведомления отключены.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f0667111-180a-453c-a55a-37aa2c1a0c3f" Name="UniquePasswordCount" Type="Int32 Not Null">
		<Description>Количество паролей пользователя, для которых проверяется, что они уникальны (не повторяются). Проверяется при изменении пароля пользователем. По умолчанию 1.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="e86bcae7-57e3-43e5-9c7b-48e1e533b829" Name="df_ServerInstances_UniquePasswordCount" Value="1" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f9eaa28d-b949-4a94-a206-7afafc3e7128" Name="MaxFileSizeMb" Type="Int32 Null">
		<Description>Максимальный размер файла в Мб, который можно загрузить со стороны клиента.
Укажите Null, если отсутствуют ограничения на загрузку файлов со стороны клиента.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="060c447d-02b8-43cb-aff4-779349e1a458" Name="LargeFileSizeMb" Type="Int32 Null">
		<Description>Минимальный размер файла в Мб для того, чтобы считаться большим файлом, для которого применяются особые правила загрузки.
Укажите Null, если любые файлы, независимо от размера, не будут считаться большими.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="adffcb81-9a75-4daf-a9c5-6eda05448f69" Name="df_ServerInstances_LargeFileSizeMb" Value="500" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ac1aa1ad-c087-49fa-b3f8-f00b2dea8d73" Name="WebDefaultWallpaper" Type="String(Max) Null">
		<Description>Дефолтный корпоративный фон для ЛК</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d25f0c33-cb00-4e59-892e-b915696233d6" Name="ModifyMessageAtNoOlderThan" Type="Int32 Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="7c3ea9bc-6e06-409b-a572-37e60a25d9c9" Name="df_ServerInstances_ModifyMessageAtNoOlderThan" Value="60" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="2203a83d-f40c-4f77-928c-778afb967aae" Name="FullTextMessageSearch" Type="Boolean Not Null">
		<Description>Полнотекстовый поиск по сообщениям</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="a36ea884-d158-4b3f-a91b-4825a0f31f5a" Name="df_ServerInstances_FullTextMessageSearch" Value="true" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="6e96782b-2f4c-4699-b03a-9df92fff0d0b" Name="HelpUrl" Type="String(Max) Null">
		<Description>Ссылка на веб-страницу со справкой. Если равна NULL или пустой строке, то кнопка со справкой скрывается.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="7cb008c1-3e52-4dd0-af3f-b784ee84e386" Name="UseNewDeputies" Type="Boolean Not Null">
		<Description>Флаг определяет, нужно ли использовать новую подсистему замещения.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="3236b4a0-0f78-44d6-876a-86a6f2d8b50c" Name="df_ServerInstances_UseNewDeputies" Value="true" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="66fee506-448f-4d5f-b363-4246705dd724" Name="DefaultCalendar" Type="Reference(Typified) Null" ReferencedTable="67b1fd42-0106-4b31-a368-ea3e4d38ac5c">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="66fee506-448f-005f-4000-0246705dd724" Name="DefaultCalendarID" Type="Guid Null" ReferencedColumn="67b1fd42-0106-0131-4000-0a3e4d38ac5c" />
		<SchemeReferencingColumn ID="174ed360-725c-4a10-b141-a00b1cb06d3d" Name="DefaultCalendarName" Type="String(255) Null" ReferencedColumn="a593a14d-d146-4071-aaa7-215307755c58" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="68f67a81-dcfc-43b8-adba-41f287d4f0e0" Name="UseRemainingTimeInAstronomicalDays" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="5cdfe6d3-c477-4704-8e90-4533c47b3481" Name="df_ServerInstances_UseRemainingTimeInAstronomicalDays" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="23e42daf-6d5e-4f8c-8702-f564c8a35b57" Name="ForumMaxAttachedFileSizeKb" Type="Int32 Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="6daaa04c-0e0b-4976-b6e1-ab2a1aaf2bdd" Name="df_ServerInstances_ForumMaxAttachedFileSizeKb" Value="300" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="57d7a5c7-6c1f-4028-ab3f-df34d59aa178" Name="ForumMaxAttachedFiles" Type="Int32 Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="15f6c5a3-ecbe-4202-a14e-48ed58a677aa" Name="df_ServerInstances_ForumMaxAttachedFiles" Value="10" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="31f7792c-2dbe-4008-91cb-91d0af28ca03" Name="ForumMaxMessageInlines" Type="Int32 Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="455a7b82-a580-418a-a84b-06bbd67205ea" Name="df_ServerInstances_ForumMaxMessageInlines" Value="10" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="834676ff-dfb8-4c5a-b4fb-8a101915a022" Name="ForumMaxMessageSize" Type="Int32 Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="70774d02-9ddb-491a-b7aa-aafd831a9c05" Name="df_ServerInstances_ForumMaxMessageSize" Value="1000" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="7173f500-37a2-4aff-8ee7-cd9e04d7580c" Name="FileConverterType" Type="Reference(Typified) Null" ReferencedTable="a1dd7426-13e0-42fb-a45a-0a714108e274">
		<Description>Тип конвертера файла</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7173f500-37a2-00ff-4000-0d9e04d7580c" Name="FileConverterTypeID" Type="Int16 Null" ReferencedColumn="51f44556-0314-496f-bd4b-4162d05b07c7" />
		<SchemeReferencingColumn ID="da3696e2-a850-4e8e-9481-8f0b05ea0667" Name="FileConverterTypeName" Type="String(128) Null" ReferencedColumn="5d525d57-ea0c-401e-b14a-81c08323b102" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="406dee1a-bc7d-4532-93e1-f57438d1ae73" Name="DefaultActionHistoryDatabase" Type="Reference(Typified) Null" ReferencedTable="db0969a9-e71d-405d-bf86-15f263cf69c8" WithForeignKey="false">
		<Description>База данных для хранения истории действий, используемая в системе по умолчанию.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="406dee1a-bc7d-0032-4000-057438d1ae73" Name="DefaultActionHistoryDatabaseID" Type="Int16 Null" ReferencedColumn="c2197dfc-e396-4d0c-9543-09fb0aee2218" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="1b2b0462-46b4-4a14-bbfe-139d9b6bd24c" Name="DeskiDisabled" Type="Boolean Not Null">
		<Description>Флаг, отвечающий за отключение Deski.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="87da8cdd-e027-46c7-bcf0-e67e61549b01" Name="df_ServerInstances_DeskiDisabled" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="5bf33e0a-4124-4e49-972d-d4a86fd1d871" Name="DeskiMobileEnabled" Type="Boolean Not Null">
		<Description>Флаг, отвечающий за включение TESSA Assistant.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="ce706686-a598-4dbc-8fda-b4526c9cac70" Name="df_ServerInstances_DeskiMobileEnabled" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="6c35ee79-cfa1-4bc3-b631-4cfb638b133e" Name="DeskiMobileJwtLifeTime" Type="Time Not Null">
		<Description>Время жизни jwt токена при взаимодействии с мобильным приложением. По умолчанию 30 минут.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="dedf18f4-d6f8-4000-a9ae-04e0a7a6df34" Name="df_ServerInstances_DeskiMobileJwtLifeTime" Value="PT30M" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="86f631c2-b59d-427a-b02f-bc146ff526c6" Name="DisableHtmlToPdfConversion" Type="Boolean Not Null">
		<Description>Отключить конвертацию HTML в PDF</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="cb2d3920-94df-46c4-91e6-274d91136c02" Name="df_ServerInstances_DisableHtmlToPdfConversion" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="bf14b923-4b88-4426-9eaf-a080f7a4239d" Name="TwoFactorAuthMode" Type="Reference(Typified) Not Null" ReferencedTable="cd2701ae-186d-426f-bdc0-e024bfd3e426">
		<Description>Режим двухфакторной аутентификации</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="bf14b923-4b88-0026-4000-0080f7a4239d" Name="TwoFactorAuthModeID" Type="Int32 Not Null" ReferencedColumn="ea903807-a197-4987-8a70-64adc2970f5b">
			<Description>Идентификатор режима</Description>
			<SchemeDefaultConstraint IsPermanent="true" ID="fa294a99-a3ff-4154-8a13-f5a4c3b92088" Name="df_ServerInstances_TwoFactorAuthModeID" Value="0" />
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="72bc50a4-e612-46a2-8db9-d8f6f6a150b6" Name="TwoFactorAuthModeName" Type="String(64) Not Null" ReferencedColumn="1426da96-a2f9-4a17-8ef1-dc78a22ba2f9">
			<Description>Название режима</Description>
			<SchemeDefaultConstraint IsPermanent="true" ID="18b42b54-9c1d-42d2-884e-b7697adede4a" Name="df_ServerInstances_TwoFactorAuthModeName" Value="$Enum_TwoFactorAuthModes_Disabled" />
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="96f9144f-cbd8-43d7-9b37-8886266a7d05" Name="MobileClientEnabled" Type="Boolean Not Null">
		<Description>Флаг, отвечающий за включение мобильного клиента.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="eb1d2d64-8597-4a92-a43a-f275b246ebc9" Name="df_ServerInstances_MobileClientEnabled" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="a3600f72-1524-49ac-9e30-e86702916145" Name="DeleteFilesWithBackup" Type="Boolean Not Null">
		<Description>Удалять файлы в корзину</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="1ace34b9-5182-4182-80af-259b6ec2e0f4" Name="df_ServerInstances_DeleteFilesWithBackup" Value="true" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="333e43fc-63cd-4113-a62e-2357dd6dd6b4" Name="DeleteFilesWithBackupPeriod" Type="Double Null">
		<Description>Срок хранения удаённых файлов в корзине (в днях).
Значение может быть дробным числом, например, 0.5 т.е. полдня.
Если значение отрицательно или NULL то срок хранения удалённых файлов не ограничен.
По умолчанию файл хранится в корзине 30 дней.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="d0ced068-88eb-467b-9a5b-a3e96671450f" Name="df_ServerInstances_DeleteFilesWithBackupPeriod" Value="30" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="29017eb3-5991-4e83-b202-2b1499de6bf5" Name="TempFileLinkMaxPeriod" Type="Double Not Null">
		<Description>Максимальный период действия ссылки на файл (дней)</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="fc3a204c-037a-4a72-82e1-1079e5e301cb" Name="df_ServerInstances_TempFileLinkMaxPeriod" Value="365" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="90a2f33a-441b-4021-8ba7-30e155f0685c" Name="ProhibitAllEmployeesSelection" Type="Boolean Not Null">
		<Description>Запретить выбор всех сотрудников для доступа к общим виджетам</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="a94478e0-73e1-428a-a4a6-f77b255feeee" Name="df_ServerInstances_ProhibitAllEmployeesSelection" Value="false" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="c3d76e97-459f-00e0-5000-06fb19b5e07e" Name="pk_ServerInstances" IsClustered="true">
		<SchemeIndexedColumn Column="c3d76e97-459f-01e0-4000-06fb19b5e07e" />
	</SchemePrimaryKey>
</SchemeTable>