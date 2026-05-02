<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="31d12e30-acd4-4c46-87ba-0bc3fc4febe3" Name="ApprovalProcessControlMethods" Group="ApprovalProcess">
	<SchemePhysicalColumn ID="b47f144f-2244-487b-b7e5-7aa510cbe851" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="90221ceb-5b2f-4e2b-b6c7-980acec3e2e1" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="96bc1d03-499a-4128-9605-9bb09f919e9a" Name="pk_ApprovalProcessControlMethods">
		<SchemeIndexedColumn Column="b47f144f-2244-487b-b7e5-7aa510cbe851" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="b47f144f-2244-487b-b7e5-7aa510cbe851">d363a5e1-c0e0-4ca4-9b2c-dc8c438774d1</ID>
		<Name ID="90221ceb-5b2f-4e2b-b6c7-980acec3e2e1">$ApprovalProcess_ControlMethods_ByLink</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="b47f144f-2244-487b-b7e5-7aa510cbe851">fa44abb0-857e-47e1-9d83-ff97ac93bb97</ID>
		<Name ID="90221ceb-5b2f-4e2b-b6c7-980acec3e2e1">$ApprovalProcess_ControlMethods_CardApprovalProcess</Name>
	</SchemeRecord>
</SchemeTable>