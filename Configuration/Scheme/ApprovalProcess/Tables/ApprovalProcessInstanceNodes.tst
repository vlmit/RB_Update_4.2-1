<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="f5fcc2bd-e398-4170-a467-e23c587d8ee0" Name="ApprovalProcessInstanceNodes" Group="ApprovalProcess">
	<SchemePhysicalColumn ID="2f954196-b341-457b-80a6-b1aea6f4b535" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="933b6940-57a1-493e-a26e-efbc4d71dffe" Name="Instance" Type="Reference(Typified) Not Null" ReferencedTable="d3b547df-315d-4cda-a345-668428c3a14d">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="933b6940-57a1-003e-4000-0fbc4d71dffe" Name="InstanceID" Type="Guid Not Null" ReferencedColumn="65623483-81df-4792-a9a3-bf17ae5ec73a" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="3bf04af3-a8d7-4f6f-9ea1-2087e915d704" Name="NodeID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="4af79baa-2f3d-40be-9466-4ef9a597b17b" Name="NodeData" Type="BinaryJson Not Null" />
	<SchemePhysicalColumn ID="2fef5dfe-d2db-4bfa-b41e-fd932d48fafe" Name="NodeType" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="0195fdce-af66-4bb6-9be0-5c7777edd668" Name="pk_ApprovalProcessInstanceNodes">
		<SchemeIndexedColumn Column="2f954196-b341-457b-80a6-b1aea6f4b535" />
	</SchemePrimaryKey>
</SchemeTable>