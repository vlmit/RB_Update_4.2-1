<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="d36554bc-ed72-4d07-bede-71c38e0a6dbc" Name="RefGroupsIncludedValuesIntVirtual" Group="RefGroups" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>"Включаемые значения и группы" - набор значений и групп, которые необходимо включить в список значений группы. Для ключей типа Int.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="d36554bc-ed72-0007-2000-01c38e0a6dbc" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d36554bc-ed72-0107-4000-01c38e0a6dbc" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="d36554bc-ed72-0007-3100-01c38e0a6dbc" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="fbe3d4d0-6059-4af7-b3ce-34079b50b6d9" Name="Value" Type="Reference(Typified) Not Null" ReferencedTable="7de9f2cf-1244-4686-9687-63657f3f2d8a" WithForeignKey="false">
		<SchemePhysicalColumn ID="f24c52c1-ecab-4529-9987-66f9b24affe5" Name="ValueName" Type="String(128) Null" />
		<SchemeReferencingColumn ID="20aba588-8c6d-427b-86be-d07d01b3c461" Name="ValueID" Type="Int32 Not Null" ReferencedColumn="7410125d-83e5-4674-bc26-86e923d210da" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="d36554bc-ed72-0007-5000-01c38e0a6dbc" Name="pk_RefGroupsIncludedValuesIntVirtual">
		<SchemeIndexedColumn Column="d36554bc-ed72-0007-3100-01c38e0a6dbc" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="d36554bc-ed72-0007-7000-01c38e0a6dbc" Name="idx_RefGroupsIncludedValuesIntVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="d36554bc-ed72-0107-4000-01c38e0a6dbc" />
	</SchemeIndex>
</SchemeTable>