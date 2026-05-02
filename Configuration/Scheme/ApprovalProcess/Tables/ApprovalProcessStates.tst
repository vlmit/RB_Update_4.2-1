<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="7845ed70-a6bc-4b62-8d3d-7a219df2f0fb" Name="ApprovalProcessStates" Group="ApprovalProcess">
	<SchemePhysicalColumn ID="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c" Name="ID" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="39a2a196-7dac-42c4-b2ac-3492da8e5139" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="6c8f38ea-403f-462a-8d5e-da60d91fad1f" Name="pk_ApprovalProcessStates">
		<SchemeIndexedColumn Column="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c">0</ID>
		<Name ID="39a2a196-7dac-42c4-b2ac-3492da8e5139">$ApprovalProcess_State_Pending</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c">1</ID>
		<Name ID="39a2a196-7dac-42c4-b2ac-3492da8e5139">$ApprovalProcess_State_Running</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c">2</ID>
		<Name ID="39a2a196-7dac-42c4-b2ac-3492da8e5139">$ApprovalProcess_State_Approved</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c">3</ID>
		<Name ID="39a2a196-7dac-42c4-b2ac-3492da8e5139">$ApprovalProcess_State_Disapproved</Name>
	</SchemeRecord>
</SchemeTable>