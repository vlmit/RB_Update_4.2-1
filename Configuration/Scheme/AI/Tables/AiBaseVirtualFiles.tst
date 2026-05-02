<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="11ab6365-8c4b-487d-951b-5a932477cdab" Name="AiBaseVirtualFiles" Group="AI">
	<SchemePhysicalColumn ID="1f8efc17-9e23-46e0-a9fe-422cc9d923be" Name="ID" Type="Guid Not Null">
		<Description>Base file identifier for Kind=VirtualFile.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="574c35a2-30d8-4619-80e9-b94a04479d02" Name="RequestFileID" Type="Guid Not Null">
		<Description>Virtual file identifier to send via request (content extensions will use this identifier, not base file ID). This identifier should not correspond to any physical file's VersionRowID.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="b8ea9823-e2f6-4603-b82c-724e5c1f7013" Name="TypeName" Type="String(128) Not Null">
		<Description>Virtual file type name provided for content extensions.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="277acc5b-ffb1-4c63-919b-d3a94450b149" Name="Name" Type="String(256) Not Null">
		<Description>File name for virtual files.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="989b7100-fce8-4cd2-80f7-67ef4bc08963" Name="Size" Type="Int64 Null">
		<Description>File size in bytes for a virtual files, or Null if size for the virtual file is unknown beforehand.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="9a2bb683-96b4-44bb-8499-ddba880bbbba" Name="pk_AiBaseVirtualFiles">
		<SchemeIndexedColumn Column="1f8efc17-9e23-46e0-a9fe-422cc9d923be" />
	</SchemePrimaryKey>
	<SchemeIndex ID="9a143942-4e68-4a3b-af52-535ff63675fc" Name="ndx_AiBaseVirtualFiles_RequestFileIDTypeName" IsUnique="true">
		<SchemeIndexedColumn Column="574c35a2-30d8-4619-80e9-b94a04479d02" />
		<SchemeIndexedColumn Column="b8ea9823-e2f6-4603-b82c-724e5c1f7013">
			<Expression Dbms="PostgreSql">lower("TypeName")</Expression>
		</SchemeIndexedColumn>
	</SchemeIndex>
</SchemeTable>