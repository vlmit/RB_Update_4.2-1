<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="81f82693-1ce7-433d-a554-1cca3e01843d" Name="WidgetTemplates" Group="Dashboards" InstanceType="Cards" ContentType="Collections">
	<Description>Шаблоны виджетов</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="81f82693-1ce7-003d-2000-0cca3e01843d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<Description>Идентификатор дашборда</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="81f82693-1ce7-013d-4000-0cca3e01843d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор дашборда</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="81f82693-1ce7-003d-3100-0cca3e01843d" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор шаблона</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="9713f989-a8ab-4187-b782-e066c19a17cf" Name="Widget" Type="Reference(Typified) Not Null" ReferencedTable="44c27d15-a840-476e-a4fb-e9e4aa7f077b">
		<Description>Ссылка на виджет</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9713f989-a8ab-0087-4000-0066c19a17cf" Name="WidgetRowID" Type="Guid Not Null" ReferencedColumn="44c27d15-a840-006e-3100-09e4aa7f077b">
			<Description>Идентификатор виджета</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="da53f124-7dc3-45f5-8561-d968755ab339" Name="Name" Type="String(128) Not Null">
		<Description>Название шаблона</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="91913c6d-ae5c-4274-87b0-0ab381faa365" Name="Description" Type="String(256) Null">
		<Description>Краткое описание шаблона</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="7fbb3250-67c9-4d92-99ba-d3efb302002f" Name="AllowForAllEmployees" Type="Boolean Not Null">
		<Description>Доступен для всех сотрудников</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="05ce2949-8616-4425-87ea-c9223179ddde" Name="Kind" Type="Reference(Typified) Not Null" ReferencedTable="436af7d9-c1dd-4e1a-b00e-5cf06c6d7111">
		<Description>Вид шаблона</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="05ce2949-8616-0025-4000-09223179ddde" Name="KindID" Type="Int32 Not Null" ReferencedColumn="79d8c2c8-02c4-471a-91c1-9fa1a3bea25a">
			<Description>Идентификатор вида шаблона</Description>
			<SchemeDefaultConstraint IsPermanent="true" ID="e9a72e7f-7512-4bbd-9b8d-34afb6b7dd05" Name="df_WidgetTemplates_KindID" Value="0" />
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="81f82693-1ce7-003d-5000-0cca3e01843d" Name="pk_WidgetTemplates">
		<SchemeIndexedColumn Column="81f82693-1ce7-003d-3100-0cca3e01843d" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="81f82693-1ce7-003d-7000-0cca3e01843d" Name="idx_WidgetTemplates_ID" IsClustered="true">
		<SchemeIndexedColumn Column="81f82693-1ce7-013d-4000-0cca3e01843d" />
	</SchemeIndex>
	<SchemeIndex ID="63a53cdc-f1b8-4c9f-b768-68f16fc13282" Name="ndx_WidgetTemplates_KindID">
		<SchemeIndexedColumn Column="05ce2949-8616-0025-4000-09223179ddde" />
	</SchemeIndex>
</SchemeTable>