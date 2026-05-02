<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="51f35878-52d2-4f40-8d54-5096ccbd3492" Name="TagReaders" Group="Tags" InstanceType="Cards" ContentType="Collections">
	<Description>Список ролей, которым тег доступен для перехода.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="51f35878-52d2-0040-2000-0096ccbd3492" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="51f35878-52d2-0140-4000-0096ccbd3492" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="51f35878-52d2-0040-3100-0096ccbd3492" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="79c160fd-8bbc-4c72-aae1-65931206fa5b" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b">
		<Description>Роль, для которой доступен тег.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="79c160fd-8bbc-0072-4000-05931206fa5b" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="6228b132-1f0d-4a7e-a428-154c9a9adbcc" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="51f35878-52d2-0040-5000-0096ccbd3492" Name="pk_TagReaders">
		<SchemeIndexedColumn Column="51f35878-52d2-0040-3100-0096ccbd3492" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="51f35878-52d2-0040-7000-0096ccbd3492" Name="idx_TagReaders_ID" IsClustered="true">
		<SchemeIndexedColumn Column="51f35878-52d2-0140-4000-0096ccbd3492" />
	</SchemeIndex>
	<SchemeIndex ID="723fa922-0c20-4806-8c9f-c0db7b813944" Name="ndx_TagReaders_RoleID">
		<SchemeIndexedColumn Column="79c160fd-8bbc-0072-4000-05931206fa5b" />
	</SchemeIndex>
</SchemeTable>