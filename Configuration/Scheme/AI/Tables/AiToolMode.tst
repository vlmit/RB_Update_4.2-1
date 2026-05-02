<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="e2b5dc2d-707d-452e-940c-29ddaabefc49" Name="AiToolMode" Group="AI">
	<Description>Режим работы ИИ инструмента.</Description>
	<SchemePhysicalColumn ID="53d6d13f-c1df-4365-822d-7c8a96d8a041" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор записи.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="5e68f51b-bfc2-49ed-aa22-c60a240fe1f8" Name="Name" Type="String(128) Not Null">
		<Description>Название вида запроса.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="465dbae4-82a9-4a46-9be4-0bdc3f6dc014" Name="pk_AiToolMode">
		<SchemeIndexedColumn Column="53d6d13f-c1df-4365-822d-7c8a96d8a041" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="53d6d13f-c1df-4365-822d-7c8a96d8a041">3a770784-ae00-4cb0-87d8-587a7d353c50</ID>
		<Name ID="5e68f51b-bfc2-49ed-aa22-c60a240fe1f8">$Ai_ToolModes_GetData</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="53d6d13f-c1df-4365-822d-7c8a96d8a041">dd242756-b082-4830-930f-cb535295e401</ID>
		<Name ID="5e68f51b-bfc2-49ed-aa22-c60a240fe1f8">$Ai_ToolModes_EnterData</Name>
	</SchemeRecord>
</SchemeTable>