<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="3b47e0b2-17ed-4ce5-9902-b4d4c02a8688" Name="RefGroupValueTypesEnum" Group="RefGroups">
	<Description>Список доступных типов значений для "Групп ссылок".</Description>
	<SchemePhysicalColumn ID="c27b4845-de64-403e-8112-e7cc62244b37" Name="ID" Type="Int32 Not Null">
		<Description>Идентификатор типа значений.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3f53729c-54a4-4ba7-ac7a-4c4d119add7b" Name="Name" Type="String(128) Not Null">
		<Description>Наименование типа значений.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="38a6bd4d-e101-4931-b0ff-5b17a4215ed3" Name="pk_RefGroupValueTypesEnum">
		<SchemeIndexedColumn Column="c27b4845-de64-403e-8112-e7cc62244b37" SortOrder="Ascending" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="c27b4845-de64-403e-8112-e7cc62244b37">0</ID>
		<Name ID="3f53729c-54a4-4ba7-ac7a-4c4d119add7b">Guid</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="c27b4845-de64-403e-8112-e7cc62244b37">1</ID>
		<Name ID="3f53729c-54a4-4ba7-ac7a-4c4d119add7b">Int32</Name>
	</SchemeRecord>
</SchemeTable>