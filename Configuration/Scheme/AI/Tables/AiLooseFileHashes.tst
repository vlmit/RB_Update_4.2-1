<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="fcfbb13c-0dbb-453a-80e9-3811b48ae5ae" Name="AiLooseFileHashes" Group="AI">
	<Description>SHA256 hashes for non-card files in the cache. ID - base file version id (always corresponds to FileVersions.RowID)</Description>
	<SchemePhysicalColumn ID="4d258bc8-d686-4b8f-81f6-3f58567439b8" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="1e391613-6b07-4d54-bf98-aed65444e4ed" Name="Hash" Type="Binary(32) Not Null" />
	<SchemePrimaryKey ID="e492d1e7-64bd-4605-b120-1b5b36f6388b" Name="pk_AiLooseFileHashes" IsClustered="true">
		<SchemeIndexedColumn Column="4d258bc8-d686-4b8f-81f6-3f58567439b8" />
	</SchemePrimaryKey>
	<SchemeIndex ID="6ab485ed-2b14-45e9-a091-aa218b29bfda" Name="ndx_AiLooseFileHashes_Hash" IsUnique="true">
		<SchemeIndexedColumn Column="1e391613-6b07-4d54-bf98-aed65444e4ed" />
	</SchemeIndex>
</SchemeTable>