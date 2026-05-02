<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c31cd2e5-5671-4eff-a0af-7c884ddd5e43" Name="DeletedFileVersions" Group="System">
	<Description>Информация об удалённых версиях файла</Description>
	<SchemePhysicalColumn ID="5fda971c-4a7c-4e7b-8fc4-d0d133b98fa9" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор файла</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ed6f0dfd-5ece-4d17-b286-b0d3812ab016" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор версии файла</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1471998b-d2ad-4234-9a57-a2ef8f29224d" Name="Number" Type="Int32 Not Null">
		<Description>Номер версии файла</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="bb4547db-4b63-4feb-9210-5200979bf8c9" Name="Name" Type="String(256) Not Null">
		<Description>Имя файла</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="5bb19625-44d7-4123-8891-e8726bb67547" Name="Size" Type="Int64 Not Null">
		<Description>Размер контента файла в байтах</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ea7fa4bf-6673-4ea1-93cc-72565e7bd66b" Name="Created" Type="DateTime Not Null">
		<Description>Дата обновления файла (фактически дата добавления версии)</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="e6d70a53-b655-4381-a73e-3608b1929f9e" Name="CreatedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" NormalizationSourceID="77e9c8bb-bb2f-4636-99df-c91a0c07c72c">
		<Description>Пользователь, изменивший файл (создавший его новую версию)</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e6d70a53-b655-0081-4000-0608b1929f9e" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>Идентификатор пользователя</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="35b7eafe-ec62-4f55-9483-07d5a1a809d1" Name="CreatedByName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964">
			<Description>Отображаемое имя пользователя</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="103a93d3-6287-4efe-bb87-0e2625dd6966" Name="Source" Type="Reference(Typified) Not Null" ReferencedTable="e8300fe5-3b24-4c27-a45a-6cd8575bfcd5" WithForeignKey="false">
		<Description>Способ хранения контента файла</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="103a93d3-6287-00fe-4000-0e2625dd6966" Name="SourceID" Type="Int16 Not Null" ReferencedColumn="983cbcc4-c185-43fd-a57c-edef94a23551">
			<Description>Идентификатор способа хранения контента файла</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="1c24ff52-3da2-4760-8e19-4cf70d78e4f9" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="de9ba182-3fc4-4f20-9060-fa83b74fd46c">
		<Description>Состояние версии файла</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1c24ff52-3da2-0060-4000-0cf70d78e4f9" Name="StateID" Type="Int16 Not Null" ReferencedColumn="a658b07f-0314-4974-8fda-8cff054dbe7d" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="20d6aa58-179c-41d4-ae2c-532571eae1f5" Name="ErrorDate" Type="DateTime Null" IsSparse="true">
		<Description>Дата ошибки</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="43374bf5-f12f-454a-b458-e5043a5a0f16" Name="ErrorMessage" Type="String(Max) Null" IsSparse="true">
		<Description>Сообщение об ошибке</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="4a7cd613-59e3-4024-8960-f0f9025074dc" Name="Hash" Type="Binary(32) Null">
		<Description>Хеш, посчитанный для контента файла, или NULL, если расчёт хеша не выполнен. Хеш расчитывается вручную на клиенте. Обычно используется для файлов приложений.

Для расчёта обычно используется функция хеширования SHA256, размер хеша в которой 256 бит или 32 байта.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="10926f5c-78b9-4c2e-b953-6718077b8760" Name="Options" Type="BinaryJson Null" IsSparse="true">
		<Description>Сериализованные в JSON настройки версии файла.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="62abd3bc-629e-4c73-8739-b2633e76e595" Name="LinkID" Type="Guid Null" IsSparse="true">
		<Description>Внешний идентификатор версии файла. Может использоваться в расширениях для связи с содержимым во внешнем местоположении.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1a3ae16e-d111-426c-ae14-4a810ef84b01" Name="Tags" Type="String(256) Null" IsSparse="true">
		<Description>Теги версии файла</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="22be60b7-eab0-40eb-9ff8-c2ed076b2837" Name="pk_DeletedFileVersions">
		<SchemeIndexedColumn Column="ed6f0dfd-5ece-4d17-b286-b0d3812ab016" />
	</SchemePrimaryKey>
	<SchemeIndex ID="6a2012ec-9de5-4868-8679-efee7384f966" Name="ndx_DeletedFileVersions_ID">
		<SchemeIndexedColumn Column="5fda971c-4a7c-4e7b-8fc4-d0d133b98fa9" />
	</SchemeIndex>
</SchemeTable>