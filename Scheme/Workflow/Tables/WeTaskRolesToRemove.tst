<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="aa28bf7e-71df-43e2-b636-e93cb133be47" Name="WeTaskRolesToRemove" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Список ФР, которые необходимо удалить из задания.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="aa28bf7e-71df-00e2-2000-093cb133be47" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="aa28bf7e-71df-01e2-4000-093cb133be47" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="aa28bf7e-71df-00e2-3100-093cb133be47" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="337ad571-d5fb-44c0-9310-0b8e8bc57089" Name="TaskRole" Type="Reference(Typified) Not Null" ReferencedTable="a59078ce-8acf-4c45-a49a-503fa88a0580">
		<Description>Функциональная роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="337ad571-d5fb-00c0-4000-0b8e8bc57089" Name="TaskRoleID" Type="Guid Not Null" ReferencedColumn="bd4fdcea-8042-488a-94c9-770b49357cfe" />
		<SchemeReferencingColumn ID="ed2fab8d-73af-4c3f-b7fc-1e4c3e8e3088" Name="TaskRoleCaption" Type="String(128) Not Null" ReferencedColumn="f8b3afc6-cea7-4a98-b907-e716e0a426c6" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="aa28bf7e-71df-00e2-5000-093cb133be47" Name="pk_WeTaskRolesToRemove">
		<SchemeIndexedColumn Column="aa28bf7e-71df-00e2-3100-093cb133be47" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="aa28bf7e-71df-00e2-7000-093cb133be47" Name="idx_WeTaskRolesToRemove_ID" IsClustered="true">
		<SchemeIndexedColumn Column="aa28bf7e-71df-01e2-4000-093cb133be47" />
	</SchemeIndex>
</SchemeTable>