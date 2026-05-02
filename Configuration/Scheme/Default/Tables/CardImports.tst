<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c9d6869c-1223-46bc-9984-7ad781d9dbef" Name="CardImports" Group="System" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c9d6869c-1223-00bc-2000-0ad781d9dbef" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c9d6869c-1223-01bc-4000-0ad781d9dbef" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
  <SchemePhysicalColumn ID="5b22fdc0-413c-4456-ae63-c5dec0970a45" Name="StateID" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="9af3cfec-6125-4406-b208-a2c1a04eea2d" Name="df_CardImports_StateID" Value="0" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="55027e40-871a-4408-aa74-6a1d367e7736" Name="CreatedBy" Type="Reference(Typified) Not Null" IsVirtual="true" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="55027e40-871a-0008-4000-0a1d367e7736" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
		<SchemeReferencingColumn ID="a8280cac-702d-4b2d-9dfb-a039eaaaf1be" Name="CreatedByName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="38bae606-091a-4af3-99a1-8357019b58bb" Name="Created" Type="DateTime Not Null" IsVirtual="true" />
	<SchemePhysicalColumn ID="782b2bf9-c038-4631-9be5-96c613e9eff2" Name="SkipIfCardsExists" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="e8a8923a-21d1-4fdd-a8cd-c98a810741c7" Name="df_CardImports_SkipIfCardsExists" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e332684b-8d69-45a2-9df0-5e80ea6cda96" Name="ImportedCount" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="12ddbb18-8e6f-4892-9636-f62be0f56af4" Name="df_CardImports_ImportedCount" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="0717e54e-176a-4844-9feb-bb892b8bc8f7" Name="WarningCount" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="cd2efd75-8cfd-4d11-976e-8c88218967ca" Name="df_CardImports_WarningCount" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="80ef824e-763a-41d6-9ddf-2a5ffc0b299b" Name="SkippedCount" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="e71de98f-07d4-4e70-83e5-e8a7ef1ba9a2" Name="df_CardImports_SkippedCount" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="4084cf62-24b9-4548-b75c-86b706a6a187" Name="ErrorCount" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="7118cd78-e104-4eec-b792-11ea7e5aee80" Name="df_CardImports_ErrorCount" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="189d49c1-1dfc-4230-b149-791f3c2033af" Name="TotalCount" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="6ca6b3e7-bc4d-43ae-bc1c-a579da67113c" Name="df_CardImports_TotalCount" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="480c3696-bfb3-4e4e-a4fc-e1193f86fd24" Name="CardsInfo" Type="BinaryJson Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="c9d6869c-1223-00bc-5000-0ad781d9dbef" Name="pk_CardImports" IsClustered="true">
		<SchemeIndexedColumn Column="c9d6869c-1223-01bc-4000-0ad781d9dbef" />
	</SchemePrimaryKey>
</SchemeTable>