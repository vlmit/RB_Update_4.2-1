<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="35f208e9-8690-4f9c-b33b-0928ed646a91" Name="UserPalettesVirtual" Group="System" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Кастомные палитры пользователя</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="35f208e9-8690-009c-2000-0928ed646a91" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="35f208e9-8690-019c-4000-0928ed646a91" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="35f208e9-8690-009c-3100-0928ed646a91" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="49c10bf6-52cc-47b4-9cf8-f3175694fab7" Name="Palette" Type="Reference(Typified) Not Null" ReferencedTable="d58b9b45-6399-4554-93d3-d75bc71b09af">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="49c10bf6-52cc-00b4-4000-03175694fab7" Name="PaletteRowID" Type="Guid Not Null" ReferencedColumn="d58b9b45-6399-0054-3100-075bc71b09af" />
		<SchemeReferencingColumn ID="99ee20e9-f518-422d-9865-e3788d4839ef" Name="PaletteCaption" Type="String(128) Not Null" ReferencedColumn="15da9543-e4b1-4505-8cf3-53d88bdce8ae" />
		<SchemeReferencingColumn ID="a90fa68d-1883-4f8c-81b6-1459cf173806" Name="PaletteAlias" Type="String(128) Not Null" ReferencedColumn="9b50539a-7f88-40ad-b1f2-dd1cf53c57be" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="e5a2fa69-e77b-4b8c-9263-58e1f15c2c15" Name="Color1" Type="Int32 Null" />
	<SchemePhysicalColumn ID="7cdd05d8-22c3-4aa6-a88a-10edf1cc5a11" Name="Color2" Type="Int32 Null" />
	<SchemePhysicalColumn ID="93f1ccb1-1075-4eed-859b-7e63d560565d" Name="Color3" Type="Int32 Null" />
	<SchemePhysicalColumn ID="be5b37ed-323a-466b-a3c9-d76475585489" Name="Color4" Type="Int32 Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="35f208e9-8690-009c-5000-0928ed646a91" Name="pk_UserPalettesVirtual">
		<SchemeIndexedColumn Column="35f208e9-8690-009c-3100-0928ed646a91" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="35f208e9-8690-009c-7000-0928ed646a91" Name="idx_UserPalettesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="35f208e9-8690-019c-4000-0928ed646a91" />
	</SchemeIndex>
</SchemeTable>