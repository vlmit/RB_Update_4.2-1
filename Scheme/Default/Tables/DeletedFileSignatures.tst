<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c98c5b44-bd83-46fe-a68c-d4d1de020ad3" Name="DeletedFileSignatures" Group="System">
	<Description>Информация об удалённых подписях файла</Description>
	<SchemePhysicalColumn ID="f84ab81b-8958-4e9a-bb89-fa8dcd21286f" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор файла</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1dfae1a7-07da-4cb9-b06d-0d08b75da615" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор подписи</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="80cb3dc9-3699-4f39-a8ef-f41ccafa5aee" Name="Version" Type="Reference(Typified) Not Null" ReferencedTable="c31cd2e5-5671-4eff-a0af-7c884ddd5e43">
		<Description>Версия файла, которой принадлежит подпись</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="80cb3dc9-3699-0039-4000-041ccafa5aee" Name="VersionRowID" Type="Guid Not Null" ReferencedColumn="ed6f0dfd-5ece-4d17-b286-b0d3812ab016">
			<Description>Идентификатор версии файла</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="55cd3fd0-0f8b-42ac-8f5e-821811475a5c" Name="User" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<Description>Пользователь, который зарегистрировал подпись в системе, т.е. добавил строку в таблицу</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="55cd3fd0-0f8b-00ac-4000-021811475a5c" Name="UserID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>Идентификатор пользователя</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="e2d154e2-ab0f-451d-88de-df23a20773ae" Name="UserName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964">
			<Description>Отображаемое имя пользователя</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="61adbca0-3501-45f9-b100-c64363e76477" Name="Event" Type="Reference(Typified) Not Null" ReferencedTable="5a8e7767-cd46-4ace-9da3-e3ea6f38cff2">
		<Description>Событие, в результате которого подпись была добавлена.
0 - произвольная логика, определяемая в расширениях.
1 - подпись была импортирована из файла, User - это пользователь, который импортировал подпись.
2 - подпись была создана в системе, User - пользователь, который осуществлял подписывание.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="61adbca0-3501-00f9-4000-064363e76477" Name="EventID" Type="Int16 Not Null" ReferencedColumn="5c07267f-b144-4691-8334-df3d75c898da" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="cffa3a44-4da4-4bbd-98da-a1a650856856" Name="Comment" Type="String(512) Null">
		<Description>Произвольный комментарий к подписи, который может использоваться для указания источника подписи и др.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1b97c7df-b2ae-4372-9a86-8b2a77f93f03" Name="SubjectName" Type="String(256) Null">
		<Description>Получатель сертификата, указанный в файле подписи</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="395a6da5-b126-4323-bd24-63175dd80b74" Name="Company" Type="String(256) Null">
		<Description>Название компании, указанное в файле подписи</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c3288989-eac6-4af7-8ab3-74b421addf7a" Name="Signed" Type="DateTime Not Null">
		<Description>Дата и время подписи, указанная в файле подписи</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="6d6d6af1-aae6-488f-88a1-38dffee0b313" Name="SerialNumber" Type="String(128) Null">
		<Description>Серийный номер сертификата, указанный в файле с подписью</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="638f1afe-a0e8-45c7-b0a4-03981cb9bf16" Name="IssuerName" Type="String(256) Null">
		<Description>Издатель сертификата, указанный в файле с подписью</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="53e1e5d9-c2f5-4e71-b833-8842d11e0dee" Name="Data" Type="Binary(Max) Null">
		<Description>Бинарные данные файла с подписью.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="91991c98-9d6b-4fa5-97dc-16dcf1517bf3" Name="SignatureType" Type="Reference(Typified) Not Null" ReferencedTable="577baaea-6832-4eb7-9333-60661367720e">
		<Description>Вид подписи</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="91991c98-9d6b-00a5-4000-06dcf1517bf3" Name="SignatureTypeID" Type="Int16 Not Null" ReferencedColumn="dfe71de9-ef54-4eac-8f54-64d5311db556">
			<Description>Идентификатор вида подписи</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="f7ecc4e2-6e7e-4f02-a83c-1643faecabb6" Name="SignatureProfile" Type="Reference(Typified) Not Null" ReferencedTable="eca29bb9-3085-4556-b19a-6015cbc8fb25">
		<Description>Профиль цифровой подписи</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="f7ecc4e2-6e7e-0002-4000-0643faecabb6" Name="SignatureProfileID" Type="Int16 Not Null" ReferencedColumn="8c01d076-b862-4d75-852c-453efccfe590">
			<Description>Идентификатор профиля цифровой подписи</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="12d39a15-49db-4d80-8258-350b6c493443" Name="NearestCertValidTo" Type="Date Null">
		<Description>Ближайшая дата окончания срока действия сертификата из цепочки сертификатов.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="4e06e632-e2fa-4fdb-b3ae-e7b66e4304ca" Name="NearestCertSerialNumber" Type="String(128) Null">
		<Description>Серийный номер сертификата, окончание срока действия которого ближайшее из цепочки сертификатов.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="7ee68d15-7d64-401e-a488-5bd426801c41" Name="pk_DeletedFileSignatures">
		<SchemeIndexedColumn Column="1dfae1a7-07da-4cb9-b06d-0d08b75da615" />
	</SchemePrimaryKey>
	<SchemeIndex ID="7060e593-7758-4794-849f-5439204680b6" Name="ndx_DeletedFileSignatures_ID">
		<SchemeIndexedColumn Column="f84ab81b-8958-4e9a-bb89-fa8dcd21286f" />
	</SchemeIndex>
	<SchemeIndex ID="d47077ff-161e-416d-baea-d0b2687e9b4a" Name="ndx_DeletedFileSignatures_VersionRowID">
		<SchemeIndexedColumn Column="80cb3dc9-3699-0039-4000-041ccafa5aee" />
		<SchemeIncludedColumn Column="f84ab81b-8958-4e9a-bb89-fa8dcd21286f" />
		<SchemeIncludedColumn Column="55cd3fd0-0f8b-00ac-4000-021811475a5c" />
		<SchemeIncludedColumn Column="e2d154e2-ab0f-451d-88de-df23a20773ae" />
		<SchemeIncludedColumn Column="cffa3a44-4da4-4bbd-98da-a1a650856856" />
		<SchemeIncludedColumn Column="1b97c7df-b2ae-4372-9a86-8b2a77f93f03" />
		<SchemeIncludedColumn Column="395a6da5-b126-4323-bd24-63175dd80b74" />
		<SchemeIncludedColumn Column="c3288989-eac6-4af7-8ab3-74b421addf7a" />
		<SchemeIncludedColumn Column="6d6d6af1-aae6-488f-88a1-38dffee0b313" />
	</SchemeIndex>
</SchemeTable>