<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="6430bb73-4243-474b-89a8-f7291117a69e" Name="WeTaskActionTaskRoleRoles" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Если в WeTaskActionTaskRoles выбран режим выбора роли - Список ролей, то тут они будут храниться.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="6430bb73-4243-004b-2000-07291117a69e" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="6430bb73-4243-014b-4000-07291117a69e" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="6430bb73-4243-004b-3100-07291117a69e" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="b9cba5a9-1be0-49f1-b615-505ae0db1d44" Name="TaskActionTaskRole" Type="Reference(Typified) Not Null" ReferencedTable="fbfa5ac2-ba00-449a-8f85-121472a99d2c" IsReferenceToOwner="true">
		<Description>Ссылка на родительскую запись в WeTaskActionTaskRoles</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b9cba5a9-1be0-00f1-4000-005ae0db1d44" Name="TaskActionTaskRoleRowID" Type="Guid Not Null" ReferencedColumn="fbfa5ac2-ba00-009a-3100-021472a99d2c" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="e1b64887-65f8-4288-ab9a-0e52177cecb4" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b">
		<Description>Ссылка на роль.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e1b64887-65f8-0088-4000-0e52177cecb4" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="458a32b9-6e31-4b86-ba86-eb99209c9ba4" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="6430bb73-4243-004b-5000-07291117a69e" Name="pk_WeTaskActionTaskRoleRoles">
		<SchemeIndexedColumn Column="6430bb73-4243-004b-3100-07291117a69e" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="6430bb73-4243-004b-7000-07291117a69e" Name="idx_WeTaskActionTaskRoleRoles_ID" IsClustered="true">
		<SchemeIndexedColumn Column="6430bb73-4243-014b-4000-07291117a69e" />
	</SchemeIndex>
</SchemeTable>