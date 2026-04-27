<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="d58b9b45-6399-4554-93d3-d75bc71b09af" Name="Palettes" Group="System" InstanceType="Cards" ContentType="Collections">
	<Description>Системные палитры</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="d58b9b45-6399-0054-2000-075bc71b09af" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d58b9b45-6399-0154-4000-075bc71b09af" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="d58b9b45-6399-0054-3100-075bc71b09af" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="9b50539a-7f88-40ad-b1f2-dd1cf53c57be" Name="Alias" Type="String(128) Not Null">
		<Description>Алиас палитры</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="15da9543-e4b1-4505-8cf3-53d88bdce8ae" Name="Caption" Type="String(128) Not Null">
		<Description>Заголовок палитры</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="843ddae7-c31b-45d1-b76d-0cb2d5d5c16f" Name="Color1" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="5d80940b-bc9d-44de-8d28-54ef7e41522b" Name="Color2" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="7423a59f-69b8-4ddd-8fbb-f1428b013a77" Name="Color3" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="d2e18263-af3f-4553-8f2c-c4fe46539868" Name="Color4" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="6762f82e-244b-44d8-9fe8-36b296f29a0f" Name="Color5" Type="Int32 Not Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="d58b9b45-6399-0054-5000-075bc71b09af" Name="pk_Palettes">
		<SchemeIndexedColumn Column="d58b9b45-6399-0054-3100-075bc71b09af" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="0bee9ced-962a-4809-8036-f9510b517df6" Name="ndx_Palettes_Alias">
		<SchemeIndexedColumn Column="9b50539a-7f88-40ad-b1f2-dd1cf53c57be" SortOrder="Ascending" />
	</SchemeUniqueKey>
	<SchemeUniqueKey ID="538bdb99-c8a3-4f5f-b32f-f884a2577094" Name="ndx_Palettes_Caption">
		<SchemeIndexedColumn Column="15da9543-e4b1-4505-8cf3-53d88bdce8ae" SortOrder="Ascending" />
	</SchemeUniqueKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="d58b9b45-6399-0054-7000-075bc71b09af" Name="idx_Palettes_ID" IsClustered="true">
		<SchemeIndexedColumn Column="d58b9b45-6399-0154-4000-075bc71b09af" />
	</SchemeIndex>
</SchemeTable>