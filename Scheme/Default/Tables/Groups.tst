<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="f3322737-cb7f-43aa-aa14-459e5087616a" Name="Groups" Group="System">
	<Description>Groups and their settings.</Description>
	<SchemePhysicalColumn ID="ebbd408d-04dd-4ccf-80e8-a11f1c6fc89d" Name="ID" Type="Guid Not Null">
		<Description>Unique identifier of a group.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="587430af-d94c-4379-b05b-8271bfbaa8ee" Name="Name" Type="String(128) Not Null">
		<Description>Display name of a group. Can be a localization string or a string with localization placeholders.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d5320ed2-4b4c-4c10-ab6d-3dc4456d11ae" Name="Description" Type="String(Max) Null">
		<Description>Description of a group. Can be a localization string or a string with localization placeholders.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d88edb73-c9cc-4623-bfd1-a96be671df27" Name="Category" Type="String(256) Not Null">
		<Description>Category descriptor for a group, example: ai/general.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="29d9ffc5-4767-429d-9121-b73ac0b9d620" Name="HiddenGroup" Type="Boolean Not Null">
		<Description>Option whether a group is hidden from the groups UI.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="18c04a87-22b2-464d-827e-5e9b80d97f90" Name="HiddenRole" Type="Boolean Not Null">
		<Description>Option whether a group's role is hidden when selected the roles in UI.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ecc537b0-4be7-4737-bc97-9a1eac2fec6c" Name="Modified" Type="DateTime Not Null">
		<Description>Date/time when a group was modified last.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="ecef5748-3122-4af5-b0a9-5e5707c572fa" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<Description>User's identifier who've modified a group last.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ecef5748-3122-00f5-4000-0e5707c572fa" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="1d48b8af-e456-4c69-825e-d8a49ea3e8ea" Name="Hash" Type="Binary(32) Null">
		<Description>SHA256 hash on group's settings and its system admins and members. Used to determine if the group was changed during initialization (web services startup). Null, if group should be updated when initializing regardless of its current settings.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="8a8d8a56-b64c-4a42-9860-d4f80af337d5" Name="pk_Groups" IsClustered="true">
		<SchemeIndexedColumn Column="ebbd408d-04dd-4ccf-80e8-a11f1c6fc89d" />
	</SchemePrimaryKey>
	<SchemeIndex ID="7d59fff2-cab6-4e5b-b2c5-74c7b780e519" Name="ndx_Groups_ID">
		<Description>Used to determine which groups should be updated during initialization (web services startup).</Description>
		<SchemeIndexedColumn Column="ebbd408d-04dd-4ccf-80e8-a11f1c6fc89d" />
		<SchemeIncludedColumn Column="1d48b8af-e456-4c69-825e-d8a49ea3e8ea" />
	</SchemeIndex>
	<SchemeIndex ID="70c67418-c3dd-4745-b3d9-160c74609c87" Name="ndx_Groups_Category">
		<SchemeIndexedColumn Column="d88edb73-c9cc-4623-bfd1-a96be671df27" />
	</SchemeIndex>
</SchemeTable>