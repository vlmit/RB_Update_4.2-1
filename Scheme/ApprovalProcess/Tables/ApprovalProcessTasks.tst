<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="5c209fc5-c16f-41af-bebc-1e125ebf3a4c" Name="ApprovalProcessTasks" Group="ApprovalProcess">
	<SchemePhysicalColumn ID="f4b50d47-0744-4f20-b1d0-0b976b273468" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="a7c979f9-8315-48ec-bb27-b8e4d56cd283" Name="Node" Type="Reference(Typified) Not Null" ReferencedTable="f5fcc2bd-e398-4170-a467-e23c587d8ee0">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a7c979f9-8315-00ec-4000-08e4d56cd283" Name="NodeID" Type="Guid Not Null" ReferencedColumn="2f954196-b341-457b-80a6-b1aea6f4b535" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="8c945993-80b6-427f-9c2b-5535bf3f7dad" Name="Task" Type="Reference(Typified) Not Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="8c945993-80b6-007f-4000-0535bf3f7dad" Name="TaskID" Type="Guid Not Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8" />
	</SchemeComplexColumn>
	<SchemePrimaryKey ID="c45bb89c-914c-4d7a-a05f-70c743595e5e" Name="pk_ApprovalProcessTasks">
		<SchemeIndexedColumn Column="f4b50d47-0744-4f20-b1d0-0b976b273468" />
	</SchemePrimaryKey>
</SchemeTable>