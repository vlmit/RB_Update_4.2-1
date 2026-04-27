<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="874fb896-ba09-42f2-9218-f3df551bc98a" Name="SettingsUnits" Group="System">
	<Description>Table to store settings units</Description>
	<SchemePhysicalColumn ID="f5346aae-2f54-4f74-89d0-8ec71f812643" Name="ID" Type="Guid Not Null">
		<Description>Unique settings unit identifier</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="a79eea8a-69b2-4610-89ec-462eee28321d" Name="Parent" Type="Reference(Typified) Null" ReferencedTable="874fb896-ba09-42f2-9218-f3df551bc98a" WithForeignKey="false">
		<Description>Parent settings unit reference</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a79eea8a-69b2-0010-4000-062eee28321d" Name="ParentID" Type="Guid Null" ReferencedColumn="f5346aae-2f54-4f74-89d0-8ec71f812643">
			<Description>Parent settings unit unique identifier</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="275e6384-5883-47b9-9784-4cfb1279074f" Name="Group" Type="Reference(Typified) Not Null" ReferencedTable="f3322737-cb7f-43aa-aa14-459e5087616a" WithForeignKey="false">
		<Description>Group reference to check access permissions</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="275e6384-5883-00b9-4000-0cfb1279074f" Name="GroupID" Type="Guid Not Null" ReferencedColumn="ebbd408d-04dd-4ccf-80e8-a11f1c6fc89d">
			<Description>Group identifier to check access permissions</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="81321d9e-2fa7-4910-904e-f9a61fd5a813" Name="Name" Type="String(256) Not Null">
		<Description>Unique name of the settings unit</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ee9ea037-b34f-4e33-be29-cbc41c0df457" Name="Version" Type="Int32 Not Null">
		<Description>Current version of the settings unit</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="d911f781-8a82-470b-9343-379d17fbb21b" Name="df_SettingsUnits_Version" Value="1" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="2a95e13b-af96-475e-a9e5-f88d26409838" Name="Category" Type="String(256) Not Null">
		<Description>Path to the category separated by "/" where the settings unit is located</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="9fdb7335-0d52-4b5a-bfcf-e95093b79bbd" Name="Modified" Type="DateTime Not Null">
		<Description>Date and time of the last settings change</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="52aece52-e493-40a1-96a4-d9a783ea12b2" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false" NormalizationSourceID="77e9c8bb-bb2f-4636-99df-c91a0c07c72c">
		<Description>User who changed the settings</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="52aece52-e493-00a1-4000-09a783ea12b2" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>User identifier who changed the settings</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="7731f290-f4c3-4f84-9400-7cb801851bd1" Name="ModifiedByName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964">
			<Description>User name who changed the settings</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="7a5d2d34-e2bb-4c01-81f7-a05e277a643e" Name="SchemeVersion" Type="Int32 Not Null">
		<Description>Current version of the settings unit data structure</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="16ff06f9-8874-4771-9044-719ff5b75d7a" Name="df_SettingsUnits_SchemeVersion" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="07771bb4-142b-4dff-bf46-bb4f5a5a7404" Name="Data" Type="BinaryJson Null">
		<Description>Settings unit data serialized as JSON</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="342e84f6-9fc5-4244-8b30-f09035fdf4bd" Name="pk_SettingsUnits">
		<SchemeIndexedColumn Column="f5346aae-2f54-4f74-89d0-8ec71f812643" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="ba4d2ab0-33a8-46ee-8322-f801bd967efb" Name="ndx_SettingsUnits_NameParentID">
		<SchemeIndexedColumn Column="81321d9e-2fa7-4910-904e-f9a61fd5a813" />
		<SchemeIndexedColumn Column="a79eea8a-69b2-0010-4000-062eee28321d" />
	</SchemeUniqueKey>
	<SchemeIndex ID="2f10a673-0ad0-4688-af8f-57dbab9442f5" Name="ndx_SettingsUnits_Category">
		<SchemeIndexedColumn Column="2a95e13b-af96-475e-a9e5-f88d26409838" />
	</SchemeIndex>
</SchemeTable>