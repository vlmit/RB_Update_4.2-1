<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="c5bcb860-656c-40de-a599-9de51c1e766a" Name="AiBaseFileUpdates" Group="AI">
	<Description>Frequently updated fields for a base file. ID - base file version id (always corresponds to FileVersions.RowID)</Description>
	<SchemePhysicalColumn ID="2a8ff588-5daa-4397-ac52-b30dc9f37785" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="6b67d66a-d448-4e77-a760-9e07db2e3834" Name="LastActivity" Type="DateTime Not Null">
		<Description>Last access to base file's content, or the content of one of its derived files. Creation date if no access to the content was made</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="4ca905aa-15a4-4c55-9762-d1db05560147" Name="pk_AiBaseFileUpdates" IsClustered="true">
		<SchemeIndexedColumn Column="2a8ff588-5daa-4397-ac52-b30dc9f37785" />
	</SchemePrimaryKey>
</SchemeTable>