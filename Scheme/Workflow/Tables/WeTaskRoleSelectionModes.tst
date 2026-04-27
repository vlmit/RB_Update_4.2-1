<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="1b600081-a51e-4f34-a039-5306c83f78e0" Name="WeTaskRoleSelectionModes" Group="WorkflowEngine">
	<Description>Режимы выбора роли для настройки связанных с действием ролей.</Description>
	<SchemePhysicalColumn ID="4531a4ed-e565-4433-9068-e3241727d091" Name="ID" Type="Int32 Not Null">
		<Description>Идентификатор режима</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1f642d7a-5a19-4480-bbbf-dfee1073d564" Name="Name" Type="String(128) Not Null">
		<Description>Название режима</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="0f4127d5-8eb7-4ce4-8553-211d18e848a2" Name="pk_WeTaskRoleSelectionModes">
		<SchemeIndexedColumn Column="4531a4ed-e565-4433-9068-e3241727d091" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="4531a4ed-e565-4433-9068-e3241727d091">0</ID>
		<Name ID="1f642d7a-5a19-4480-bbbf-dfee1073d564">$WorkflowEngine_TaskRoleSelectionModes_CurrentUser</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="4531a4ed-e565-4433-9068-e3241727d091">1</ID>
		<Name ID="1f642d7a-5a19-4480-bbbf-dfee1073d564">$WorkflowEngine_TaskRoleSelectionModes_Role</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="4531a4ed-e565-4433-9068-e3241727d091">2</ID>
		<Name ID="1f642d7a-5a19-4480-bbbf-dfee1073d564">$WorkflowEngine_TaskRoleSelectionModes_RolesList</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="4531a4ed-e565-4433-9068-e3241727d091">3</ID>
		<Name ID="1f642d7a-5a19-4480-bbbf-dfee1073d564">$WorkflowEngine_TaskRoleSelectionModes_Script</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="4531a4ed-e565-4433-9068-e3241727d091">4</ID>
		<Name ID="1f642d7a-5a19-4480-bbbf-dfee1073d564">$WorkflowEngine_TaskRoleSelectionModes_SQLQuery</Name>
	</SchemeRecord>
</SchemeTable>