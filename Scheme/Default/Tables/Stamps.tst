<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="a19c9ff0-df22-4827-a4f8-44e45e9119f9" Name="Stamps" Group="PdfAnnotations" InstanceType="Cards" ContentType="Collections">
	<Description>Секция для хранения штампов конструктора штампов</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="a19c9ff0-df22-0027-2000-04e45e9119f9" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a19c9ff0-df22-0127-4000-04e45e9119f9" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="a19c9ff0-df22-0027-3100-04e45e9119f9" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="e77f1184-3e93-4a0a-9fa0-41ac7fb537bb" Name="Annotations" Type="BinaryJson Not Null" />
	<SchemePhysicalColumn ID="5ab099ec-e1eb-48e5-a060-2e2b065c0a2a" Name="Name" Type="String(Max) Not Null" />
	<SchemePhysicalColumn ID="8a4c462d-44a0-40f2-9a4f-5000b2995aab" Name="PreviewFile" Type="Binary(Max) Null">
		<Description>File for a preview</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ab1a0de2-542e-494c-bc49-92ca5554e2f2" Name="File" Type="Binary(Max) Null">
		<Description>File for a stamping</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="a19c9ff0-df22-0027-5000-04e45e9119f9" Name="pk_Stamps">
		<SchemeIndexedColumn Column="a19c9ff0-df22-0027-3100-04e45e9119f9" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="a19c9ff0-df22-0027-7000-04e45e9119f9" Name="idx_Stamps_ID" IsClustered="true">
		<SchemeIndexedColumn Column="a19c9ff0-df22-0127-4000-04e45e9119f9" />
	</SchemeIndex>
</SchemeTable>