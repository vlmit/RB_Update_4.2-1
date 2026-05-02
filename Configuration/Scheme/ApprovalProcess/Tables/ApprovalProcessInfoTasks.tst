<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="b43c68d0-a317-4aad-a400-dda21b33aa29" Name="ApprovalProcessInfoTasks" Group="ApprovalProcess" IsVirtual="true" InstanceType="Tasks" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="b43c68d0-a317-00ad-2000-0da21b33aa29" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b43c68d0-a317-01ad-4000-0da21b33aa29" Name="ID" Type="Guid Not Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="b43c68d0-a317-00ad-3100-0da21b33aa29" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="4b28f562-c0f5-426d-bde9-745266f6a6bf" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="b0538ece-8468-4d0b-8b4e-5a1d43e024db" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="4b28f562-c0f5-006d-4000-045266f6a6bf" Name="TypeID" Type="Guid Not Null" ReferencedColumn="a628a864-c858-4200-a6b7-da78c8e6e1f4" />
		<SchemeReferencingColumn ID="b4f4d88e-a487-4c79-bfb2-149e724ed7db" Name="TypeCaption" Type="String(128) Not Null" ReferencedColumn="0a02451e-2e06-4001-9138-b4805e641afa" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="7cede05c-919f-466b-89e9-7b8e77e53495" Name="CurrentPerformer" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7cede05c-919f-006b-4000-0b8e77e53495" Name="CurrentPerformerID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
		<SchemeReferencingColumn ID="e920d77b-d081-4547-9686-d71d6f2be057" Name="CurrentPerformerName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="ea5f5816-1f4e-409a-9e4f-f9508a91fb24" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="057a85c8-c20f-430b-bd3b-6ea9f9fb82ee">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ea5f5816-1f4e-009a-4000-09508a91fb24" Name="StateID" Type="Int16 Not Null" ReferencedColumn="413df3de-fc7a-476d-a604-77ee5135e7bc" />
		<SchemeReferencingColumn ID="ac7f4d66-608c-462a-b689-90b7229412cc" Name="StateName" Type="String(128) Not Null" ReferencedColumn="e715302d-7604-416a-b7f6-8c8d99b48a17" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="496a794c-8440-483f-b3cd-f7c817c5a758" Name="Created" Type="DateTime Not Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="b43c68d0-a317-00ad-5000-0da21b33aa29" Name="pk_ApprovalProcessInfoTasks">
		<SchemeIndexedColumn Column="b43c68d0-a317-00ad-3100-0da21b33aa29" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="b43c68d0-a317-00ad-7000-0da21b33aa29" Name="idx_ApprovalProcessInfoTasks_ID" IsClustered="true">
		<SchemeIndexedColumn Column="b43c68d0-a317-01ad-4000-0da21b33aa29" />
	</SchemeIndex>
</SchemeTable>