<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="59f08c9a-1149-44d7-aa15-64f44917c16f" Name="ApprovalProcessActionApprovedLinks" Group="ApprovalProcess" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="59f08c9a-1149-00d7-2000-04f44917c16f" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="59f08c9a-1149-01d7-4000-04f44917c16f" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="59f08c9a-1149-00d7-3100-04f44917c16f" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="32a652b9-5abc-4f75-93dc-6c7ff246e102" Name="Link" Type="Reference(Abstract) Not Null" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="32a652b9-5abc-0075-4000-0c7ff246e102" Name="LinkID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="f49d56ff-9a04-45f6-be31-ad109899f2ce" Name="LinkName" Type="String(Max) Not Null" />
		<SchemePhysicalColumn ID="d6776da5-f55a-421d-8124-4d5442ac4f86" Name="LinkCaption" Type="String(Max) Not Null" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="59f08c9a-1149-00d7-5000-04f44917c16f" Name="pk_ApprovalProcessActionApprovedLinks">
		<SchemeIndexedColumn Column="59f08c9a-1149-00d7-3100-04f44917c16f" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="59f08c9a-1149-00d7-7000-04f44917c16f" Name="idx_ApprovalProcessActionApprovedLinks_ID" IsClustered="true">
		<SchemeIndexedColumn Column="59f08c9a-1149-01d7-4000-04f44917c16f" />
	</SchemeIndex>
</SchemeTable>