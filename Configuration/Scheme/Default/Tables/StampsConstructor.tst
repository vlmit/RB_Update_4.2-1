<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="38a6ddc0-ea7f-4051-b2dd-b0cc5c09bbb6" Name="StampsConstructor" Group="PdfAnnotations" InstanceType="Cards" ContentType="Entries">
	<Description>Основная секция для типа конструктор штампов</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="38a6ddc0-ea7f-0051-2000-00cc5c09bbb6" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="38a6ddc0-ea7f-0151-4000-00cc5c09bbb6" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="89796d56-24a7-456f-aa6b-3060d4d26a84" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="38a6ddc0-ea7f-0051-5000-00cc5c09bbb6" Name="pk_StampsConstructor" IsClustered="true">
		<SchemeIndexedColumn Column="38a6ddc0-ea7f-0151-4000-00cc5c09bbb6" />
	</SchemePrimaryKey>
</SchemeTable>