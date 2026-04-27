<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="251b7528-a0db-43f7-83c0-8fb0c45db41a" Name="AiToolSearchService" Group="AI">
	<Description>Типы сервисов поиска значения по справочнику.</Description>
	<SchemePhysicalColumn ID="56970d6f-ddd2-4685-a76c-d1eea45938c0" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="03d30f9e-1afc-4780-9f57-33871a222eb3" Name="Name" Type="String(128) Not Null">
		<Description>Название.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="feee6edf-f77d-4b26-aa6c-2a2e06cca396" Name="pk_AiToolSearchService">
		<SchemeIndexedColumn Column="56970d6f-ddd2-4685-a76c-d1eea45938c0" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="56970d6f-ddd2-4685-a76c-d1eea45938c0">22ae13ff-5a5b-466c-af53-6ba60daaae09</ID>
		<Name ID="03d30f9e-1afc-4780-9f57-33871a222eb3">$Ai_SearchService_Employees</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="56970d6f-ddd2-4685-a76c-d1eea45938c0">f2cf1206-2c66-4827-892c-215f76a8c60c</ID>
		<Name ID="03d30f9e-1afc-4780-9f57-33871a222eb3">$Ai_SearchService_Partners</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="56970d6f-ddd2-4685-a76c-d1eea45938c0">85864bc1-439b-4df8-b34c-3ab89f7fe3fe</ID>
		<Name ID="03d30f9e-1afc-4780-9f57-33871a222eb3">$Ai_SearchService_States</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="56970d6f-ddd2-4685-a76c-d1eea45938c0">d8a0a59a-b97e-42ff-9474-98ed713119b1</ID>
		<Name ID="03d30f9e-1afc-4780-9f57-33871a222eb3">$Ai_SearchService_Departments</Name>
	</SchemeRecord>
</SchemeTable>