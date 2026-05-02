<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="3324df2e-6cbd-4e7a-bf9f-8f7762e3f83d" Name="AiBaseFiles" Group="AI">
	<Description>ID - base file version id (always corresponds to FileVersions.RowID, for card files or loose files)</Description>
	<SchemePhysicalColumn ID="227cd8be-452b-4c7a-9962-3874031f8f13" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="e165044a-14df-4ff2-a235-96703eed075b" Name="Kind" Type="Reference(Typified) Not Null" ReferencedTable="b184a102-c827-4950-a012-cfede4272867" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e165044a-14df-00f2-4000-06703eed075b" Name="KindID" Type="Int16 Not Null" ReferencedColumn="a6dd0944-5481-4b12-b81b-debffe31d627" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="d6c0cb18-027a-4005-af21-515585d29f58" Name="TextExtraction" Type="Reference(Typified) Not Null" ReferencedTable="83e5f876-1537-45e8-a962-d7d7a8749bab" WithForeignKey="false">
		<Description>State of text extraction process from a base file into single derived file</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d6c0cb18-027a-0005-4000-015585d29f58" Name="TextExtractionID" Type="Int16 Not Null" ReferencedColumn="6f756f7f-9ac8-4599-81a2-d25b1beddeaf" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="9b64d387-3ad9-42e8-9c2d-6a091840bce1" Name="PageExtraction" Type="Reference(Typified) Not Null" ReferencedTable="83e5f876-1537-45e8-a962-d7d7a8749bab" WithForeignKey="false">
		<Description>State of pages extraction process from a base file into multiple derived files, each with an image of a page</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9b64d387-3ad9-00e8-4000-0a091840bce1" Name="PageExtractionID" Type="Int16 Not Null" ReferencedColumn="6f756f7f-9ac8-4599-81a2-d25b1beddeaf" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="253bb66e-bd51-4628-ae75-75b688539d00" Name="PageCount" Type="Int32 Null">
		<Description>Null if page extraction isn't completed yet</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="87b827aa-a03b-48f1-8a3a-1cb8694ea4ac" Name="TextExtractionError" Type="String(256) Null" IsSparse="true">
		<Description>Error message occured when extracting the text, or Null if there was no error</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="29f61874-b9c2-41ef-8a3d-76db0ff0e0d1" Name="PageExtractionError" Type="String(256) Null" IsSparse="true">
		<Description>Error message occured when extracting the pages, or Null if there was no error</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="704119e6-8dfe-43a4-b026-ead4b2a925ca" Name="pk_AiBaseFiles" IsClustered="true">
		<SchemeIndexedColumn Column="227cd8be-452b-4c7a-9962-3874031f8f13" />
	</SchemePrimaryKey>
</SchemeTable>