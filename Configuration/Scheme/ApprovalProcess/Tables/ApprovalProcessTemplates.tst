<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="5cfc5ed0-ba1e-4068-8385-c2e7e8178a93" Name="ApprovalProcessTemplates" Group="ApprovalProcess" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="5cfc5ed0-ba1e-0068-2000-02e7e8178a93" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="5cfc5ed0-ba1e-0168-4000-02e7e8178a93" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="08289207-172a-4899-ba7c-4f47967af3a4" Name="Name" Type="String(128) Not Null" />
	<SchemePhysicalColumn ID="1659cd18-5cba-450d-b37b-41ce44d9e7e9" Name="TemplateData" Type="BinaryJson Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="6fbe88ec-938a-43a1-b4ae-1b5e4188093f" Name="df_ApprovalProcessTemplates_TemplateData" Value="{}" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="43b0d8f4-da73-48c5-8c49-0d2923868161" Name="Author" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="43b0d8f4-da73-00c5-4000-0d2923868161" Name="AuthorID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
		<SchemeReferencingColumn ID="56f6385c-8491-4b39-b578-a33807dd56a5" Name="AuthorName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="0089e31f-dd3e-486f-bb77-9ae4323792c1" Name="Description" Type="String(Max) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="5cfc5ed0-ba1e-0068-5000-02e7e8178a93" Name="pk_ApprovalProcessTemplates" IsClustered="true">
		<SchemeIndexedColumn Column="5cfc5ed0-ba1e-0168-4000-02e7e8178a93" />
	</SchemePrimaryKey>
</SchemeTable>