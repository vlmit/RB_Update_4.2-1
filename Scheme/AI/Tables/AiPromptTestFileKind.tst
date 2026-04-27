<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="36f8762c-1731-4682-bff3-696a8609bc42" Name="AiPromptTestFileKind" Group="AI">
	<Description>Тип файла механизма тестирования промптов ИИ.</Description>
	<SchemePhysicalColumn ID="385a1ebd-d255-44d2-bef5-f7bef1dbe81d" Name="ID" Type="Int16 Not Null">
		<Description>Типы файлов, используемых в механизме тестирования промптов.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d191b6f7-0521-4b05-9998-4150420f3bd0" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="17de0220-6236-4893-9c37-9f743dc8b10b" Name="pk_AiPromptTestFileKind">
		<SchemeIndexedColumn Column="385a1ebd-d255-44d2-bef5-f7bef1dbe81d" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="385a1ebd-d255-44d2-bef5-f7bef1dbe81d">0</ID>
		<Name ID="d191b6f7-0521-4b05-9998-4150420f3bd0">Test</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="385a1ebd-d255-44d2-bef5-f7bef1dbe81d">1</ID>
		<Name ID="d191b6f7-0521-4b05-9998-4150420f3bd0">Result</Name>
	</SchemeRecord>
</SchemeTable>