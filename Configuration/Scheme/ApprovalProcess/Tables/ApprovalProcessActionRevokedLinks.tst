<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="19dfd8b9-a3dd-46e4-b686-51cf08bd6e01" Name="ApprovalProcessActionRevokedLinks" Group="ApprovalProcess" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="19dfd8b9-a3dd-00e4-2000-01cf08bd6e01" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="19dfd8b9-a3dd-01e4-4000-01cf08bd6e01" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="19dfd8b9-a3dd-00e4-3100-01cf08bd6e01" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="97d3593c-bec6-42f6-b10a-491bc640c2d6" Name="Link" Type="Reference(Abstract) Not Null" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="97d3593c-bec6-00f6-4000-091bc640c2d6" Name="LinkID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="5548c7e8-1136-42de-8c72-e1a25e44aa7c" Name="LinkName" Type="String(Max) Not Null" />
		<SchemePhysicalColumn ID="96b618fb-cf11-4004-8363-7b885784d58e" Name="LinkCaption" Type="String(Max) Not Null" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="19dfd8b9-a3dd-00e4-5000-01cf08bd6e01" Name="pk_ApprovalProcessActionRevokedLinks">
		<SchemeIndexedColumn Column="19dfd8b9-a3dd-00e4-3100-01cf08bd6e01" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="19dfd8b9-a3dd-00e4-7000-01cf08bd6e01" Name="idx_ApprovalProcessActionRevokedLinks_ID" IsClustered="true">
		<SchemeIndexedColumn Column="19dfd8b9-a3dd-01e4-4000-01cf08bd6e01" />
	</SchemeIndex>
</SchemeTable>