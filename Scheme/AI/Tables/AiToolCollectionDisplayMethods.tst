<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="924e1a98-cdb4-49bb-9b83-2729cd787bea" Name="AiToolCollectionDisplayMethods" Group="AI">
	<Description>Способы вывода списка в чате ассистента.</Description>
	<SchemePhysicalColumn ID="c6567a18-a108-4114-9ec8-2d4218d16ad2" Name="ID" Type="Int16 Not Null" />
	<SchemePhysicalColumn ID="4fe309ee-179a-4d49-8883-e708a1edc760" Name="Name" Type="String(64) Not Null" />
	<SchemePrimaryKey ID="14b31e9a-60dc-491c-b3dc-5693743d9c21" Name="pk_AiToolCollectionDisplayMethods">
		<SchemeIndexedColumn Column="c6567a18-a108-4114-9ec8-2d4218d16ad2" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="c6567a18-a108-4114-9ec8-2d4218d16ad2">0</ID>
		<Name ID="4fe309ee-179a-4d49-8883-e708a1edc760">$Ai_CollectionDisplayMethods_InRow</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="c6567a18-a108-4114-9ec8-2d4218d16ad2">1</ID>
		<Name ID="4fe309ee-179a-4d49-8883-e708a1edc760">$Ai_CollectionDisplayMethods_BulletedList</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="c6567a18-a108-4114-9ec8-2d4218d16ad2">2</ID>
		<Name ID="4fe309ee-179a-4d49-8883-e708a1edc760">$Ai_CollectionDisplayMethods_NumberedList</Name>
	</SchemeRecord>
</SchemeTable>