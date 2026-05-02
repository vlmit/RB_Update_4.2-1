<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="9325c071-0ecf-43a8-991b-e25e3c0cb4a3" Name="KrSigningStageSettingsFileCategoriesVirtual" Group="KrStageTypes" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9325c071-0ecf-00a8-2000-025e3c0cb4a3" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9325c071-0ecf-01a8-4000-025e3c0cb4a3" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9325c071-0ecf-00a8-3100-025e3c0cb4a3" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="59c4c172-7e3b-45db-8d0f-c81277d2d9bc" Name="FileCategory" Type="Reference(Typified) Not Null" ReferencedTable="e1599715-02d4-4ca9-b63e-b4b1ce642c7a">
		<Description>Категория файла.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="59c4c172-7e3b-00db-4000-081277d2d9bc" Name="FileCategoryID" Type="Guid Not Null" ReferencedColumn="e1599715-02d4-01a9-4000-04b1ce642c7a" />
		<SchemeReferencingColumn ID="493b5b49-5f20-4227-8457-6f7022586923" Name="FileCategoryName" Type="String(255) Not Null" ReferencedColumn="e2598c40-038d-4af4-9907-f2514170cc4d" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="9325c071-0ecf-00a8-5000-025e3c0cb4a3" Name="pk_KrSigningStageSettingsFileCategoriesVirtual">
		<SchemeIndexedColumn Column="9325c071-0ecf-00a8-3100-025e3c0cb4a3" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="9325c071-0ecf-00a8-7000-025e3c0cb4a3" Name="idx_KrSigningStageSettingsFileCategoriesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="9325c071-0ecf-01a8-4000-025e3c0cb4a3" />
	</SchemeIndex>
</SchemeTable>