<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="fbfa5ac2-ba00-449a-8f85-121472a99d2c" Name="WeTaskActionTaskRoles" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Секция для настройки связанных с действием Задание ролей</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="fbfa5ac2-ba00-009a-2000-021472a99d2c" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="fbfa5ac2-ba00-019a-4000-021472a99d2c" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="fbfa5ac2-ba00-009a-3100-021472a99d2c" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="edb40e1c-782e-4594-a182-e156d24a2d7e" Name="TaskRole" Type="Reference(Typified) Not Null" ReferencedTable="a59078ce-8acf-4c45-a49a-503fa88a0580">
		<Description>Функциональная роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="edb40e1c-782e-0094-4000-0156d24a2d7e" Name="TaskRoleID" Type="Guid Not Null" ReferencedColumn="bd4fdcea-8042-488a-94c9-770b49357cfe" />
		<SchemeReferencingColumn ID="47137860-f7c4-4951-91b3-de5791ae43db" Name="TaskRoleCaption" Type="String(128) Not Null" ReferencedColumn="f8b3afc6-cea7-4a98-b907-e716e0a426c6" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="93cd98d3-4983-4426-8d71-f4371effc218" Name="RoleSelectionMode" Type="Reference(Typified) Null" ReferencedTable="1b600081-a51e-4f34-a039-5306c83f78e0">
		<Description>Режим выбора роли</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="f94b5692-2524-4dda-87f3-6d6cfd8dd35f" Name="RoleSelectionModeID" Type="Int32 Null" ReferencedColumn="4531a4ed-e565-4433-9068-e3241727d091" />
		<SchemeReferencingColumn ID="e7dee8f2-79bf-4c26-811f-a4f57f0c72f0" Name="RoleSelectionModeName" Type="String(128) Null" ReferencedColumn="1f642d7a-5a19-4480-bbbf-dfee1073d564" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="0ae056c9-2d09-47d9-8144-76285c6af143" Name="Master" Type="Boolean Not Null">
		<Description>Основная запись. Опираясь на неё берётся временная зона и календарь.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="9330dfc9-a2ca-4ee1-a43f-dac4aa467874" Name="df_WeTaskActionTaskRoles_Master" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="9b4e1fa2-6c25-4783-9b40-510e82b677f3" Name="ShowInTaskDetails" Type="Boolean Not Null">
		<Description>Показывать запись в информации о задании.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="5416c565-8dbe-46fa-8c8e-fee6d41e2d8b" Name="df_WeTaskActionTaskRoles_ShowInTaskDetails" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="cfed364d-a2dd-4ab4-b712-23c0c7da60af" Name="Role" Type="Reference(Typified) Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b">
		<Description>Роль, на которую будет отправлено задание. Если выбран режим выбора роли - Роль.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="cfed364d-a2dd-00b4-4000-03c0c7da60af" Name="RoleID" Type="Guid Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="e3a0dea8-0755-42cb-879d-3f6c3208b2a5" Name="RoleName" Type="String(128) Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="08c334e0-356c-40d1-8289-110a67754acd" Name="TaskRoleScript" Type="String(Max) Null">
		<Description>Cкрипт на языке C#, который определяет список ролей, если выбран режим выбора роли - Скрипт.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="2aeaeb1e-e9fd-4e0a-b818-840668c3641e" Name="SqlQuery" Type="String(Max) Null">
		<Description>SQL-запрос, который определяет список ролей, если выбран режим выбора роли - SQL-запрос.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="fbfa5ac2-ba00-009a-5000-021472a99d2c" Name="pk_WeTaskActionTaskRoles">
		<SchemeIndexedColumn Column="fbfa5ac2-ba00-009a-3100-021472a99d2c" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="fbfa5ac2-ba00-009a-7000-021472a99d2c" Name="idx_WeTaskActionTaskRoles_ID" IsClustered="true">
		<SchemeIndexedColumn Column="fbfa5ac2-ba00-019a-4000-021472a99d2c" />
	</SchemeIndex>
</SchemeTable>