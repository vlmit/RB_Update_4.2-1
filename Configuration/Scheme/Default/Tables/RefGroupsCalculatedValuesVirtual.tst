<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="bf3e61e4-082c-4183-8049-1d165886f56f" Name="RefGroupsCalculatedValuesVirtual" Group="RefGroups" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Рассчитанные значения для "Группы ссылок". Доступен только для чтения. Это виртуальная секция.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="bf3e61e4-082c-0083-2000-0d165886f56f" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="bf3e61e4-082c-0183-4000-0d165886f56f" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="bf3e61e4-082c-0083-3100-0d165886f56f" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="884e4b70-a557-486f-8466-98361dc0227e" Name="Value" Type="String(Max) Not Null" />
	<SchemePhysicalColumn ID="7d7251d1-b0a2-4ee1-91bd-cb30712e7769" Name="Name" Type="String(128) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="bf3e61e4-082c-0083-5000-0d165886f56f" Name="pk_RefGroupsCalculatedValuesVirtual">
		<SchemeIndexedColumn Column="bf3e61e4-082c-0083-3100-0d165886f56f" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="bf3e61e4-082c-0083-7000-0d165886f56f" Name="idx_RefGroupsCalculatedValuesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="bf3e61e4-082c-0183-4000-0d165886f56f" />
	</SchemeIndex>
</SchemeTable>