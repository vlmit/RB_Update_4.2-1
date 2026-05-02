<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="83f77ddf-94f8-4322-918f-151c32bf58b1" Name="ApprovalProcessInfoModes" Group="ApprovalProcess">
	<SchemePhysicalColumn ID="92f68dcb-2ade-417a-8325-499647bfdfe1" Name="ID" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="27821845-4b78-480f-a0ed-11f691752b7e" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="7a739ca5-3074-4dce-bb48-3a9dc5137eb0" Name="pk_ApprovalProcessInfoModes">
		<SchemeIndexedColumn Column="92f68dcb-2ade-417a-8325-499647bfdfe1" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="92f68dcb-2ade-417a-8325-499647bfdfe1">0</ID>
		<Name ID="27821845-4b78-480f-a0ed-11f691752b7e">$ApprovalProcess_InfoModes_None</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="92f68dcb-2ade-417a-8325-499647bfdfe1">1</ID>
		<Name ID="27821845-4b78-480f-a0ed-11f691752b7e">$ApprovalProcess_InfoModes_Show</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="92f68dcb-2ade-417a-8325-499647bfdfe1">2</ID>
		<Name ID="27821845-4b78-480f-a0ed-11f691752b7e">$ApprovalProcess_InfoModes_ShowIfActive</Name>
	</SchemeRecord>
</SchemeTable>