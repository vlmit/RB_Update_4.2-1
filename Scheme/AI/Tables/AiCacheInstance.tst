<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="62ec8a40-5bc5-41b3-9e8a-9f2c52fc318a" Name="AiCacheInstance" Group="AI" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="62ec8a40-5bc5-00b3-2000-0f2c52fc318a" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="62ec8a40-5bc5-01b3-4000-0f2c52fc318a" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="975cee50-69d2-4c16-a74f-01e3e42a4575" Name="LastCleanup" Type="DateTime Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="62ec8a40-5bc5-00b3-5000-0f2c52fc318a" Name="pk_AiCacheInstance" IsClustered="true">
		<SchemeIndexedColumn Column="62ec8a40-5bc5-01b3-4000-0f2c52fc318a" />
	</SchemePrimaryKey>
</SchemeTable>