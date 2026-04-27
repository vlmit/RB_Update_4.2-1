<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="685b1e88-d57c-4c47-aded-685df64c2c1c" Name="TwoFactorAuthUserTypes" Group="System" InstanceType="Cards" ContentType="Collections">
	<Description>User-supported two-factor authentication types</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="685b1e88-d57c-0047-2000-085df64c2c1c" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<Description>Card identifier</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="685b1e88-d57c-0147-4000-085df64c2c1c" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Card identifier</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="685b1e88-d57c-0047-3100-085df64c2c1c" Name="RowID" Type="Guid Not Null">
		<Description>Record identifier</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="a8ae196a-43bf-44df-acab-97e638d2ac22" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<Description>User who changed type settings</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a8ae196a-43bf-00df-4000-07e638d2ac22" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>User identifier</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="bd6d37ac-aa91-4faf-8bb6-6604cb735b27" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="14d2c937-f1c0-4db4-bd7c-3d907ea0b01b">
		<Description>Type 2FA</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="bd6d37ac-aa91-00af-4000-0604cb735b27" Name="TypeID" Type="Guid Not Null" ReferencedColumn="7749e823-fddd-4086-9c40-a631a2ef0c0a">
			<Description>Type identifier</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="a02389d4-89c3-4a8a-a2d3-5608e71c1fe2" Name="TypeName" Type="String(256) Not Null" ReferencedColumn="ab9facd3-f116-44c1-b2c7-966d6cba2623">
			<Description>Type name</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="8682f422-58cc-4f5c-89c0-c7f546ede5c1" Name="IsDefault" Type="Boolean Not Null">
		<Description>Sign indicates that 2FA type used by default</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="0e84c070-ab26-48ff-91dd-d4934c006758" Name="df_TwoFactorAuthUserTypes_IsDefault" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c8c366c3-f135-4958-af87-1049544254c6" Name="Settings" Type="Json Null">
		<Description>Type 2FA settings</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="685b1e88-d57c-0047-5000-085df64c2c1c" Name="pk_TwoFactorAuthUserTypes">
		<SchemeIndexedColumn Column="685b1e88-d57c-0047-3100-085df64c2c1c" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="685b1e88-d57c-0047-7000-085df64c2c1c" Name="idx_TwoFactorAuthUserTypes_ID" IsClustered="true">
		<SchemeIndexedColumn Column="685b1e88-d57c-0147-4000-085df64c2c1c" />
	</SchemeIndex>
	<SchemeIndex ID="8cfe146d-317d-4c14-b403-c3381fc3e7af" Name="ndx_TwoFactorAuthUserTypes_TypeID">
		<SchemeIndexedColumn Column="bd6d37ac-aa91-00af-4000-0604cb735b27" />
		<SchemeIncludedColumn Column="a02389d4-89c3-4a8a-a2d3-5608e71c1fe2" />
	</SchemeIndex>
	<SchemeIndex ID="2a9d0fbb-298b-4cc9-acd4-64dba14f0542" Name="ndx_TwoFactorAuthUserTypes_IsDefault">
		<SchemeIndexedColumn Column="8682f422-58cc-4f5c-89c0-c7f546ede5c1" />
		<SchemeIncludedColumn Column="bd6d37ac-aa91-00af-4000-0604cb735b27" />
		<SchemeIncludedColumn Column="a02389d4-89c3-4a8a-a2d3-5608e71c1fe2" />
		<SchemeIncludedColumn Column="c8c366c3-f135-4958-af87-1049544254c6" />
	</SchemeIndex>
</SchemeTable>