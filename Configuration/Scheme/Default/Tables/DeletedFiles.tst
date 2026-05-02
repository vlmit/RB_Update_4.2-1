<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="71fe0eb4-d04d-4141-aa3c-8210209bfb8f" Name="DeletedFiles" Group="System">
	<Description>Информация об удалённых файлах</Description>
	<SchemePhysicalColumn ID="c2b789a2-ded7-4a23-b963-4c72071846ed" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор карточки, к которой относится файл</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="a67204f3-0709-4186-a101-88932accfbd0" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор файла</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="0e1390d5-132c-47cf-a847-6051ef55ddba" Name="Name" Type="String(256) Not Null">
		<Description>Имя файла</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="c2f40c2c-1314-4a0d-8780-3a3fdd394b59" Name="Task" Type="Reference(Typified) Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8" WithForeignKey="false">
		<Description>Внимание! Колонка не используется и может быть удалена в одном из следующих релизов. Для хранения сторонних идентификаторов используйте одну из колонок: Files.Options, FileVersions.LinkID, FileVersions.Options.

Ссылка на задание, к которому приложен файл, или Null, если файл приложен к основной карточке.

Если добавить внешний ключ в этой колонке, то также необходим индекс на неё, иначе при очень большом количестве файлов завершение/удаление любых заданий значительно замедляется.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c2f40c2c-1314-000d-4000-0a3fdd394b59" Name="TaskID" Type="Guid Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8">
			<Description>Идентификатор задания</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="024a8195-ccd9-4296-9170-9b95a35252d1" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="b0538ece-8468-4d0b-8b4e-5a1d43e024db" WithForeignKey="false" NormalizationSourceID="35531951-3d12-4a93-b7f7-e9826a6e4875">
		<Description>Тип файла</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="024a8195-ccd9-0096-4000-0b95a35252d1" Name="TypeID" Type="Guid Not Null" ReferencedColumn="a628a864-c858-4200-a6b7-da78c8e6e1f4">
			<Description>Идентификатор типа</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="041a5c47-18e8-429a-a51f-fc1f90e8f089" Name="TypeCaption" Type="String(128) Not Null" ReferencedColumn="0a02451e-2e06-4001-9138-b4805e641afa">
			<Description>Заголовок типа</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="a1ec61da-4669-4616-a107-26fa073cb60e" Name="Version" Type="Reference(Typified) Not Null" ReferencedTable="c31cd2e5-5671-4eff-a0af-7c884ddd5e43" WithForeignKey="false">
		<Description>Актуальная версия файла</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a1ec61da-4669-0016-4000-06fa073cb60e" Name="VersionRowID" Type="Guid Not Null" ReferencedColumn="ed6f0dfd-5ece-4d17-b286-b0d3812ab016">
			<Description>Идентификатор версии</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="3eb6ecf7-fc9b-4776-99e6-90a566ce01f9" Name="VersionNumber" Type="Int32 Not Null" ReferencedColumn="1471998b-d2ad-4234-9a57-a2ef8f29224d">
			<Description>Номер версии</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="df88cd6c-2a87-440d-8912-25d9b3a64463" Name="Created" Type="DateTime Not Null">
		<Description>Дата создания файла</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="7a1f16a5-9c03-4105-ab90-e4d1190ddd48" Name="CreatedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" NormalizationSourceID="77e9c8bb-bb2f-4636-99df-c91a0c07c72c">
		<Description>Пользователь, создавший файл</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7a1f16a5-9c03-0005-4000-04d1190ddd48" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>Идентификатор пользователя</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="fa250bb2-87c8-4edc-a671-3934c0425a04" Name="CreatedByName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964">
			<Description>Отображаемое имя пользователя</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="f4ae5f17-1fa3-4cb8-b08e-c35595b0fda5" Name="Modified" Type="DateTime Not Null">
		<Description>Дата изменения файла</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="8efe02cc-872d-4e76-b0dd-f748f16c2400" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" NormalizationSourceID="77e9c8bb-bb2f-4636-99df-c91a0c07c72c">
		<Description>Пользователь, изменивший файл</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="8efe02cc-872d-0076-4000-0748f16c2400" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>Идентификатор пользователя</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="cc986da1-eb4c-49ef-a8b2-9d5ece6c46a0" Name="ModifiedByName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964">
			<Description>Отображаемое имя пользователя</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="796f128b-362a-4d71-98c0-44f8e839adda" Name="Category" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Категория файла или NULL, если файл не имеет категории.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="796f128b-362a-0071-4000-04f8e839adda" Name="CategoryID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор категории</Description>
		</SchemeReferencingColumn>
		<SchemePhysicalColumn ID="8114b23d-e935-489c-869d-4acdd7e772fb" Name="CategoryCaption" Type="String(256) Null">
			<Description>Отображаемое имя категории файла или NULL, если файл не имеет категории</Description>
		</SchemePhysicalColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="115267d7-53df-456a-84af-9034337ef821" Name="OriginalFile" Type="Reference(Typified) Null" ReferencedTable="dd716146-b177-4920-bc90-b1196b16347c" WithForeignKey="false">
		<Description>Ссылка на файл, копией которого является текущий файл, или NULL, если текущий файл не является копией.

