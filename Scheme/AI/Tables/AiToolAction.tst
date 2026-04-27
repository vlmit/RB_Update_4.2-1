<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="78a63577-af61-494e-984a-b5c6ae2c3f71" Name="AiToolAction" Group="AI">
	<Description>Действия, выполняемые в ТЕССА.</Description>
	<SchemePhysicalColumn ID="520b6ee2-07c2-42b7-ad8b-49f4d72693ee" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор действия.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="7d879c5a-2e3e-4eb1-afa6-eb2d4f3a1e1e" Name="Name" Type="String(64) Not Null">
		<Description>Название действия.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="35dbd54c-fe20-418c-ae72-7afc2ec1968d" Name="pk_AiToolAction">
		<SchemeIndexedColumn Column="520b6ee2-07c2-42b7-ad8b-49f4d72693ee" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="520b6ee2-07c2-42b7-ad8b-49f4d72693ee">86a84c37-9a2d-4e28-bbd7-79555f585afa</ID>
		<Name ID="7d879c5a-2e3e-4eb1-afa6-eb2d4f3a1e1e">$Ai_Actions_CreateCardAndStartProcess</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="520b6ee2-07c2-42b7-ad8b-49f4d72693ee">160a8cae-95ba-4722-bea1-4c3466942757</ID>
		<Name ID="7d879c5a-2e3e-4eb1-afa6-eb2d4f3a1e1e">$Ai_Actions_CreateAndSaveCard</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="520b6ee2-07c2-42b7-ad8b-49f4d72693ee">6da968ff-e7a5-42cd-9ce5-afc8bc8f0722</ID>
		<Name ID="7d879c5a-2e3e-4eb1-afa6-eb2d4f3a1e1e">$Ai_Actions_CreateAndOpenCard</Name>
	</SchemeRecord>
</SchemeTable>