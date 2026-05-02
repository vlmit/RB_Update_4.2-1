<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="eaa6cb07-cc29-4747-9503-093c7630d298" Name="KrUniversalTaskActionButtonTaskRolesVirtual" Group="KrWe" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="eaa6cb07-cc29-0047-2000-093c7630d298" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="eaa6cb07-cc29-0147-4000-093c7630d298" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="eaa6cb07-cc29-0047-3100-093c7630d298" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="60180e2e-7cc4-47ec-a1f8-ee318f89815a" Name="TaskRole" Type="Reference(Typified) Not Null" ReferencedTable="a59078ce-8acf-4c45-a49a-503fa88a0580">
		<Description>Функциональная роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="60180e2e-7cc4-00ec-4000-0e318f89815a" Name="TaskRoleID" Type="Guid Not Null" ReferencedColumn="bd4fdcea-8042-488a-94c9-770b49357cfe" />
		<SchemeReferencingColumn ID="25ee21bf-23c8-4491-9b2d-e095504fd4d1" Name="TaskRoleCaption" Type="String(128) Not Null" ReferencedColumn="f8b3afc6-cea7-4a98-b907-e716e0a426c6" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="405d3073-05ea-4a63-9737-d6df631977d8" Name="TaskButton" Type="Reference(Typified) Not Null" ReferencedTable="e85631c4-0014-4842-86f4-9a6ba66166f3" IsReferenceToOwner="true" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="405d3073-05ea-0063-4000-06df631977d8" Name="TaskButtonRowID" Type="Guid Not Null" ReferencedColumn="e85631c4-0014-0042-3100-0a6ba66166f3" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="eaa6cb07-cc29-0047-5000-093c7630d298" Name="pk_KrUniversalTaskActionButtonTaskRolesVirtual">
		<SchemeIndexedColumn Column="eaa6cb07-cc29-0047-3100-093c7630d298" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="eaa6cb07-cc29-0047-7000-093c7630d298" Name="idx_KrUniversalTaskActionButtonTaskRolesVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="eaa6cb07-cc29-0147-4000-093c7630d298" />
	</SchemeIndex>
</SchemeTable>