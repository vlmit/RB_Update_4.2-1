<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="bc9f3fc4-21ed-4c9b-acb7-997acd12702b" Name="RefGroupsExcludedValuesIntVirtual" Group="RefGroups" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>"Исключаемые значения" – набор значений и групп, которые необходимо исключить из результата запроса SQL после его подсчёта. Для ключей типа Int.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="bc9f3fc4-21ed-009b-2000-097acd12702b" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="bc9f3fc4-21ed-019b-4000-097acd12702b" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="bc9f3fc4-21ed-009b-3100-097acd12702b" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="45ec3a9c-6ee6-42b2-85f0-27dce6dc587e" Name="Value" Type="Reference(Typified) Not Null" ReferencedTable="7de9f2cf-1244-4686-9687-63657f3f2d8a" WithForeignKey="false">
		<SchemePhysicalColumn ID="6997cda2-aebf-4720-a409-2f34e57e65ed" Name="ValueName" Type="String(128) Null" />
		<SchemeReferencingColumn ID="134a8082-62f2-4025-86fa-f93677479050" Name="ValueID" Type="Int32 Not Null" ReferencedColumn="7410125d-83e5-4674-bc26-86e923d210da" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="bc9f3fc4-21ed-009b-5000-097acd12702b" Name="pk_RefGroupsExcludedValuesIntVirtual">
		<SchemeIndexedColumn Column="bc9f3fc4-21ed-009b-3100-097acd12702b" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="bc9f3fc4-21ed-009b-7000-097acd12702b" Name="idx_RefGroupsExcludedValuesIntVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="bc9f3fc4-21ed-019b-4000-097acd12702b" />
	</SchemeIndex>
</SchemeTable>