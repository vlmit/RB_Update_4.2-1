<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="ab2ec272-eb83-429d-b4fc-155bb55f13dd" Name="ApprovalProcessInfo" Group="ApprovalProcess" IsVirtual="true" InstanceType="Tasks" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="ab2ec272-eb83-009d-2000-055bb55f13dd" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ab2ec272-eb83-019d-4000-055bb55f13dd" Name="ID" Type="Guid Not Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="f4967635-c802-43d5-b40d-3e8a018249bf" Name="Cycle" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="4a9a8b18-b321-4341-b288-222d85644162" Name="State" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="2ed3a97f-6535-41e8-821c-6dafad1209c5" Name="Approved" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="3d4333f2-1686-47dc-82cd-bb13e778a061" Name="Disapproved" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="73b4f61c-b500-47e9-b2ef-b49c0c25198a" Name="Pending" Type="Int32 Not Null" />
	<SchemeComplexColumn ID="4f528bfc-e0e5-4fdf-b20c-f33a4caa85a6" Name="Instance" Type="Reference(Typified) Not Null" ReferencedTable="d3b547df-315d-4cda-a345-668428c3a14d" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="4f528bfc-e0e5-00df-4000-033a4caa85a6" Name="InstanceID" Type="Guid Not Null" ReferencedColumn="65623483-81df-4792-a9a3-bf17ae5ec73a" />
		<SchemePhysicalColumn ID="038a633d-a918-425e-98fb-a641f2bf8f09" Name="InstanceName" Type="String(Max) Not Null" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="42221a88-cc46-45b2-a722-3a49e2a8895e" Name="WithoutTask" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="f06644fe-f33d-4390-baba-0aa8b81e7dda" Name="StateText" Type="String(Max) Not Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="ab2ec272-eb83-009d-5000-055bb55f13dd" Name="pk_ApprovalProcessInfo" IsClustered="true">
		<SchemeIndexedColumn Column="ab2ec272-eb83-019d-4000-055bb55f13dd" />
	</SchemePrimaryKey>
</SchemeTable>