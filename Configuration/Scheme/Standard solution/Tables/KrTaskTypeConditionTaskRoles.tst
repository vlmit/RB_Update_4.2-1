<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="dc7396ff-b154-42e7-b54a-05cd51eb4620" Name="KrTaskTypeConditionTaskRoles" Group="Kr" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Список функциональных ролей для условия.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="dc7396ff-b154-00e7-2000-05cd51eb4620" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="dc7396ff-b154-01e7-4000-05cd51eb4620" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="dc7396ff-b154-00e7-3100-05cd51eb4620" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="565d55f9-fe3c-4314-8eb7-a1200574dd1e" Name="TaskRole" Type="Reference(Typified) Not Null" ReferencedTable="a59078ce-8acf-4c45-a49a-503fa88a0580">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="565d55f9-fe3c-0014-4000-01200574dd1e" Name="TaskRoleID" Type="Guid Not Null" ReferencedColumn="bd4fdcea-8042-488a-94c9-770b49357cfe" />
		<SchemeReferencingColumn ID="8c314e02-1d3d-4d35-a7da-b084f5bdc2f1" Name="TaskRoleCaption" Type="String(128) Not Null" ReferencedColumn="f8b3afc6-cea7-4a98-b907-e716e0a426c6" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="dc7396ff-b154-00e7-5000-05cd51eb4620" Name="pk_KrTaskTypeConditionTaskRoles">
		<SchemeIndexedColumn Column="dc7396ff-b154-00e7-3100-05cd51eb4620" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="dc7396ff-b154-00e7-7000-05cd51eb4620" Name="idx_KrTaskTypeConditionTaskRoles_ID" IsClustered="true">
		<SchemeIndexedColumn Column="dc7396ff-b154-01e7-4000-05cd51eb4620" />
	</SchemeIndex>
</SchemeTable>