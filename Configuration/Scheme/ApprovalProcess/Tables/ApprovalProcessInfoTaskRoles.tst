<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="7f7dc925-15a3-4d93-b96c-4c6140ecc36f" Name="ApprovalProcessInfoTaskRoles" Group="ApprovalProcess" IsVirtual="true" InstanceType="Tasks" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="7f7dc925-15a3-0093-2000-0c6140ecc36f" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7f7dc925-15a3-0193-4000-0c6140ecc36f" Name="ID" Type="Guid Not Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="7f7dc925-15a3-0093-3100-0c6140ecc36f" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="2d771a7e-e6c7-4c7c-b50f-402676287198" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="2d771a7e-e6c7-007c-4000-002676287198" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="7c5ba929-2bb2-4d33-aaca-29277b2805fb" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
		<SchemePhysicalColumn ID="1af4e373-14df-4684-bd3e-43cb26b600a2" Name="RoleTypeID" Type="Int32 Not Null" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="a2f68d1b-91f4-4473-9557-0e445037d629" Name="Task" Type="Reference(Typified) Not Null" ReferencedTable="b43c68d0-a317-4aad-a400-dda21b33aa29" IsReferenceToOwner="true" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a2f68d1b-91f4-0073-4000-0e445037d629" Name="TaskRowID" Type="Guid Not Null" ReferencedColumn="b43c68d0-a317-00ad-3100-0da21b33aa29" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="7f7dc925-15a3-0093-5000-0c6140ecc36f" Name="pk_ApprovalProcessInfoTaskRoles">
		<SchemeIndexedColumn Column="7f7dc925-15a3-0093-3100-0c6140ecc36f" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="7f7dc925-15a3-0093-7000-0c6140ecc36f" Name="idx_ApprovalProcessInfoTaskRoles_ID" IsClustered="true">
		<SchemeIndexedColumn Column="7f7dc925-15a3-0193-4000-0c6140ecc36f" />
	</SchemeIndex>
</SchemeTable>