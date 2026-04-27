<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="cd3b3d0e-85de-40ec-8fea-5f685a99e591" Name="AiModel" Group="AI" IsVirtual="true">
	<SchemePhysicalColumn ID="23adb511-51f8-4fec-a8c6-720fb235a06f" Name="ID" Type="String(128) Not Null">
		<Description>Идентификатор модели.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f8ab2dfa-6e89-4266-92e5-537e858b7f82" Name="Name" Type="String(Max) Not Null">
		<Description>Имя модели</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="ec7aaa33-67c0-4a69-a35b-5fe37ef8a9fe" Name="pk_AiModel">
		<SchemeIndexedColumn Column="23adb511-51f8-4fec-a8c6-720fb235a06f" />
	</SchemePrimaryKey>
</SchemeTable>