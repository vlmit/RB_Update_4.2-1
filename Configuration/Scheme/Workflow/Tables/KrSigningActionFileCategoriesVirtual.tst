<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="ff98a173-90c3-46cb-bdf5-b9283fd56c7d" Name="KrSigningActionFileCategoriesVirtual" Group="KrWe" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Действие "Подписание". Таблица с категориями файлов для подписания ЭП.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="ff98a173-90c3-00cb-2000-09283fd56c7d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ff98a173-90c3-01cb-4000-09283fd56c7d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="ff98a173-90c3-00cb-3100-09283fd56c7d" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="59e41dce-dbb0-46e3-8051-582964b35216" Name="FileCategory" Type="Reference(Typified) Not Null" ReferencedTable="e1599715-02d4-4ca9-b63e-b4b1ce642c7a">
		<Description>Категория файла.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="59e41dce-dbb0-00e3-4000-082964b35216" Name="FileCategoryID" Type="Guid Not Null" ReferencedColumn="e1599715-02d4-01a9-4000-04b1ce642c7a" />
		<SchemeReferencingColumn ID="68a73312-4439-46e1-ac6d-73fb0a61e02b" Name="FileCategoryName" Type="String(255) Not Null" ReferencedColumn="e2598c40-038d-4af4-9907-f2514170cc4d" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="ff98a173-90c3-00cb-5000-09283fd56c7d" Name="pk_KrSigningActionFileCategoriesVirtual">
		<SchemeIndexedColumn Column="ff98a173-90c3-00cb-3100-09283fd56c7d" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="ff98a173-90c3-00cb-7000-09283fd56c7d" Name="idx_KrSigningActionFileCategoriesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="ff98a173-90c3-01cb-4000-09283fd56c7d" />
	</SchemeIndex>
</SchemeTable>