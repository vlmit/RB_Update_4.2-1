<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="b31d2f6f-7980-4686-8029-3abd969ee11b" Name="KrAddFromTemplateSettingsVirtual" Group="KrStageTypes" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="b31d2f6f-7980-0086-2000-0abd969ee11b" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b31d2f6f-7980-0186-4000-0abd969ee11b" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="6cc9e9e1-ffeb-4706-ab65-68184c62f2d2" Name="Name" Type="String(Max) Null" />
	<SchemeComplexColumn ID="99935616-d608-4270-baad-91443bbcf8f2" Name="FileTemplate" Type="Reference(Typified) Not Null" ReferencedTable="98e0c3a9-0b9a-4fec-9843-4a077f6ff5f0">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="99935616-d608-0070-4000-01443bbcf8f2" Name="FileTemplateID" Type="Guid Not Null" ReferencedColumn="98e0c3a9-0b9a-01ec-4000-0a077f6ff5f0" />
		<SchemeReferencingColumn ID="18047848-7bc9-45c5-8e11-3ab7ad836721" Name="FileTemplateName" Type="String(256) Not Null" ReferencedColumn="db93e6bd-9e6a-4232-bf8c-bfe652e5573c" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="030c0416-f6dc-4bbf-8607-e88e3ecb955c" Name="FileCategory" Type="Reference(Typified) Null" ReferencedTable="e1599715-02d4-4ca9-b63e-b4b1ce642c7a">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="030c0416-f6dc-00bf-4000-088e3ecb955c" Name="FileCategoryID" Type="Guid Null" ReferencedColumn="e1599715-02d4-01a9-4000-04b1ce642c7a" />
		<SchemeReferencingColumn ID="a308712b-edd1-43fa-88a8-979eaf2a15bf" Name="FileCategoryName" Type="String(255) Null" ReferencedColumn="e2598c40-038d-4af4-9907-f2514170cc4d" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="b31d2f6f-7980-0086-5000-0abd969ee11b" Name="pk_KrAddFromTemplateSettingsVirtual" IsClustered="true">
		<SchemeIndexedColumn Column="b31d2f6f-7980-0186-4000-0abd969ee11b" />
	</SchemePrimaryKey>
</SchemeTable>