Колонка указывается без FK, т.к. файл, на который держится ссылка, может быть уже удалён.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="115267d7-53df-006a-4000-0034337ef821" Name="OriginalFileID" Type="Guid Null" IsSparse="true" ReferencedColumn="dd716146-b177-0020-3100-01196b16347c">
			<Description>Идентификатор файла</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="a23616af-92ca-44dd-9612-e212d5f44555" Name="OriginalVersion" Type="Reference(Typified) Null" ReferencedTable="e17fd270-5c61-49af-955d-ed6bb983f0d8" WithForeignKey="false">
		<Description>Ссылка на версию файла, копией которого является текущий файл, или Null, если текущий файл не является копией.

Колонка указывается без FK, т.к. файл, на версию которого держится ссылка, может быть уже удалён.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a23616af-92ca-00dd-4000-0212d5f44555" Name="OriginalVersionRowID" Type="Guid Null" IsSparse="true" ReferencedColumn="e17fd270-5c61-00af-3100-0d6bb983f0d8">
			<Description>Идентификатор версии файла</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="3579d4a2-c190-4051-b6aa-3f12f443439f" Name="Options" Type="BinaryJson Null" IsSparse="true">
		<Description>Сериализованные в JSON настройки файла</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3aba18c2-06d4-4274-8905-4fb16547fa8f" Name="Deleted" Type="DateTime Not Null">
		<Description>Дата и время удаления файла</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="d58a2834-6561-449a-9dee-425fddb395a0" Name="DeletedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" NormalizationSourceID="77e9c8bb-bb2f-4636-99df-c91a0c07c72c">
		<Description>Сотрудник, удаливший файл</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d58a2834-6561-009a-4000-025fddb395a0" Name="DeletedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>Идентификатор сотрудника, удалившего файл</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="5ef6a99c-958e-41ea-a076-442fa1f66bbf" Name="DeletedByName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964">
			<Description>Имя сотрудника, удалившего файл</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="cc6f625e-63a8-4317-aaee-322043654b46" Name="DeletedFrom" Type="Reference(Abstract) Not Null" WithForeignKey="false">
		<Description>Ссылка на карточку, из которой был удалён файл</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="cc6f625e-63a8-0017-4000-022043654b46" Name="DeletedFromID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор карточки, из которой был удалён файл</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey ID="bcb2b296-fa4a-407f-b010-f7a974aa697b" Name="pk_DeletedFiles">
		<SchemeIndexedColumn Column="a67204f3-0709-4186-a101-88932accfbd0" />
	</SchemePrimaryKey>
	<SchemeIndex ID="e61c70c1-8ccf-4168-9213-59b026df67af" Name="ndx_DeletedFiles_ID">
		<SchemeIndexedColumn Column="c2b789a2-ded7-4a23-b963-4c72071846ed" />
	</SchemeIndex>
	<SchemeIndex ID="67d4b3f1-99f6-4ed4-a754-4342be4c048b" Name="ndx_DeletedFiles_DeletedFromID">
		<SchemeIndexedColumn Column="cc6f625e-63a8-0017-4000-022043654b46" />
		<SchemeIncludedColumn Column="a67204f3-0709-4186-a101-88932accfbd0" />
	</SchemeIndex>
</SchemeTable>