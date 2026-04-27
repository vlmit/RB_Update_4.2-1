<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="140a411c-3b68-44c0-8a5b-cf641d2421f2" Name="WorkflowEngineSettingsObjectTypes" Group="WorkflowEngine" InstanceType="Cards" ContentType="Collections">
	<Description>Секция с типами для редактора процессов</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="140a411c-3b68-00c0-2000-0f641d2421f2" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="140a411c-3b68-01c0-4000-0f641d2421f2" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="140a411c-3b68-00c0-3100-0f641d2421f2" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="90d86e92-f561-4e53-a4d4-5fba86115f3b" Name="Name" Type="String(Max) Not Null" />
	<SchemePhysicalColumn ID="826e10e0-c59b-4c8c-aead-5ac06a93080b" Name="Caption" Type="String(Max) Not Null" />
	<SchemePhysicalColumn ID="f5d5f3de-383c-45f5-b5ec-03746cf07d4e" Name="RefSection" Type="String(Max) Null" />
	<SchemeComplexColumn ID="0c76ed42-293c-4db9-8ffd-b05cad97e371" Name="Table" Type="Reference(Typified) Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="0c76ed42-293c-00b9-4000-005cad97e371" Name="TableID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="3c182a7f-86e2-45ee-8fd9-bf34791180ab" Name="TableName" Type="String(Max) Null" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="c1780b09-121a-4bba-8e8e-210ffcea500c" Name="ViewAlias" Type="String(256) Null" />
	<SchemePhysicalColumn ID="b742c7a4-72e4-408c-9bbd-d904e7b873ad" Name="IDColumn" Type="String(256) Null" />
	<SchemePhysicalColumn ID="59e7f6e9-a7fa-4326-8493-642906066859" Name="NameColumn" Type="String(256) Null" />
	<SchemePhysicalColumn ID="9ef5e93b-3dc8-4fd4-af20-ed5dcacdc059" Name="ParameterAlias" Type="String(256) Null" />
	<SchemePhysicalColumn ID="a5937f8a-c572-4a42-b293-79bedddd0c48" Name="Prefix" Type="String(256) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="140a411c-3b68-00c0-5000-0f641d2421f2" Name="pk_WorkflowEngineSettingsObjectTypes">
		<SchemeIndexedColumn Column="140a411c-3b68-00c0-3100-0f641d2421f2" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="140a411c-3b68-00c0-7000-0f641d2421f2" Name="idx_WorkflowEngineSettingsObjectTypes_ID" IsClustered="true">
		<SchemeIndexedColumn Column="140a411c-3b68-01c0-4000-0f641d2421f2" />
	</SchemeIndex>
</SchemeTable>