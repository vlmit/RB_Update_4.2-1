<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="992c83aa-1f04-415c-be10-e7dcc10b4eb5" Name="KrSigningActionHiddenFileCategoriesVirtual" Group="KrWe" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Действие "Подписание". Таблица с категориями файлов, скрытых для подписания ЭП.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="992c83aa-1f04-005c-2000-07dcc10b4eb5" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="992c83aa-1f04-015c-4000-07dcc10b4eb5" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="992c83aa-1f04-005c-3100-07dcc10b4eb5" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="1ed954a5-18d8-4d6f-b38b-73e3da08e39e" Name="FileCategory" Type="Reference(Typified) Not Null" ReferencedTable="e1599715-02d4-4ca9-b63e-b4b1ce642c7a">
		<Description>Категория файла.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1ed954a5-18d8-006f-4000-03e3da08e39e" Name="FileCategoryID" Type="Guid Not Null" ReferencedColumn="e1599715-02d4-01a9-4000-04b1ce642c7a" />
		<SchemeReferencingColumn ID="2e8a5cfe-4dce-4e9f-90a3-d8958ae552b3" Name="FileCategoryName" Type="String(255) Not Null" ReferencedColumn="e2598c40-038d-4af4-9907-f2514170cc4d" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="992c83aa-1f04-005c-5000-07dcc10b4eb5" Name="pk_KrSigningActionHiddenFileCategoriesVirtual">
		<SchemeIndexedColumn Column="992c83aa-1f04-005c-3100-07dcc10b4eb5" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="992c83aa-1f04-005c-7000-07dcc10b4eb5" Name="idx_KrSigningActionHiddenFileCategoriesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="992c83aa-1f04-015c-4000-07dcc10b4eb5" />
	</SchemeIndex>
</SchemeTable>