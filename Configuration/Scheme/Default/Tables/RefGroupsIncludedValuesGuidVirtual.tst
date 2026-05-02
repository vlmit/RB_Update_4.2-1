<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="3c8a0a8b-6c00-44c2-88d8-dfab3b3ae43d" Name="RefGroupsIncludedValuesGuidVirtual" Group="RefGroups" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>"Включаемые значения и группы" - набор значений и групп, которые необходимо включить в список значений группы. Для ключей типа Guid.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="3c8a0a8b-6c00-00c2-2000-0fab3b3ae43d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3c8a0a8b-6c00-01c2-4000-0fab3b3ae43d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="3c8a0a8b-6c00-00c2-3100-0fab3b3ae43d" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="5a657033-37ce-4cb2-89d5-6f8df9729312" Name="Value" Type="Reference(Abstract) Not Null" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="5a657033-37ce-00b2-4000-0f8df9729312" Name="ValueID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="4608c8f5-b52f-4fdc-9a64-29d78ffc8be0" Name="ValueName" Type="String(128) Null" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="3c8a0a8b-6c00-00c2-5000-0fab3b3ae43d" Name="pk_RefGroupsIncludedValuesGuidVirtual">
		<SchemeIndexedColumn Column="3c8a0a8b-6c00-00c2-3100-0fab3b3ae43d" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="3c8a0a8b-6c00-00c2-7000-0fab3b3ae43d" Name="idx_RefGroupsIncludedValuesGuidVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="3c8a0a8b-6c00-01c2-4000-0fab3b3ae43d" />
	</SchemeIndex>
</SchemeTable>