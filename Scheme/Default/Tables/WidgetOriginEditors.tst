<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="114555ad-161c-4ccb-9c1a-c19c0c21a4a3" Name="WidgetOriginEditors" Group="Dashboards" InstanceType="Cards" ContentType="Collections">
	<Description>Список ролей, которым доступно редактирование оригинального общего виджета</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="114555ad-161c-00cb-2000-019c0c21a4a3" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<Description>Идентификатор дашборда</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="114555ad-161c-01cb-4000-019c0c21a4a3" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор дашборда</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="114555ad-161c-00cb-3100-019c0c21a4a3" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор записи</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="dff3e546-671f-4f84-95d4-343ee0557201" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" NormalizationSourceID="58e79fc4-a1d3-4739-b2c2-44812b44c82a">
		<Description>Ссылка на роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="dff3e546-671f-0084-4000-043ee0557201" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b">
			<Description>Идентификатор роли</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="fed6c53e-1368-4d25-8d8f-37c83b7ea9a8" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0">
			<Description>Название роли</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="5e6f1243-357c-4946-a292-b7e76dec3034" Name="WidgetOrigin" Type="Reference(Typified) Not Null" ReferencedTable="81f82693-1ce7-433d-a554-1cca3e01843d">
		<Description>Ссылка на шаблон оригинального виджета</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="5e6f1243-357c-0046-4000-07e76dec3034" Name="WidgetOriginRowID" Type="Guid Not Null" ReferencedColumn="81f82693-1ce7-003d-3100-0cca3e01843d">
			<Description>Идентификатор шаблона оригинального виджета</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="114555ad-161c-00cb-5000-019c0c21a4a3" Name="pk_WidgetOriginEditors">
		<SchemeIndexedColumn Column="114555ad-161c-00cb-3100-019c0c21a4a3" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="114555ad-161c-00cb-7000-019c0c21a4a3" Name="idx_WidgetOriginEditors_ID" IsClustered="true">
		<SchemeIndexedColumn Column="114555ad-161c-01cb-4000-019c0c21a4a3" />
	</SchemeIndex>
	<SchemeIndex ID="384d95fd-a3d1-4a06-a907-f34f2d2b2f3f" Name="ndx_WidgetOriginEditors_WidgetOriginRowID">
		<SchemeIndexedColumn Column="5e6f1243-357c-0046-4000-07e76dec3034" />
	</SchemeIndex>
	<SchemeIndex ID="59e0f019-fe8d-416d-bb9c-20842b3e1251" Name="ndx_WidgetOriginEditors_RoleID">
		<SchemeIndexedColumn Column="dff3e546-671f-0084-4000-043ee0557201" />
	</SchemeIndex>
</SchemeTable>