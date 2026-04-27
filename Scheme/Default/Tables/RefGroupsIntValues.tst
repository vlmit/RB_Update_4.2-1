<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="1a48067e-f814-4aa1-84a8-4d6a50b64ccf" Name="RefGroupsIntValues" Group="RefGroups" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a48067e-f814-00a1-2000-0d6a50b64ccf" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1a48067e-f814-01a1-4000-0d6a50b64ccf" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a48067e-f814-00a1-3100-0d6a50b64ccf" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="6da687b3-93d2-47aa-85a6-a2a6dd7f2401" Name="Name" Type="String(128) Not Null" />
	<SchemePhysicalColumn ID="8a445add-f30e-4140-b154-620b90e5063a" Name="Value" Type="Int32 Not Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a48067e-f814-00a1-5000-0d6a50b64ccf" Name="pk_RefGroupsIntValues">
		<SchemeIndexedColumn Column="1a48067e-f814-00a1-3100-0d6a50b64ccf" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="1a48067e-f814-00a1-7000-0d6a50b64ccf" Name="idx_RefGroupsIntValues_ID" IsClustered="true">
		<SchemeIndexedColumn Column="1a48067e-f814-01a1-4000-0d6a50b64ccf" />
	</SchemeIndex>
</SchemeTable>