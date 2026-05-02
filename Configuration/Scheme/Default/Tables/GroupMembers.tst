<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="6195ff16-f65d-4328-88eb-10f9cec315af" Name="GroupMembers" Group="System">
	<Description>Members inside a group, i.e., users, roles (durable) and other groups.</Description>
	<SchemePhysicalColumn ID="cb279872-cb9f-4528-bf22-0d9bc108e7e6" Name="ID" Type="Guid Not Null">
		<Description>Unique identifier of a group.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e4438431-ad37-4ba8-a8b2-863a1cf76d40" Name="RowID" Type="Guid Not Null">
		<Description>Unique row identifier.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="7b298093-3af9-41a1-a805-c3a13ff82f57" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" WithForeignKey="false">
		<Description>Identifier of a group, members of which are included in the group as its members.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7b298093-3af9-00a1-4000-03a13ff82f57" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="128feff4-0ccb-4050-9e2e-57930b982b42" Name="Modified" Type="DateTime Not Null">
		<Description>Date/time when this row was modified last.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="5c450d4c-0ef6-41a5-a1ca-8742e952e783" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<Description>User's identifier who've modified this row last.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="5c450d4c-0ef6-00a5-4000-0742e952e783" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="9bdc88cd-f29c-443d-98be-de71f2e67b0e" Name="IsSystem" Type="Boolean Not Null">
		<Description>Current member is determined by the system, i.e., it was set up in program's code.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="18f81042-922f-4784-bf8f-a5f841b7d248" Name="pk_GroupMembers">
		<SchemeIndexedColumn Column="e4438431-ad37-4ba8-a8b2-863a1cf76d40" />
	</SchemePrimaryKey>
	<SchemeIndex ID="bb8bb1a5-a72e-4a74-a17b-ce38f3542429" Name="idx_GroupMembers_ID" IsClustered="true">
		<SchemeIndexedColumn Column="cb279872-cb9f-4528-bf22-0d9bc108e7e6" />
	</SchemeIndex>
	<SchemeIndex ID="9d653472-74c6-4a93-95bf-c0292539ea9a" Name="ndx_GroupMembers_RoleID">
		<SchemeIndexedColumn Column="7b298093-3af9-00a1-4000-03a13ff82f57" />
	</SchemeIndex>
</SchemeTable>