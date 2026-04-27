<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="1d350f24-c25b-495c-9704-28e02a412631" Name="ApprovalProcessTemplateWriters" Group="ApprovalProcess" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1d350f24-c25b-005c-2000-08e02a412631" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1d350f24-c25b-015c-4000-08e02a412631" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1d350f24-c25b-005c-3100-08e02a412631" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="e80843b9-b486-4c3d-822b-8f8c82a609c3" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e80843b9-b486-003d-4000-0f8c82a609c3" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="04fb64be-797a-4b29-8361-9a410592bcb0" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="1d350f24-c25b-005c-5000-08e02a412631" Name="pk_ApprovalProcessTemplateWriters">
		<SchemeIndexedColumn Column="1d350f24-c25b-005c-3100-08e02a412631" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="1d350f24-c25b-005c-7000-08e02a412631" Name="idx_ApprovalProcessTemplateWriters_ID" IsClustered="true">
		<SchemeIndexedColumn Column="1d350f24-c25b-015c-4000-08e02a412631" />
	</SchemeIndex>
</SchemeTable>