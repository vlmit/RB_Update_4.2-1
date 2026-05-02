<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="604aa12f-9c39-4d79-a418-15061e29e1f4" Name="WysiwygSubForms" Group="WysiwygEditor">
	<SchemePhysicalColumn ID="ca5fd7fe-ef95-475d-9376-d6bb57adb978" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="a158a839-41c0-47c5-9b7e-4b2044b8789a" Name="Alias" Type="String(128) Not Null" />
	<SchemePhysicalColumn ID="1b494e67-6ea7-4663-88d7-78108466bc49" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="ce0e5c83-41be-48a4-8ce0-2e520e9a3c2a" Name="Child" Type="Json Not Null" />
	<SchemePhysicalColumn ID="ac47d42f-0e42-4168-a8a5-cf79812a2488" Name="ItemID" Type="String(8) Not Null" />
	<SchemePrimaryKey ID="154f1021-17db-4a95-aaf0-3e239fc78fea" Name="pk_WysiwygSubForms">
		<SchemeIndexedColumn Column="1b494e67-6ea7-4663-88d7-78108466bc49" />
	</SchemePrimaryKey>
</SchemeTable>