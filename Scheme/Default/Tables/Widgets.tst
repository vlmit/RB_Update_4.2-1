<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="44c27d15-a840-476e-a4fb-e9e4aa7f077b" Name="Widgets" Group="Dashboards" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="44c27d15-a840-006e-2000-09e4aa7f077b" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<Description>Идентификатор дашборда</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="44c27d15-a840-016e-4000-09e4aa7f077b" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор дашборда</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="44c27d15-a840-006e-3100-09e4aa7f077b" Name="RowID" Type="Guid Not Null">
		<Description>Идентификатор виджета</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3d6c3c36-5545-4e43-9180-f8594aedac0f" Name="Settings" Type="BinaryJson Null">
		<Description>Сериализованные настройки виджета</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="15c843a4-4959-4809-a6ff-10446c74901b" Name="Content" Type="BinaryJson Null">
		<Description>Сериализованный контент виджета</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="aa42882e-3d24-47e0-bb3b-0db2aaf797f7" Name="Type" Type="String(256) Not Null">
		<Description>Название типа виджета</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="42594051-ed14-4c74-8370-52005d3e9351" Name="Version" Type="Int32 Not Null">
		<Description>Версия виджета</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="cbace907-55f5-4ace-9769-f68be1bc3dc3" Name="Origin" Type="Reference(Typified) Null" ReferencedTable="44c27d15-a840-476e-a4fb-e9e4aa7f077b" WithForeignKey="false">
		<Description>Ссылка на оригинальный виджет, содержащий контент</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="cbace907-55f5-00ce-4000-068be1bc3dc3" Name="OriginRowID" Type="Guid Null" ReferencedColumn="44c27d15-a840-006e-3100-09e4aa7f077b">
			<Description>Идентификатор оригинального виджета</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="44c27d15-a840-006e-5000-09e4aa7f077b" Name="pk_Widgets">
		<SchemeIndexedColumn Column="44c27d15-a840-006e-3100-09e4aa7f077b" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="44c27d15-a840-006e-7000-09e4aa7f077b" Name="idx_Widgets_ID" IsClustered="true">
		<SchemeIndexedColumn Column="44c27d15-a840-016e-4000-09e4aa7f077b" />
	</SchemeIndex>
	<SchemeIndex ID="cb05dd8d-bc59-4bbb-af23-0ffcf1bbbe5e" Name="ndx_Widgets_OriginRowID">
		<SchemeIndexedColumn Column="cbace907-55f5-00ce-4000-068be1bc3dc3" />
	</SchemeIndex>
</SchemeTable>