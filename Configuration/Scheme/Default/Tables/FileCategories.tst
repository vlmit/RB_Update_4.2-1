<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="e1599715-02d4-4ca9-b63e-b4b1ce642c7a" Name="FileCategories" Group="System" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="e1599715-02d4-00a9-2000-04b1ce642c7a" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e1599715-02d4-01a9-4000-04b1ce642c7a" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="e2598c40-038d-4af4-9907-f2514170cc4d" Name="Name" Type="String(255) Not Null" />
	<SchemePhysicalColumn ID="2fe156e5-ec49-4012-af59-5763810e6039" Name="System" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="0480cfc1-f044-4257-b0c9-0f7cc7b84b34" Name="df_FileCategories_System" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="31b4bbfe-7963-4764-8b6c-1cf644afd0e6" Name="Order" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="4562a911-7787-4131-a3af-eba9031c2cf8" Name="df_FileCategories_Order" Value="0" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="e1599715-02d4-00a9-5000-04b1ce642c7a" Name="pk_FileCategories" IsClustered="true">
		<SchemeIndexedColumn Column="e1599715-02d4-01a9-4000-04b1ce642c7a" />
	</SchemePrimaryKey>
	<SchemeIndex ID="406cf721-7b79-44e1-b3e1-2fb85f360aa8" Name="ndx_FileCategories_Name" IsUnique="true">
		<SchemeIndexedColumn Column="e2598c40-038d-4af4-9907-f2514170cc4d">
			<Expression Dbms="PostgreSql">lower("Name")</Expression>
		</SchemeIndexedColumn>
	</SchemeIndex>
</SchemeTable>