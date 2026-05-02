<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="c6c447cc-adc6-4ee5-bf1e-2c65537162e6" Name="AiPromptTestRequestStates" Group="AI">
	<Description>Состояние запросов на тестирование.</Description>
	<SchemePhysicalColumn ID="5e2cd114-db8f-4bb9-9776-09cc9d8e1e45" Name="ID" Type="Int16 Not Null" />
	<SchemePhysicalColumn ID="81a60825-1216-4028-b77a-0747b64c6716" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="fd20dc31-1d5f-48a9-9dc7-494df84ebd64" Name="pk_AiPromptTestRequestStates">
		<SchemeIndexedColumn Column="5e2cd114-db8f-4bb9-9776-09cc9d8e1e45" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="5e2cd114-db8f-4bb9-9776-09cc9d8e1e45">0</ID>
		<Name ID="81a60825-1216-4028-b77a-0747b64c6716">Created</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="5e2cd114-db8f-4bb9-9776-09cc9d8e1e45">1</ID>
		<Name ID="81a60825-1216-4028-b77a-0747b64c6716">InProgress</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="5e2cd114-db8f-4bb9-9776-09cc9d8e1e45">2</ID>
		<Name ID="81a60825-1216-4028-b77a-0747b64c6716">Completed</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="5e2cd114-db8f-4bb9-9776-09cc9d8e1e45">3</ID>
		<Name ID="81a60825-1216-4028-b77a-0747b64c6716">Error</Name>
	</SchemeRecord>
</SchemeTable>