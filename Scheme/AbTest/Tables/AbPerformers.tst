<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="5c4efc57-f311-4292-9cb1-7be629984589" ID="f3886f6b-24cc-421e-802f-73f0eda24d1d" Name="AbPerformers" Group="AbTest" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="f3886f6b-24cc-001e-2000-03f0eda24d1d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="f3886f6b-24cc-011e-4000-03f0eda24d1d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="f3886f6b-24cc-001e-3100-03f0eda24d1d" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="f5026c2c-c5ee-4a94-acbb-354e5e43d647" Name="User" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" NormalizationSourceID="77e9c8bb-bb2f-4636-99df-c91a0c07c72c">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="f5026c2c-c5ee-0094-4000-054e5e43d647" Name="UserID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
		<SchemeReferencingColumn ID="dd2da147-0363-4c9a-b39b-116b240f7d4c" Name="UserName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="f3886f6b-24cc-001e-5000-03f0eda24d1d" Name="pk_AbPerformers">
		<SchemeIndexedColumn Column="f3886f6b-24cc-001e-3100-03f0eda24d1d" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="f3886f6b-24cc-001e-7000-03f0eda24d1d" Name="idx_AbPerformers_ID" IsClustered="true">
		<SchemeIndexedColumn Column="f3886f6b-24cc-011e-4000-03f0eda24d1d" />
	</SchemeIndex>
</SchemeTable>