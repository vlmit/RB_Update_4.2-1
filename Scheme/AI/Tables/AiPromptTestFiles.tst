<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="0fd5a4f7-ce06-4250-9f9b-4760d334aadf" Name="AiPromptTestFiles" Group="AI">
	<Description>Файлы механизма тестирования промптов.</Description>
	<SchemePhysicalColumn ID="f5bce34f-743a-47a2-a3e5-629c3018a399" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="4ef0479e-10b1-40f7-bf7d-627af1082b4a" Name="Name" Type="String(128) Not Null">
		<Description>Имя файла.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="244389cc-f383-4b8d-8914-22657650cd2c" Name="Test" Type="Reference(Typified) Not Null" ReferencedTable="91e83dd3-0932-404d-b744-fab67214777e">
		<Description>К какому тесту относится файл.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="244389cc-f383-008d-4000-02657650cd2c" Name="TestID" Type="Guid Not Null" ReferencedColumn="15abc721-1bcd-488e-9c15-d0cd43a79c74" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="9743a132-96ec-4c77-939d-671035dcf4dc" Name="Kind" Type="Reference(Typified) Not Null" ReferencedTable="36f8762c-1731-4682-bff3-696a8609bc42" WithForeignKey="false">
		<Description>Тип файла.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9743a132-96ec-0077-4000-071035dcf4dc" Name="KindID" Type="Int16 Not Null" ReferencedColumn="385a1ebd-d255-44d2-bef5-f7bef1dbe81d" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="e815cd7b-107f-4b69-afa4-2c621d249329" Name="Data" Type="BinaryJson Not Null">
		<Description>Данные.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="67d46102-c263-428e-8700-49d2e742362d" Name="pk_AiPromptTestFiles">
		<SchemeIndexedColumn Column="f5bce34f-743a-47a2-a3e5-629c3018a399" />
	</SchemePrimaryKey>
</SchemeTable>