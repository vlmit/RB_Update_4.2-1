<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="7f8d57be-1ea4-4884-84d1-d7d9bfe1fa11" Name="RefGroupTypes" Group="RefGroups" InstanceType="Cards" ContentType="Entries">
	<Description>Тип значений "Группы ссылок" группы. Для механизма "Групп ссылок".</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="7f8d57be-1ea4-0084-2000-07d9bfe1fa11" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7f8d57be-1ea4-0184-4000-07d9bfe1fa11" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="949b46b6-fb6f-4e95-8624-908efdaf1db9" Name="Name" Type="String(128) Not Null">
		<Description>"Название" – название типа списка. Например "Состояния".</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="44c20867-c8d9-4b9b-b66f-869659dee618" Name="SQL" Type="String(Max) Null">
		<Description>"SQL-запрос для получения всех значений" – запрос для получения всех значений данного списка.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c9ea7543-9a88-4b85-923c-bc19aed96805" Name="RefSection" Type="String(Max) Not Null">
		<Description>"RefSection значений" - определяет список используемых RefSection, разделённых через пробел, для выбора значения данного типа. Используется в контролах "Значения" и "Исключаемые значения" в типе карточки "Группа значений".</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ccfb2b9e-0ccf-43fc-afae-510484fd5a54" Name="Script" Type="String(Max) Null">
		<Description>"Скрипт получения всех значений" - скрипт на языке C#, возвращающий список значений.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="eb922aaa-fefd-499f-bae2-0ca9f3783dc7" Name="KeyType" Type="Reference(Typified) Not Null" ReferencedTable="3b47e0b2-17ed-4ce5-9902-b4d4c02a8688">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="eb922aaa-fefd-009f-4000-0ca9f3783dc7" Name="KeyTypeID" Type="Int32 Not Null" ReferencedColumn="c27b4845-de64-403e-8112-e7cc62244b37" />
		<SchemeReferencingColumn ID="f101e1ce-3be0-4d90-aa4c-809f04a25003" Name="KeyTypeName" Type="String(128) Not Null" ReferencedColumn="3f53729c-54a4-4ba7-ac7a-4c4d119add7b" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="0fc556fb-6063-49f4-8669-ce331501cfb6" Name="Calculated" Type="DateTime Null">
		<Description>Дата и время последнего успешного расчета значений типа группы.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="9ca6bece-d180-4bd1-a14c-cb33842d6621" Name="LastError" Type="DateTime Null">
		<Description>Дата и время последней ошибки перерасчёта.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="b19c97a3-6008-4f4f-a284-faeceee42d11" Name="MessageText" Type="String(Max) Null">
		<Description>Текст ошибки или предупреждения, возникших в ходе последнего перерасчёта.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="068e165d-dc07-4947-9e6c-fb82473dc331" Name="GroupRefSection" Type="String(128) Not Null">
		<Description>RefSection группы, который используется в генерируемых представлениях как RefSection представления.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="8bb6ca13-dc10-4d22-8ff7-adc1505b4026" Name="df_RefGroupTypes_GroupRefSection" Value="RefGroups" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="fd54218e-fc4b-4003-9690-0cf1a39ac798" Name="ValueNameColumn" Type="String(128) Not Null">
		<Description>Имя колонки, в которой должно хранится имя значения.
Используется для получение имени значения из представления в группах ссылок, а также определяет имя колонки с именем группы для выбора группы как значения.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="80f71865-ef06-4e7c-a0fa-7bb14f896db3" Name="df_RefGroupTypes_ValueNameColumn" Value="Name" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="7f8d57be-1ea4-0084-5000-07d9bfe1fa11" Name="pk_RefGroupTypes" IsClustered="true">
		<SchemeIndexedColumn Column="7f8d57be-1ea4-0184-4000-07d9bfe1fa11" />
	</SchemePrimaryKey>
</SchemeTable>