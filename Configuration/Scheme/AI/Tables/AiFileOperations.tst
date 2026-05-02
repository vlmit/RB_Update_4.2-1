<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="ede76637-d1de-4022-b479-69d8eca71232" Name="AiFileOperations" Group="AI">
	<Description>Operations performed with files before they are sent to AI model. Corresponds to enum object AiFileRequestOperation.</Description>
	<SchemePhysicalColumn ID="e5a9b9e8-fe27-4cad-b14b-030dbca4849c" Name="ID" Type="Int16 Not Null" />
	<SchemePhysicalColumn ID="471fe95d-e67d-43da-8046-c6738dbdde90" Name="Caption" Type="String(128) Not Null" />
	<SchemePhysicalColumn ID="97121b54-b457-4b76-9463-690dcd586588" Name="Description" Type="String(Max) Null" IsVirtual="true" />
	<SchemePrimaryKey ID="3df86368-e4ce-4a84-a8cf-dc7a3f453f54" Name="pk_AiFileOperations">
		<SchemeIndexedColumn Column="e5a9b9e8-fe27-4cad-b14b-030dbca4849c" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="e5a9b9e8-fe27-4cad-b14b-030dbca4849c">0</ID>
		<Caption ID="471fe95d-e67d-43da-8046-c6738dbdde90">$Ai_AiFileRequestOperation_AsIs</Caption>
		<Description ID="97121b54-b457-4b76-9463-690dcd586588">File is sent as-is, no operations are performed.</Description>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="e5a9b9e8-fe27-4cad-b14b-030dbca4849c">1</ID>
		<Caption ID="471fe95d-e67d-43da-8046-c6738dbdde90">$Ai_AiFileRequestOperation_Text</Caption>
		<Description ID="97121b54-b457-4b76-9463-690dcd586588">Text is extracted from the file, and then it's sent to AI model.</Description>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="e5a9b9e8-fe27-4cad-b14b-030dbca4849c">2</ID>
		<Caption ID="471fe95d-e67d-43da-8046-c6738dbdde90">$Ai_AiFileRequestOperation_Page</Caption>
		<Description ID="97121b54-b457-4b76-9463-690dcd586588">Page images are extracted from the file, and then they are sent to AI model.</Description>
	</SchemeRecord>
</SchemeTable>