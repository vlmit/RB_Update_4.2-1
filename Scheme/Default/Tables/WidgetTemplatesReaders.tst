<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="477bf78a-44c5-45e5-8f41-a05c28536d19" Name="WidgetTemplatesReaders" Group="Dashboards" InstanceType="Cards" ContentType="Collections">
	<Description>Список ролей, которым доступен шаблон</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="477bf78a-44c5-00e5-2000-005c28536d19" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<Description>Идентификатор дашборда</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="477bf78a-44c5-01e5-4000-005c28536d19" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор дашборда</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="477bf78a-44c5-00e5-3100-005c28536d19" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор записи</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="d7e90d0b-17e7-46c8-ba6a-c3f4da7a4a62" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b" NormalizationSourceID="58e79fc4-a1d3-4739-b2c2-44812b44c82a">
		<Description>Ссылка на роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d7e90d0b-17e7-00c8-4000-03f4da7a4a62" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b">
			<Description>Идентификатор роли</Description>
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="36d08d1e-9984-4804-9c17-eda167f1fbaa" Name="RoleName" Type="String(128) Not Null" ReferencedColumn="616d6b2e-35d5-424d-846b-618eb25962d0">
			<Description>Название роли</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="80725e68-6fad-4dc5-b84e-886e23f44c4b" Name="WidgetTemplate" Type="Reference(Typified) Not Null" ReferencedTable="81f82693-1ce7-433d-a554-1cca3e01843d">
		<Description>Ссылка на шаблон виджета</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="80725e68-6fad-00c5-4000-086e23f44c4b" Name="WidgetTemplateRowID" Type="Guid Not Null" ReferencedColumn="81f82693-1ce7-003d-3100-0cca3e01843d">
			<Description>Идентификатор шаблона виджета</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="477bf78a-44c5-00e5-5000-005c28536d19" Name="pk_WidgetTemplatesReaders">
		<SchemeIndexedColumn Column="477bf78a-44c5-00e5-3100-005c28536d19" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="477bf78a-44c5-00e5-7000-005c28536d19" Name="idx_WidgetTemplatesReaders_ID" IsClustered="true">
		<SchemeIndexedColumn Column="477bf78a-44c5-01e5-4000-005c28536d19" />
	</SchemeIndex>
	<SchemeIndex ID="2752624f-327d-4ff0-8359-00224a1fcb66" Name="ndx_WidgetTemplatesReaders_WidgetTemplateRowID">
		<SchemeIndexedColumn Column="80725e68-6fad-00c5-4000-086e23f44c4b" />
	</SchemeIndex>
	<SchemeIndex ID="e38d3b65-31cf-43e3-aa36-01597ff54a18" Name="ndx_WidgetTemplatesReaders_RoleID">
		<SchemeIndexedColumn Column="d7e90d0b-17e7-00c8-4000-03f4da7a4a62" />
	</SchemeIndex>
</SchemeTable>