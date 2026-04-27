<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="b0e55702-728d-42ec-a9cc-015dad02ab53" Name="RefGroupModeEnum" Group="RefGroups">
	<Description>"Режим заполнения" – список режимов заполнение группы.</Description>
	<SchemePhysicalColumn ID="1d584515-e168-46df-b0f0-2f9d80963c70" Name="ID" Type="Int32 Not Null">
		<Description>Идентификатор режима заполнения.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="4e1eeb14-dece-41a2-9aa7-eaa98ea93f79" Name="Name" Type="String(128) Not Null">
		<Description>Наименование режима заполнения.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="d4dad09c-3e3e-42a3-b5ff-073f010c1656" Name="pk_RefGroupModeEnum">
		<SchemeIndexedColumn Column="1d584515-e168-46df-b0f0-2f9d80963c70" SortOrder="Ascending" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="1d584515-e168-46df-b0f0-2f9d80963c70">0</ID>
		<Name ID="4e1eeb14-dece-41a2-9aa7-eaa98ea93f79">$RefGroupMode_Manual</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="1d584515-e168-46df-b0f0-2f9d80963c70">1</ID>
		<Name ID="4e1eeb14-dece-41a2-9aa7-eaa98ea93f79">$RefGroupMode_ValuesFromType</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="1d584515-e168-46df-b0f0-2f9d80963c70">2</ID>
		<Name ID="4e1eeb14-dece-41a2-9aa7-eaa98ea93f79">$RefGroupMode_SQL</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="1d584515-e168-46df-b0f0-2f9d80963c70">3</ID>
		<Name ID="4e1eeb14-dece-41a2-9aa7-eaa98ea93f79">$RefGroupMode_Script</Name>
	</SchemeRecord>
</SchemeTable>