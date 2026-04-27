<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="436af7d9-c1dd-4e1a-b00e-5cf06c6d7111" Name="WidgetTemplatesKinds" Group="Dashboards">
	<Description>Виды шаблонов виджетов</Description>
	<SchemePhysicalColumn ID="79d8c2c8-02c4-471a-91c1-9fa1a3bea25a" Name="ID" Type="Int32 Not Null">
		<Description>Идентификатор вида</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="7c589f36-b472-48de-8208-6ce21a948af8" Name="Name" Type="String(64) Not Null">
		<Description>Название вида</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="17e16f86-bfe3-4f8c-8452-7ebe44122e7d" Name="pk_WidgetTemplatesKinds">
		<SchemeIndexedColumn Column="79d8c2c8-02c4-471a-91c1-9fa1a3bea25a" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="e094d413-e1a6-4f6f-9b7f-3f7e51f67f84" Name="ndx_WidgetTemplatesKinds_Name">
		<SchemeIndexedColumn Column="7c589f36-b472-48de-8208-6ce21a948af8" />
	</SchemeUniqueKey>
	<SchemeRecord>
		<ID ID="79d8c2c8-02c4-471a-91c1-9fa1a3bea25a">0</ID>
		<Name ID="7c589f36-b472-48de-8208-6ce21a948af8">$Enum_WidgetTemplatesKinds_Template</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="79d8c2c8-02c4-471a-91c1-9fa1a3bea25a">1</ID>
		<Name ID="7c589f36-b472-48de-8208-6ce21a948af8">$Enum_WidgetTemplatesKinds_Shared</Name>
	</SchemeRecord>
</SchemeTable>