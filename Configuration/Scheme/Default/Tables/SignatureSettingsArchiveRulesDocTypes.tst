<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="0d33fa3a-db0c-48e6-9763-4361deacbce5" Name="SignatureSettingsArchiveRulesDocTypes" Group="System" InstanceType="Cards" ContentType="Collections">
	<Description>Типы карточек, к которым применяется правило.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="0d33fa3a-db0c-00e6-2000-0361deacbce5" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="0d33fa3a-db0c-01e6-4000-0361deacbce5" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="0d33fa3a-db0c-00e6-3100-0361deacbce5" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="aedea7ff-9b5f-4037-9e1d-16f2058384b6" Name="ArchiveRule" Type="Reference(Typified) Not Null" ReferencedTable="9cb70cb0-ef71-4546-9308-b7043af02e5d" IsReferenceToOwner="true">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="aedea7ff-9b5f-0037-4000-06f2058384b6" Name="ArchiveRuleRowID" Type="Guid Not Null" ReferencedColumn="9cb70cb0-ef71-0046-3100-07043af02e5d" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="c3587a60-202b-4940-99b6-e5c0bfd0bdcd" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="a90baecf-c9ce-4cba-8bb0-150a13666266">
		<Description>Типы карточек, к которым применяется правило.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c3587a60-202b-0040-4000-05c0bfd0bdcd" Name="TypeID" Type="Guid Not Null" ReferencedColumn="a90baecf-c9ce-01ba-4000-050a13666266" />
		<SchemeReferencingColumn ID="c0bf7a04-f4af-4edd-8c9b-1770c973c01c" Name="TypeCaption" Type="String(128) Not Null" ReferencedColumn="447f7cb1-76ae-4703-b3bb-16a57d4e7ab1" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="0d33fa3a-db0c-00e6-5000-0361deacbce5" Name="pk_SignatureSettingsArchiveRulesDocTypes">
		<SchemeIndexedColumn Column="0d33fa3a-db0c-00e6-3100-0361deacbce5" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="0d33fa3a-db0c-00e6-7000-0361deacbce5" Name="idx_SignatureSettingsArchiveRulesDocTypes_ID" IsClustered="true">
		<SchemeIndexedColumn Column="0d33fa3a-db0c-01e6-4000-0361deacbce5" />
	</SchemeIndex>
</SchemeTable>