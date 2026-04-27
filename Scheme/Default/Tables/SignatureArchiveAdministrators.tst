<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c9a8ccb9-5468-4fc1-a8cc-435651a9a486" Name="SignatureArchiveAdministrators" Group="System" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c9a8ccb9-5468-00c1-2000-035651a9a486" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c9a8ccb9-5468-01c1-4000-035651a9a486" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c9a8ccb9-5468-00c1-3100-035651a9a486" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="df999e8b-32a0-431d-9172-a776da720031" Name="Role" Type="Reference(Typified) Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" NormalizationSourceID="58e79fc4-a1d3-4739-b2c2-44812b44c82a">
		<Description>Ссылка на роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="df999e8b-32a0-001d-4000-0776da720031" Name="RoleID" Type="Guid Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
		<SchemeReferencingColumn ID="f498bbef-2224-4914-a61b-0e92821c334f" Name="RoleName" Type="String(128) Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="c9a8ccb9-5468-00c1-5000-035651a9a486" Name="pk_SignatureArchiveAdministrators">
		<SchemeIndexedColumn Column="c9a8ccb9-5468-00c1-3100-035651a9a486" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="c9a8ccb9-5468-00c1-7000-035651a9a486" Name="idx_SignatureArchiveAdministrators_ID" IsClustered="true">
		<SchemeIndexedColumn Column="c9a8ccb9-5468-01c1-4000-035651a9a486" />
	</SchemeIndex>
</SchemeTable>