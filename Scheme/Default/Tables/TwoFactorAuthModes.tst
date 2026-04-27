<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="cd2701ae-186d-426f-bdc0-e024bfd3e426" Name="TwoFactorAuthModes" Group="System">
	<Description>Two-factor authentication modes</Description>
	<SchemePhysicalColumn ID="ea903807-a197-4987-8a70-64adc2970f5b" Name="ID" Type="Int32 Not Null">
		<Description>Mode identifier</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1426da96-a2f9-4a17-8ef1-dc78a22ba2f9" Name="Name" Type="String(64) Not Null">
		<Description>Mode name</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="a2c9568c-5288-4c02-adf7-1ce8344a6513" Name="pk_TwoFactorAuthModes">
		<SchemeIndexedColumn Column="ea903807-a197-4987-8a70-64adc2970f5b" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="3420d601-9137-4c35-bd76-e6288058906c" Name="ndx_TwoFactorAuthModes_Name">
		<SchemeIndexedColumn Column="1426da96-a2f9-4a17-8ef1-dc78a22ba2f9" />
	</SchemeUniqueKey>
	<SchemeRecord>
		<ID ID="ea903807-a197-4987-8a70-64adc2970f5b">0</ID>
		<Name ID="1426da96-a2f9-4a17-8ef1-dc78a22ba2f9">$Enum_TwoFactorAuthModes_Disabled</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="ea903807-a197-4987-8a70-64adc2970f5b">1</ID>
		<Name ID="1426da96-a2f9-4a17-8ef1-dc78a22ba2f9">$Enum_TwoFactorAuthModes_Optional</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="ea903807-a197-4987-8a70-64adc2970f5b">2</ID>
		<Name ID="1426da96-a2f9-4a17-8ef1-dc78a22ba2f9">$Enum_TwoFactorAuthModes_Required</Name>
	</SchemeRecord>
</SchemeTable>