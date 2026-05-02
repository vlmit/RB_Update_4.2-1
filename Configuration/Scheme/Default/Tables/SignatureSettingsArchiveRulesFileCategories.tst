<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="fdc4968a-cd6c-4122-a786-0dc52d0d0f42" Name="SignatureSettingsArchiveRulesFileCategories" Group="System" InstanceType="Cards" ContentType="Collections">
	<Description>Категории файлов, к которым применяется правило.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="fdc4968a-cd6c-0022-2000-0dc52d0d0f42" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="fdc4968a-cd6c-0122-4000-0dc52d0d0f42" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="fdc4968a-cd6c-0022-3100-0dc52d0d0f42" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="d9bff968-0f2c-47e8-9bd1-7f18daf73321" Name="ArchiveRule" Type="Reference(Typified) Not Null" ReferencedTable="9cb70cb0-ef71-4546-9308-b7043af02e5d" IsReferenceToOwner="true">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d9bff968-0f2c-00e8-4000-0f18daf73321" Name="ArchiveRuleRowID" Type="Guid Not Null" ReferencedColumn="9cb70cb0-ef71-0046-3100-07043af02e5d" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="588f018a-bcf8-4897-96dd-7db0745c634b" Name="Category" Type="Reference(Typified) Not Null" ReferencedTable="e1599715-02d4-4ca9-b63e-b4b1ce642c7a">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="588f018a-bcf8-0097-4000-0db0745c634b" Name="CategoryID" Type="Guid Not Null" ReferencedColumn="e1599715-02d4-01a9-4000-04b1ce642c7a" />
		<SchemeReferencingColumn ID="b17c7f5b-7481-47bf-ab0a-397420d7b99f" Name="CategoryName" Type="String(255) Not Null" ReferencedColumn="e2598c40-038d-4af4-9907-f2514170cc4d" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="fdc4968a-cd6c-0022-5000-0dc52d0d0f42" Name="pk_SignatureSettingsArchiveRulesFileCategories">
		<SchemeIndexedColumn Column="fdc4968a-cd6c-0022-3100-0dc52d0d0f42" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="fdc4968a-cd6c-0022-7000-0dc52d0d0f42" Name="idx_SignatureSettingsArchiveRulesFileCategories_ID" IsClustered="true">
		<SchemeIndexedColumn Column="fdc4968a-cd6c-0122-4000-0dc52d0d0f42" />
	</SchemeIndex>
</SchemeTable>