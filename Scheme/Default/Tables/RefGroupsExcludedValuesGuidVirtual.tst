<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="cafc7228-76c7-4f36-b841-5fa85d673d86" Name="RefGroupsExcludedValuesGuidVirtual" Group="RefGroups" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>"Исключаемые значения" – набор значений и групп, которые необходимо исключить из результата запроса SQL после его подсчёта. Для ключей типа Guid.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="cafc7228-76c7-0036-2000-0fa85d673d86" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="cafc7228-76c7-0136-4000-0fa85d673d86" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="cafc7228-76c7-0036-3100-0fa85d673d86" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="099bd012-e88f-4cb8-8562-37a0e3e9b3b2" Name="Value" Type="Reference(Abstract) Not Null" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="099bd012-e88f-00b8-4000-07a0e3e9b3b2" Name="ValueID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="b7c40039-37e8-41d2-9227-30ec4e729888" Name="ValueName" Type="String(128) Null" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="cafc7228-76c7-0036-5000-0fa85d673d86" Name="pk_RefGroupsExcludedValuesGuidVirtual">
		<SchemeIndexedColumn Column="cafc7228-76c7-0036-3100-0fa85d673d86" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="cafc7228-76c7-0036-7000-0fa85d673d86" Name="idx_RefGroupsExcludedValuesGuidVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="cafc7228-76c7-0136-4000-0fa85d673d86" />
	</SchemeIndex>
</SchemeTable>