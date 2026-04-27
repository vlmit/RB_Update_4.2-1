<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="eac3013d-02ea-4a26-ab28-75af8ea61e4e" Name="ApprovalProcessInfoApprovers" Group="ApprovalProcess" IsVirtual="true" InstanceType="Tasks" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="eac3013d-02ea-0026-2000-05af8ea61e4e" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="eac3013d-02ea-0126-4000-05af8ea61e4e" Name="ID" Type="Guid Not Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="eac3013d-02ea-0026-3100-05af8ea61e4e" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="ef8e49d4-8a26-4253-bb64-3e5b0beaa500" Name="Approver" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ef8e49d4-8a26-0053-4000-0e5b0beaa500" Name="ApproverID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="46717892-e7e7-4456-9f97-694c4779af6d" Name="ApproverName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
		<SchemePhysicalColumn ID="05299905-2de2-409e-a0de-f5d3413e9b5a" Name="ApproverTypeID" Type="Int32 Not Null" />
		<SchemePhysicalColumn ID="fa6642bf-7f1e-4421-8c25-ba1cfea98705" Name="ApproverDescription" Type="String(Max) Null" />
		<SchemePhysicalColumn ID="4fa38fff-c2e7-4dcc-a73e-32c73418496e" Name="ApproverParent" Type="String(Max) Null" />
		<SchemePhysicalColumn ID="a4a5c9d0-e02c-401b-bcf1-ed6b639ce25f" Name="ApproverHidden" Type="Boolean Null" />
		<SchemePhysicalColumn ID="e9e73d07-167a-498b-89aa-76c24f2c7cda" Name="ApproverDelegated" Type="Boolean Null" />
		<SchemePhysicalColumn ID="10171ff6-ff58-4151-93af-15b5aa0e1a23" Name="ApproverCompletedByDeputy" Type="Boolean Null" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="d312c0d5-4637-47f4-837c-61d05b09bc6a" Name="Cycle" Type="Int32 Null" />
	<SchemePhysicalColumn ID="5a1394a5-04ec-4f63-ae96-3811e820a976" Name="State" Type="String(Max) Not Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="eac3013d-02ea-0026-5000-05af8ea61e4e" Name="pk_ApprovalProcessInfoApprovers">
		<SchemeIndexedColumn Column="eac3013d-02ea-0026-3100-05af8ea61e4e" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="eac3013d-02ea-0026-7000-05af8ea61e4e" Name="idx_ApprovalProcessInfoApprovers_ID" IsClustered="true">
		<SchemeIndexedColumn Column="eac3013d-02ea-0126-4000-05af8ea61e4e" />
	</SchemeIndex>
</SchemeTable>