<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="faf18963-f52c-498a-b975-b7e16f759b2e" Name="RefGroupTypesGuidValues" Group="RefGroups" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="faf18963-f52c-008a-2000-07e16f759b2e" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="faf18963-f52c-018a-4000-07e16f759b2e" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="faf18963-f52c-008a-3100-07e16f759b2e" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="f7ec12c1-d669-4332-9d3a-0b3a8ea1f922" Name="Value" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="e89e6fa8-9bed-4f20-b126-68ba038072c6" Name="Name" Type="String(128) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="faf18963-f52c-008a-5000-07e16f759b2e" Name="pk_RefGroupTypesGuidValues">
		<SchemeIndexedColumn Column="faf18963-f52c-008a-3100-07e16f759b2e" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="faf18963-f52c-008a-7000-07e16f759b2e" Name="idx_RefGroupTypesGuidValues_ID" IsClustered="true">
		<SchemeIndexedColumn Column="faf18963-f52c-018a-4000-07e16f759b2e" />
	</SchemeIndex>
</SchemeTable>