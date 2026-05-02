<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c5ec452a-190e-45f0-a5bd-aae1af45d981" Name="StampsStampsPlaces" Group="PdfAnnotations" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c5ec452a-190e-00f0-2000-0ae1af45d981" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c5ec452a-190e-01f0-4000-0ae1af45d981" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c5ec452a-190e-00f0-3100-0ae1af45d981" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="84255f84-72c0-4c1e-ab6d-2794ffeddff6" Name="Stamp" Type="Reference(Typified) Not Null" ReferencedTable="a19c9ff0-df22-4827-a4f8-44e45e9119f9">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="84255f84-72c0-001e-4000-0794ffeddff6" Name="StampRowID" Type="Guid Not Null" ReferencedColumn="a19c9ff0-df22-0027-3100-04e45e9119f9" />
		<SchemeReferencingColumn ID="5f06e6bd-dccb-4c7f-83a8-d58314004b46" Name="StampName" Type="String(Max) Not Null" ReferencedColumn="5ab099ec-e1eb-48e5-a060-2e2b065c0a2a" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="ae5eb803-95e8-4d21-9a53-823ceef1fc93" Name="StampPlace" Type="Reference(Typified) Not Null" ReferencedTable="baea7dbe-e1cd-47aa-ac66-1365c7fe562a" IsReferenceToOwner="true">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ae5eb803-95e8-0021-4000-023ceef1fc93" Name="StampPlaceRowID" Type="Guid Not Null" ReferencedColumn="baea7dbe-e1cd-00aa-3100-0365c7fe562a" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="c5ec452a-190e-00f0-5000-0ae1af45d981" Name="pk_StampsStampsPlaces">
		<SchemeIndexedColumn Column="c5ec452a-190e-00f0-3100-0ae1af45d981" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="c5ec452a-190e-00f0-7000-0ae1af45d981" Name="idx_StampsStampsPlaces_ID" IsClustered="true">
		<SchemeIndexedColumn Column="c5ec452a-190e-01f0-4000-0ae1af45d981" />
	</SchemeIndex>
</SchemeTable>