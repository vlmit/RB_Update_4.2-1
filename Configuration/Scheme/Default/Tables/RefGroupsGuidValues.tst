<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="b540f2ec-d334-4899-ac32-ebb28f84f7e7" Name="RefGroupsGuidValues" Group="RefGroups" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="b540f2ec-d334-0099-2000-0bb28f84f7e7" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b540f2ec-d334-0199-4000-0bb28f84f7e7" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="b540f2ec-d334-0099-3100-0bb28f84f7e7" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="7112084a-69da-4598-8bb5-70430c3d2c4c" Name="Value" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="e432eb1c-5ec7-403f-a883-ec464076920c" Name="Name" Type="String(128) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="b540f2ec-d334-0099-5000-0bb28f84f7e7" Name="pk_RefGroupsGuidValues">
		<SchemeIndexedColumn Column="b540f2ec-d334-0099-3100-0bb28f84f7e7" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="b540f2ec-d334-0099-7000-0bb28f84f7e7" Name="idx_RefGroupsGuidValues_ID" IsClustered="true">
		<SchemeIndexedColumn Column="b540f2ec-d334-0199-4000-0bb28f84f7e7" />
	</SchemeIndex>
</SchemeTable>