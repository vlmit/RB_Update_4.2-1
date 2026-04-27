<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="d3b547df-315d-4cda-a345-668428c3a14d" Name="ApprovalProcessInstances" Group="ApprovalProcess">
	<SchemePhysicalColumn ID="65623483-81df-4792-a9a3-bf17ae5ec73a" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="e6c9b4c6-d85f-486d-9e10-9a9dd8678895" Name="InstanceData" Type="BinaryJson Not Null" />
	<SchemeComplexColumn ID="605e0acf-3c49-4c21-9e5e-7f7a602d62d6" Name="Card" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="605e0acf-3c49-0021-4000-0f7a602d62d6" Name="CardID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="3d4eac7d-10ae-42b0-895f-34bdf4ac5c13" Name="CreatedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3d4eac7d-10ae-00b0-4000-04bdf4ac5c13" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="d015dce5-365b-45db-a8c2-8b8cd33f3732" Name="ExternalProcessID" Type="Guid Null" />
	<SchemePhysicalColumn ID="ff70927e-0b2e-45c0-9fa2-54edd439b938" Name="ExternalWorkflowType" Type="String(128) Null" />
	<SchemePhysicalColumn ID="57ce724f-6c33-405e-b5ba-c76d1704ab8e" Name="ExternalStartID" Type="Guid Null" />
	<SchemePhysicalColumn ID="43b8eb8a-458c-4781-b3c7-9ed75fcd5f7e" Name="Settings" Type="BinaryJson Null" />
	<SchemePhysicalColumn ID="0627c829-adbd-4789-9966-ae4528fdfe2f" Name="Created" Type="DateTime Not Null" />
	<SchemePhysicalColumn ID="9c3e3941-67ba-41f7-a7be-26a510f68f08" Name="Modified" Type="DateTime Not Null" />
	<SchemeComplexColumn ID="2afd1687-e767-4efb-9a8b-85c9d3a48363" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="7845ed70-a6bc-4b62-8d3d-7a219df2f0fb">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="2afd1687-e767-00fb-4000-05c9d3a48363" Name="StateID" Type="Int32 Not Null" ReferencedColumn="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c" />
	</SchemeComplexColumn>
	<SchemePrimaryKey ID="93d228c7-cf17-4dfb-aaad-9a05033e265b" Name="pk_ApprovalProcessInstances">
		<SchemeIndexedColumn Column="65623483-81df-4792-a9a3-bf17ae5ec73a" />
	</SchemePrimaryKey>
</SchemeTable>