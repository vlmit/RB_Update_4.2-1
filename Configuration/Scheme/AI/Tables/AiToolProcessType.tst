<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="0b8beefb-4da7-492c-b5d5-2864e2797f27" Name="AiToolProcessType" Group="AI">
	<Description>Типы запускаемых процессов.</Description>
	<SchemePhysicalColumn ID="e5689715-659a-4f00-80ef-5ffcb80e469e" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d0da662b-f05d-4de6-a14f-7da77ecbc645" Name="Name" Type="String(64) Not Null">
		<Description>Название</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="af265d4d-c704-4484-9ea3-82caa61e27b3" Name="pk_AiToolProcessType">
		<SchemeIndexedColumn Column="e5689715-659a-4f00-80ef-5ffcb80e469e" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="e5689715-659a-4f00-80ef-5ffcb80e469e">8888f269-7964-4b9b-bd9d-5e61f9ce29cc</ID>
		<Name ID="d0da662b-f05d-4de6-a14f-7da77ecbc645">$Ai_ProcessType_KrRoutes</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="e5689715-659a-4f00-80ef-5ffcb80e469e">8eb3e1c5-fa03-4b06-b1fd-926fda20be53</ID>
		<Name ID="d0da662b-f05d-4de6-a14f-7da77ecbc645">$Ai_ProcessType_WorkflowEngine</Name>
	</SchemeRecord>
</SchemeTable>