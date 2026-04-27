<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="89532789-a0b6-44be-9807-570a2583d994" Name="TwoFactorAuthAllowedTypes" Group="System" InstanceType="Cards" ContentType="Collections">
	<Description>Allowed two-factor authentication types at platform</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="89532789-a0b6-00be-2000-070a2583d994" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<Description>Card identifier</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="89532789-a0b6-01be-4000-070a2583d994" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Card identifier</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="89532789-a0b6-00be-3100-070a2583d994" Name="RowID" Type="Guid Not Null">
		<Description>Record identifier</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="b23ecfc7-8435-4d7b-b91a-d87b043c6b7f" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="14d2c937-f1c0-4db4-bd7c-3d907ea0b01b">
		<Description>Type 2FA</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b23ecfc7-8435-007b-4000-087b043c6b7f" Name="TypeID" Type="Guid Not Null" ReferencedColumn="7749e823-fddd-4086-9c40-a631a2ef0c0a">
			<Description>Type identifier</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="5694b487-0e67-40f9-b35e-a31a865934b2" Name="TypeName" Type="String(256) Not Null" ReferencedColumn="ab9facd3-f116-44c1-b2c7-966d6cba2623">
			<Description>Type name</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="89532789-a0b6-00be-5000-070a2583d994" Name="pk_TwoFactorAuthAllowedTypes">
		<SchemeIndexedColumn Column="89532789-a0b6-00be-3100-070a2583d994" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="89532789-a0b6-00be-7000-070a2583d994" Name="idx_TwoFactorAuthAllowedTypes_ID" IsClustered="true">
		<SchemeIndexedColumn Column="89532789-a0b6-01be-4000-070a2583d994" />
	</SchemeIndex>
	<SchemeIndex ID="2ff7f35a-4031-4ba2-8183-d468d1979ae0" Name="ndx_TwoFactorAuthAllowedTypes_TypeID" IsUnique="true">
		<SchemeIndexedColumn Column="b23ecfc7-8435-007b-4000-087b043c6b7f" />
		<SchemeIncludedColumn Column="5694b487-0e67-40f9-b35e-a31a865934b2" />
	</SchemeIndex>
</SchemeTable>