<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="14d2c937-f1c0-4db4-bd7c-3d907ea0b01b" Name="TwoFactorAuthTypes" Group="System">
	<Description>Two-factor authentication types</Description>
	<SchemePhysicalColumn ID="7749e823-fddd-4086-9c40-a631a2ef0c0a" Name="ID" Type="Guid Not Null">
		<Description>Type identifier</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ab9facd3-f116-44c1-b2c7-966d6cba2623" Name="Name" Type="String(256) Not Null">
		<Description>Type name</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="27ce4347-9aab-4eb1-854f-d97d21feddec" Name="pk_TwoFactorAuthTypes">
		<SchemeIndexedColumn Column="7749e823-fddd-4086-9c40-a631a2ef0c0a" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="24a6076f-c3bb-4764-8343-ba5840d14a8b" Name="ndx_TwoFactorAuthTypes_Name">
		<SchemeIndexedColumn Column="ab9facd3-f116-44c1-b2c7-966d6cba2623" />
	</SchemeUniqueKey>
	<SchemeRecord>
		<ID ID="7749e823-fddd-4086-9c40-a631a2ef0c0a">5dde924c-61e0-4b3e-8058-217d1ca707ff</ID>
		<Name ID="ab9facd3-f116-44c1-b2c7-966d6cba2623">$Enum_TwoFactorAuthTypes_Email</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="7749e823-fddd-4086-9c40-a631a2ef0c0a">fef9bdb2-c3b3-4e18-9b85-d97f4eb4a365</ID>
		<Name ID="ab9facd3-f116-44c1-b2c7-966d6cba2623">$Enum_TwoFactorAuthTypes_TOTP</Name>
	</SchemeRecord>
</SchemeTable>