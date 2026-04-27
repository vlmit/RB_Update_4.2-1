<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="83e5f876-1537-45e8-a962-d7d7a8749bab" Name="AiBaseFileOperationStates" Group="AI">
	<SchemePhysicalColumn ID="6f756f7f-9ac8-4599-81a2-d25b1beddeaf" Name="ID" Type="Int16 Not Null" />
	<SchemePhysicalColumn ID="89552778-1c74-4833-8b4c-8267476afa8e" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="13d6a281-0c02-46e3-ac25-090724b5e8ec" Name="pk_AiBaseFileOperationStates">
		<SchemeIndexedColumn Column="6f756f7f-9ac8-4599-81a2-d25b1beddeaf" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="6f756f7f-9ac8-4599-81a2-d25b1beddeaf">0</ID>
		<Name ID="89552778-1c74-4833-8b4c-8267476afa8e">None</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="6f756f7f-9ac8-4599-81a2-d25b1beddeaf">1</ID>
		<Name ID="89552778-1c74-4833-8b4c-8267476afa8e">InProgress</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="6f756f7f-9ac8-4599-81a2-d25b1beddeaf">2</ID>
		<Name ID="89552778-1c74-4833-8b4c-8267476afa8e">Completed</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="6f756f7f-9ac8-4599-81a2-d25b1beddeaf">3</ID>
		<Name ID="89552778-1c74-4833-8b4c-8267476afa8e">Error</Name>
	</SchemeRecord>
</SchemeTable>