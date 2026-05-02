<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="0b988b75-1b59-4174-8a17-eb3c37f5c8d0" Name="WidgetTemplatesEditors" Group="Dashboards" InstanceType="Cards" ContentType="Collections">
	<Description>Список ролей, которым доступно редактирование шаблонов</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="0b988b75-1b59-0074-2000-0b3c37f5c8d0" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<Description>Идентификатор карточки</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="0b988b75-1b59-0174-4000-0b3c37f5c8d0" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор карточки</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="0b988b75-1b59-0074-3100-0b3c37f5c8d0" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор записи</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="53739e87-260a-4974-98d9-92bf6673a8aa" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" NormalizationSourceID="58e79fc4-a1d3-4739-b2c2-44812b44c82a">
		<Description>Ссылка на роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="53739e87-260a-0074-4000-02bf6673a8aa" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b">
			<Description>Идентификатор роли</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="1deda17a-4126-4aaa-b58f-32125b8571db" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0">
			<Description>Название роли</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="0b988b75-1b59-0074-5000-0b3c37f5c8d0" Name="pk_WidgetTemplatesEditors">
		<SchemeIndexedColumn Column="0b988b75-1b59-0074-3100-0b3c37f5c8d0" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="0b988b75-1b59-0074-7000-0b3c37f5c8d0" Name="idx_WidgetTemplatesEditors_ID" IsClustered="true">
		<SchemeIndexedColumn Column="0b988b75-1b59-0174-4000-0b3c37f5c8d0" />
	</SchemeIndex>
	<SchemeIndex ID="899b072a-92fc-4a0b-97ff-a2ce26fe613b" Name="ndx_WidgetTemplatesEditors_RoleID">
		<SchemeIndexedColumn Column="53739e87-260a-0074-4000-02bf6673a8aa" />
	</SchemeIndex>
</SchemeTable>