<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="baea7dbe-e1cd-47aa-ac66-1365c7fe562a" Name="StampPlaces" Group="PdfAnnotations" InstanceType="Cards" ContentType="Collections">
	<Description>Описание положения штампа на странице</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="baea7dbe-e1cd-00aa-2000-0365c7fe562a" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="baea7dbe-e1cd-01aa-4000-0365c7fe562a" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="baea7dbe-e1cd-00aa-3100-0365c7fe562a" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="f05a900e-5f79-4df5-bd55-a35d776942d3" Name="Place" Type="BinaryJson Not Null" />
	<SchemePhysicalColumn ID="77af9de9-3459-4c4e-8a0a-0424ec201c35" Name="Name" Type="String(Max) Not Null">
		<Description>Название расположения штампа</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="8d4a6525-13bb-477c-9c21-6a082f510314" Name="File" Type="Binary(Max) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="baea7dbe-e1cd-00aa-5000-0365c7fe562a" Name="pk_StampPlaces">
		<SchemeIndexedColumn Column="baea7dbe-e1cd-00aa-3100-0365c7fe562a" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="baea7dbe-e1cd-00aa-7000-0365c7fe562a" Name="idx_StampPlaces_ID" IsClustered="true">
		<SchemeIndexedColumn Column="baea7dbe-e1cd-01aa-4000-0365c7fe562a" />
	</SchemeIndex>
</SchemeTable>