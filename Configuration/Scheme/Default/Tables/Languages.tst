<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="1ed36bf1-2ebf-43da-acb2-1ddb3298dbd8" Name="Languages" Group="System">
	<SchemePhysicalColumn ID="f13de4a3-34d7-4e7b-95b6-f34372ed724c" Name="ID" Type="Int16 Not Null" />
	<SchemePhysicalColumn ID="40a3d47c-40f7-48bd-ab8e-edef2f84094d" Name="Caption" Type="String(256) Not Null" />
	<SchemePhysicalColumn ID="9e7084bb-c1dc-4ace-90c9-800dbcf7f3c2" Name="Code" Type="AnsiString(3) Not Null" />
	<SchemePhysicalColumn ID="d66a9b58-d9d9-483c-80e1-0470a5863a66" Name="FallbackCode" Type="AnsiString(3) Null" />
	<SchemePrimaryKey ID="75746b23-6666-4cb5-9c34-5b7f677bc342" Name="pk_Languages" IsClustered="true">
		<SchemeIndexedColumn Column="f13de4a3-34d7-4e7b-95b6-f34372ed724c" />
	</SchemePrimaryKey>
	<SchemeIndex ID="b9623eb8-3c81-4ae1-86ae-9c01bc1a94a8" Name="ndx_Languages_Code" IsUnique="true">
		<SchemeIndexedColumn Column="9e7084bb-c1dc-4ace-90c9-800dbcf7f3c2" />
		<SchemeIncludedColumn Column="d66a9b58-d9d9-483c-80e1-0470a5863a66" />
	</SchemeIndex>
	<SchemeRecord>
		<ID ID="f13de4a3-34d7-4e7b-95b6-f34372ed724c">0</ID>
		<Caption ID="40a3d47c-40f7-48bd-ab8e-edef2f84094d">English</Caption>
		<Code ID="9e7084bb-c1dc-4ace-90c9-800dbcf7f3c2">en</Code>
		<FallbackCode ID="d66a9b58-d9d9-483c-80e1-0470a5863a66" xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" />
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="f13de4a3-34d7-4e7b-95b6-f34372ed724c">1</ID>
		<Caption ID="40a3d47c-40f7-48bd-ab8e-edef2f84094d">Русский</Caption>
		<Code ID="9e7084bb-c1dc-4ace-90c9-800dbcf7f3c2">ru</Code>
		<FallbackCode ID="d66a9b58-d9d9-483c-80e1-0470a5863a66" xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" />
	</SchemeRecord>
</SchemeTable>