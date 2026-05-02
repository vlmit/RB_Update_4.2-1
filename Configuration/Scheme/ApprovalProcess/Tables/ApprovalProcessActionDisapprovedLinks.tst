<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="1a8788db-7279-420d-8976-b6d04a921d2b" Name="ApprovalProcessActionDisapprovedLinks" Group="ApprovalProcess" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a8788db-7279-000d-2000-06d04a921d2b" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1a8788db-7279-010d-4000-06d04a921d2b" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a8788db-7279-000d-3100-06d04a921d2b" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="a6fb6c84-59fc-496f-8340-98237b4fc1b6" Name="Link" Type="Reference(Abstract) Not Null" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a6fb6c84-59fc-006f-4000-08237b4fc1b6" Name="LinkID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="150d0ec5-84f4-4d03-9a1c-366a921ba725" Name="LinkName" Type="String(Max) Not Null" />
		<SchemePhysicalColumn ID="d5d6be89-6d11-40d8-b368-79d1b7a93f31" Name="LinkCaption" Type="String(Max) Not Null" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a8788db-7279-000d-5000-06d04a921d2b" Name="pk_ApprovalProcessActionDisapprovedLinks">
		<SchemeIndexedColumn Column="1a8788db-7279-000d-3100-06d04a921d2b" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a8788db-7279-000d-7000-06d04a921d2b" Name="idx_ApprovalProcessActionDisapprovedLinks_ID" IsClustered="true">
		<SchemeIndexedColumn Column="1a8788db-7279-010d-4000-06d04a921d2b" />
	</SchemeIndex>
</SchemeTable>