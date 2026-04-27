<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="20245246-2793-4a40-b3f9-94b72eef3001" Name="RefGroupTypesIntValues" Group="RefGroups" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="20245246-2793-0040-2000-04b72eef3001" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="20245246-2793-0140-4000-04b72eef3001" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="20245246-2793-0040-3100-04b72eef3001" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="560d813b-8937-460f-9fbd-e2188a54702c" Name="Value" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="71f72a35-dcfd-4fc7-958d-ea5a83a6bea3" Name="Name" Type="String(128) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="20245246-2793-0040-5000-04b72eef3001" Name="pk_RefGroupTypesIntValues">
		<SchemeIndexedColumn Column="20245246-2793-0040-3100-04b72eef3001" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="20245246-2793-0040-7000-04b72eef3001" Name="idx_RefGroupTypesIntValues_ID" IsClustered="true">
		<SchemeIndexedColumn Column="20245246-2793-0140-4000-04b72eef3001" />
	</SchemeIndex>
</SchemeTable>