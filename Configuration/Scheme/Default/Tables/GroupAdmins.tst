<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="2cef3664-43c5-4074-85d7-064015126ef5" Name="GroupAdmins" Group="System">
	<Description>Administrators who can modifiy a group. Can be users, roles (durable) and other groups.</Description>
	<SchemePhysicalColumn ID="a973c0ce-587c-400e-b7ac-c6b07e083edf" Name="ID" Type="Guid Not Null">
		<Description>Unique identifier of a group.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="32ec41fe-2f8e-480d-9918-e4239bc16942" Name="RowID" Type="Guid Not Null">
		<Description>Unique row identifier.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="25fb4ed7-7ea2-49d0-937f-e6f8a99ea064" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" WithForeignKey="false">
		<Description>Identifier of a group, members of which are included in the group as its administrators.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="25fb4ed7-7ea2-00d0-4000-06f8a99ea064" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="063391fe-a020-432a-afba-c08433c3e6c0" Name="FullPermissions" Type="Boolean Not Null">
		<Description>Option whether administrators can modify GroupAdmins, i.e., add or remove other administrators (re-delegate).</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c25c14a6-abb3-4af2-8fb9-97e2ebdfc23c" Name="Modified" Type="DateTime Not Null">
		<Description>Date/time when this row was modified last.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="721a7ba1-22d2-4fc1-9846-0bb98aef2c10" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<Description>User's identifier who've modified this row last.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="721a7ba1-22d2-00c1-4000-0bb98aef2c10" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="eecf21a7-ad2e-43ff-836f-511adcb67618" Name="IsSystem" Type="Boolean Not Null">
		<Description>Current member is determined by the system, i.e., it was set up in program's code.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="031e4481-ce7a-4b09-85b0-4e6a3fc77e00" Name="pk_GroupAdmins">
		<SchemeIndexedColumn Column="32ec41fe-2f8e-480d-9918-e4239bc16942" />
	</SchemePrimaryKey>
	<SchemeIndex ID="677b76f4-d505-4358-a135-0b1d0c2df664" Name="idx_GroupAdmins_ID" IsClustered="true">
		<SchemeIndexedColumn Column="a973c0ce-587c-400e-b7ac-c6b07e083edf" />
	</SchemeIndex>
	<SchemeIndex ID="7a1be3cf-c081-47d4-a407-d2aebcf8597f" Name="ndx_GroupAdmins_RoleID">
		<SchemeIndexedColumn Column="25fb4ed7-7ea2-00d0-4000-06f8a99ea064" />
	</SchemeIndex>
</SchemeTable>