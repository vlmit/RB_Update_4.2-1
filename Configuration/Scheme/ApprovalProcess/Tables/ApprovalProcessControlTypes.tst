<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="86bc6d2f-dce4-4382-ab7c-204bc9c0ec77" Name="ApprovalProcessControlTypes" Group="ApprovalProcess">
	<SchemePhysicalColumn ID="e15cff17-155b-4997-8552-ef76d42f7801" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="e3729b05-c5f0-4d79-9612-e6ceec0ec4aa" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="45c92985-098a-45f9-b695-9db1c84dd0cf" Name="pk_ApprovalProcessControlTypes">
		<SchemeIndexedColumn Column="e15cff17-155b-4997-8552-ef76d42f7801" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="e15cff17-155b-4997-8552-ef76d42f7801">a923c693-5ac0-4616-8beb-e84f08a70f2a</ID>
		<Name ID="e3729b05-c5f0-4d79-9612-e6ceec0ec4aa">$ApprovalProcess_ControlTypes_Revoke</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="e15cff17-155b-4997-8552-ef76d42f7801">3c4d94ba-d32d-4416-a09c-5dbc6c0bcf04</ID>
		<Name ID="e3729b05-c5f0-4d79-9612-e6ceec0ec4aa">$ApprovalProcess_ControlTypes_ChangeState</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="e15cff17-155b-4997-8552-ef76d42f7801">ebaeb555-fb31-47ed-a103-a8a2e51aa943</ID>
		<Name ID="e3729b05-c5f0-4d79-9612-e6ceec0ec4aa">$ApprovalProcess_ControlTypes_Delete</Name>
	</SchemeRecord>
</SchemeTable>