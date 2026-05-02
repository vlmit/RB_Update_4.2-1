<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="08dfa806-532d-462a-959f-5c864af55f02" Name="BusinessProcessButtonToolbarVisibilityModes" Group="WorkflowEngine">
	<Description>Режимы отображения кнопок бизнес-процессов на тулбаре.</Description>
	<SchemePhysicalColumn ID="f516ae6d-c644-4a68-b4ca-20d162510516" Name="ID" Type="Int32 Not Null">
		<Description>Идентификатор</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="6d25a78b-9ae8-45e2-85e6-e45a2986725a" Name="Name" Type="String(Max) Not Null">
		<Description>Наименование</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="b3d99fb7-c6c7-467a-b32e-18c5c98e23b2" Name="pk_BusinessProcessButtonToolbarVisibilityModes">
		<SchemeIndexedColumn Column="f516ae6d-c644-4a68-b4ca-20d162510516" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="f516ae6d-c644-4a68-b4ca-20d162510516">0</ID>
		<Name ID="6d25a78b-9ae8-45e2-85e6-e45a2986725a">$WorkflowEngine_BusinessProcessButtonToolbarVisibilityModes_DoNotShowOnToolbar</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="f516ae6d-c644-4a68-b4ca-20d162510516">1</ID>
		<Name ID="6d25a78b-9ae8-45e2-85e6-e45a2986725a">$WorkflowEngine_BusinessProcessButtonToolbarVisibilityModes_ShowOnToolbar</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="f516ae6d-c644-4a68-b4ca-20d162510516">2</ID>
		<Name ID="6d25a78b-9ae8-45e2-85e6-e45a2986725a">$WorkflowEngine_BusinessProcessButtonToolbarVisibilityModes_ShowOnToolbarWithoutText</Name>
	</SchemeRecord>
</SchemeTable>