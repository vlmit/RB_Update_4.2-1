<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="e9e0a007-c818-4813-8f39-c3b44fc74edd" Name="DashboardTypes" Group="Dashboards">
	<Description>Типы дашбордов</Description>
	<SchemePhysicalColumn ID="dc238801-e2f9-482c-a1f8-3555c845bce6" Name="ID" Type="Int32 Not Null">
		<Description>Идентификатор типа</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f57fef7c-1f9a-4446-9dff-1c56f76b9185" Name="Name" Type="String(128) Not Null">
		<Description>Название типа</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="64b1a331-900a-4a25-b11f-10ee2787fd74" Name="pk_DashboardTypes">
		<SchemeIndexedColumn Column="dc238801-e2f9-482c-a1f8-3555c845bce6" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="32ca8c48-b538-4dd2-bc78-c3d37b0a7afb" Name="ndx_DashboardTypes_Name">
		<SchemeIndexedColumn Column="f57fef7c-1f9a-4446-9dff-1c56f76b9185" />
	</SchemeUniqueKey>
	<SchemeRecord>
		<ID ID="dc238801-e2f9-482c-a1f8-3555c845bce6">0</ID>
		<Name ID="f57fef7c-1f9a-4446-9dff-1c56f76b9185">$Enum_DashboardTypes_Personal</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="dc238801-e2f9-482c-a1f8-3555c845bce6">1</ID>
		<Name ID="f57fef7c-1f9a-4446-9dff-1c56f76b9185">$Enum_DashboardTypes_Template</Name>
	</SchemeRecord>
</SchemeTable>