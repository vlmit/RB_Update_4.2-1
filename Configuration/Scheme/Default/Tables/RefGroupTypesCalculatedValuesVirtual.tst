<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="4efe2551-40c9-477e-8eb1-82525bd7cd7b" Name="RefGroupTypesCalculatedValuesVirtual" Group="RefGroups" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Рассчитанные значения для "Типа групп ссылок". Доступен только для чтения. Это виртуальная секция.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="4efe2551-40c9-007e-2000-02525bd7cd7b" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="4efe2551-40c9-017e-4000-02525bd7cd7b" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="4efe2551-40c9-007e-3100-02525bd7cd7b" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="7e086201-00f0-46a3-ad3f-6027c34f593b" Name="Value" Type="String(Max) Not Null" />
	<SchemePhysicalColumn ID="61ba59b8-fe53-4b16-8570-b06e0abe2444" Name="Name" Type="String(128) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="4efe2551-40c9-007e-5000-02525bd7cd7b" Name="pk_RefGroupTypesCalculatedValuesVirtual">
		<SchemeIndexedColumn Column="4efe2551-40c9-007e-3100-02525bd7cd7b" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="4efe2551-40c9-007e-7000-02525bd7cd7b" Name="idx_RefGroupTypesCalculatedValuesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="4efe2551-40c9-017e-4000-02525bd7cd7b" />
	</SchemeIndex>
</SchemeTable>