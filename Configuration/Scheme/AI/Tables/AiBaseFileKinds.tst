<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="b184a102-c827-4950-a012-cfede4272867" Name="AiBaseFileKinds" Group="AI">
	<SchemePhysicalColumn ID="a6dd0944-5481-4b12-b81b-debffe31d627" Name="ID" Type="Int16 Not Null" />
	<SchemePhysicalColumn ID="45f47d23-4ca0-47f7-b5b1-7cde74acf5f8" Name="Name" Type="String(128) Not Null" />
	<SchemePhysicalColumn ID="d04840aa-47a8-49eb-afc5-e0b7ef2be5c3" Name="Description" Type="String(Max) Null" IsVirtual="true" />
	<SchemePrimaryKey ID="f4fd2f86-327d-46c6-b946-22176c109a68" Name="pk_AiBaseFileKinds">
		<SchemeIndexedColumn Column="a6dd0944-5481-4b12-b81b-debffe31d627" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="a6dd0944-5481-4b12-b81b-debffe31d627">1</ID>
		<Name ID="45f47d23-4ca0-47f7-b5b1-7cde74acf5f8">LooseFile</Name>
		<Description ID="d04840aa-47a8-49eb-afc5-e0b7ef2be5c3">Loose file uploaded to the cache, it's not linked to a card or another object</Description>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="a6dd0944-5481-4b12-b81b-debffe31d627">2</ID>
		<Name ID="45f47d23-4ca0-47f7-b5b1-7cde74acf5f8">CardFile</Name>
		<Description ID="d04840aa-47a8-49eb-afc5-e0b7ef2be5c3">A file in another card used in AI</Description>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="a6dd0944-5481-4b12-b81b-debffe31d627">3</ID>
		<Name ID="45f47d23-4ca0-47f7-b5b1-7cde74acf5f8">VirtualFile</Name>
		<Description ID="d04840aa-47a8-49eb-afc5-e0b7ef2be5c3">Virtual file used in AI with content resolving via extensions</Description>
	</SchemeRecord>
</SchemeTable>