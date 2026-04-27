<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="871e395a-8f86-465a-963f-b5b05086ef4e" Name="KrSigningStageSettingsHiddenFileCategoriesVirtual" Group="KrStageTypes" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Действие "Подписание". Таблица с категориями файлов, скрытых для подписания ЭП.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="871e395a-8f86-005a-2000-05b05086ef4e" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="871e395a-8f86-015a-4000-05b05086ef4e" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="871e395a-8f86-005a-3100-05b05086ef4e" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="b958203b-6eb3-4c90-ac5d-f5a8f92becb7" Name="FileCategory" Type="Reference(Typified) Not Null" ReferencedTable="e1599715-02d4-4ca9-b63e-b4b1ce642c7a">
		<Description>Категория файла.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b958203b-6eb3-0090-4000-05a8f92becb7" Name="FileCategoryID" Type="Guid Not Null" ReferencedColumn="e1599715-02d4-01a9-4000-04b1ce642c7a" />
		<SchemeReferencingColumn ID="0e21899c-7746-421d-aea7-514a581688da" Name="FileCategoryName" Type="String(255) Not Null" ReferencedColumn="e2598c40-038d-4af4-9907-f2514170cc4d" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="871e395a-8f86-005a-5000-05b05086ef4e" Name="pk_KrSigningStageSettingsHiddenFileCategoriesVirtual">
		<SchemeIndexedColumn Column="871e395a-8f86-005a-3100-05b05086ef4e" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="871e395a-8f86-005a-7000-05b05086ef4e" Name="idx_KrSigningStageSettingsHiddenFileCategoriesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="871e395a-8f86-015a-4000-05b05086ef4e" />
	</SchemeIndex>
</SchemeTable>