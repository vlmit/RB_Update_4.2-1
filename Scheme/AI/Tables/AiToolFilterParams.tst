<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="0e274b40-2e24-4315-ab1e-b63c46986179" Name="AiToolFilterParams" Group="AI" InstanceType="Cards" ContentType="Collections">
	<Description>Параметры фильтрации для справочника действий.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="0e274b40-2e24-0015-2000-063c46986179" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="0e274b40-2e24-0115-4000-063c46986179" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="0e274b40-2e24-0015-3100-063c46986179" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="4ad6c789-b435-476b-b20c-85e78cfc0a49" Name="IsRequired" Type="Boolean Not Null">
		<Description>Обязательный.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="acde08dc-c56c-41c6-a11f-2ffa55a5e6cb" Name="df_AiToolFilterParams_IsRequired" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="7d6d2615-ffb3-4c3b-9877-1ef69f87cdb3" Name="ViewParameter" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Параметр представления.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7d6d2615-ffb3-003b-4000-0ef69f87cdb3" Name="ViewParameterID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="5029cfe3-5828-428b-90be-7f7ead1a3801" Name="ViewParameterAlias" Type="String(128) Null" />
		<SchemePhysicalColumn ID="13ee8bf8-1126-4cbb-be34-ead082a95ec5" Name="ViewParameterCaption" Type="String(128) Null" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="65bb63d7-36ba-4c89-ba43-b851cf68ee3c" Name="SearchService" Type="Reference(Typified) Null" ReferencedTable="251b7528-a0db-43f7-83c0-8fb0c45db41a">
		<Description>Тип используемого сервиса для поиска значения.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="65bb63d7-36ba-0089-4000-0851cf68ee3c" Name="SearchServiceID" Type="Guid Null" ReferencedColumn="56970d6f-ddd2-4685-a76c-d1eea45938c0" />
		<SchemeReferencingColumn ID="c3207e0a-665e-4eaf-a93e-c58b53677783" Name="SearchServiceName" Type="String(128) Null" ReferencedColumn="03d30f9e-1afc-4780-9f57-33871a222eb3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="89eb3952-25b6-4e60-8eea-59032576de90" Name="Name" Type="String(64) Null">
		<Description>Название.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="b50ac108-f919-408a-97c3-ca2f01b651d1" Name="Description" Type="String(512) Null">
		<Description>Описание.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="efa1e9ef-0cf5-4aa2-93e6-27dbfb94560e" Name="Order" Type="Int16 Not Null">
		<Description>Порядок.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="2760cb9e-972c-4f70-b33f-36b46ec53ceb" Name="df_AiToolFilterParams_Order" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="9577679a-5515-4f6f-9f4f-bf231a8d7cd4" Name="RangeMechanism" Type="Boolean Not Null">
		<Description>Использовать механизм поиска диапазонов.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="8743b76b-1e8a-4d3e-b088-f685b63df10d" Name="df_AiFilterParams_RangeMechanism" Value="false" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="0e274b40-2e24-0015-5000-063c46986179" Name="pk_AiToolFilterParams">
		<SchemeIndexedColumn Column="0e274b40-2e24-0015-3100-063c46986179" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="0e274b40-2e24-0015-7000-063c46986179" Name="idx_AiToolFilterParams_ID" IsClustered="true">
		<SchemeIndexedColumn Column="0e274b40-2e24-0115-4000-063c46986179" />
	</SchemeIndex>
</SchemeTable>