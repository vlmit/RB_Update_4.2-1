<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="1f850cc2-da8e-41a9-95da-b837e4cb6c5f" Name="AiToolViewColumns" Group="AI" InstanceType="Cards" ContentType="Collections">
	<Description>Колонки представления.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1f850cc2-da8e-00a9-2000-0837e4cb6c5f" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1f850cc2-da8e-01a9-4000-0837e4cb6c5f" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="1f850cc2-da8e-00a9-3100-0837e4cb6c5f" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="9f467427-affc-4074-ac02-382c23d99a9c" Name="Column" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Колонка.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9f467427-affc-0074-4000-082c23d99a9c" Name="ColumnID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="26103baf-319f-4efa-86ff-12b5165f2c01" Name="ColumnAlias" Type="String(128) Null" />
		<SchemePhysicalColumn ID="50224bd5-2fbb-4715-841d-b70a9fa9e755" Name="ColumnCaption" Type="String(128) Null" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="1f850cc2-da8e-00a9-5000-0837e4cb6c5f" Name="pk_AiToolViewColumns">
		<SchemeIndexedColumn Column="1f850cc2-da8e-00a9-3100-0837e4cb6c5f" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="1f850cc2-da8e-00a9-7000-0837e4cb6c5f" Name="idx_AiToolViewColumns_ID" IsClustered="true">
		<SchemeIndexedColumn Column="1f850cc2-da8e-01a9-4000-0837e4cb6c5f" />
	</SchemeIndex>
</SchemeTable>