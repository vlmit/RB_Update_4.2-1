<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="1a33e40c-bf9c-4506-bebd-45b9c20f8512" Name="ApprovalProcessTemplateUsers" Group="ApprovalProcess" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a33e40c-bf9c-0006-2000-05b9c20f8512" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1a33e40c-bf9c-0106-4000-05b9c20f8512" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a33e40c-bf9c-0006-3100-05b9c20f8512" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="b68feea8-808c-446f-9039-2cf5f0ed5946" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b68feea8-808c-006f-4000-0cf5f0ed5946" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="2e99307f-10c0-439a-b75b-b6314fe41f33" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a33e40c-bf9c-0006-5000-05b9c20f8512" Name="pk_ApprovalProcessTemplateUsers">
		<SchemeIndexedColumn Column="1a33e40c-bf9c-0006-3100-05b9c20f8512" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a33e40c-bf9c-0006-7000-05b9c20f8512" Name="idx_ApprovalProcessTemplateUsers_ID" IsClustered="true">
		<SchemeIndexedColumn Column="1a33e40c-bf9c-0106-4000-05b9c20f8512" />
	</SchemeIndex>
</SchemeTable>