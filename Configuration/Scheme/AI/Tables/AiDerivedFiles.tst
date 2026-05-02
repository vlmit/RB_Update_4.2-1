<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="76bcb0f5-cd68-4655-b7fe-07c675d2ad3b" Name="AiDerivedFiles" Group="AI">
	<Description>ID - derived file version id (always corresponds to FileVersions.RowID)</Description>
	<SchemePhysicalColumn ID="76c0beb9-2664-4ae1-81ca-4598c17b92d7" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="9dd3ca23-c8dc-4990-b5b0-b46d4b504426" Name="BaseFile" Type="Reference(Typified) Not Null" ReferencedTable="3324df2e-6cbd-4e7a-bf9f-8f7762e3f83d" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9dd3ca23-c8dc-0090-4000-046d4b504426" Name="BaseFileID" Type="Guid Not Null" ReferencedColumn="227cd8be-452b-4c7a-9962-3874031f8f13" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="ecde8c4b-7dc2-4c6c-a01d-a3c9cd9ba91f" Name="Kind" Type="Reference(Typified) Not Null" ReferencedTable="dad4ef24-8b08-41f7-b5ea-6f556f95bd2c" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ecde8c4b-7dc2-006c-4000-03c9cd9ba91f" Name="KindID" Type="Int16 Not Null" ReferencedColumn="e7592aba-9acc-4722-9f73-5b632b5f26e6" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="cec079f3-cddc-4d7d-b68a-277b5e3502b3" Name="Page" Type="Int32 Null">
		<Description>Only for Kind=Page, first page is 0</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="9aa5f09c-2e21-4b7a-a325-e3684e199b95" Name="pk_AiDerivedFiles" IsClustered="true">
		<SchemeIndexedColumn Column="76c0beb9-2664-4ae1-81ca-4598c17b92d7" />
	</SchemePrimaryKey>
	<SchemeIndex ID="acf7b50d-7361-4d4d-a03f-c3dbba2b81cf" Name="ndx_AiDerivedFiles_BaseFileID">
		<Description>Use cases: Get derived files to delete by base file id</Description>
		<SchemeIndexedColumn Column="9dd3ca23-c8dc-0090-4000-046d4b504426" />
	</SchemeIndex>
</SchemeTable>