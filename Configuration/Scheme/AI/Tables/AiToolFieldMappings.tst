<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="54b30f43-f5ba-4748-af85-a891bb352352" Name="AiToolFieldMappings" Group="AI" InstanceType="Cards" ContentType="Collections">
	<Description>Маппинг полей.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="54b30f43-f5ba-0048-2000-0891bb352352" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="54b30f43-f5ba-0148-4000-0891bb352352" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="54b30f43-f5ba-0048-3100-0891bb352352" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="b39f9df4-22d3-46fa-a891-6407a65f1b64" Name="IsRequired" Type="Boolean Not Null">
		<Description>Обязательный.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="97d63e5d-bd15-4df5-9474-98bc964d9ed9" Name="df_AiToolFieldMappings_IsRequired" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="3e685165-32ef-4ab5-be12-5063c96b9252" Name="Section" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Секция.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3e685165-32ef-00b5-4000-0063c96b9252" Name="SectionID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="b892230e-3a6d-4052-9062-31489c80d7a2" Name="SectionName" Type="String(Max) Null" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="49f740e3-754e-4cda-beb2-f0ffc6478ce9" Name="Field" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Поле.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="49f740e3-754e-00da-4000-00ffc6478ce9" Name="FieldID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="91f894eb-c2b7-464d-8209-661b66fa09a6" Name="FieldName" Type="String(Max) Null" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="359b134a-083b-4bf0-9961-5809ce2d538b" Name="SearchService" Type="Reference(Typified) Null" ReferencedTable="251b7528-a0db-43f7-83c0-8fb0c45db41a">
		<Description>Тип используемого сервиса для поиска значения.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="359b134a-083b-00f0-4000-0809ce2d538b" Name="SearchServiceID" Type="Guid Null" ReferencedColumn="56970d6f-ddd2-4685-a76c-d1eea45938c0" />
		<SchemeReferencingColumn ID="b21a8535-c276-41fd-b149-93d70f49d932" Name="SearchServiceName" Type="String(128) Null" ReferencedColumn="03d30f9e-1afc-4780-9f57-33871a222eb3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="a9392071-cd1a-498a-9dd1-b75de8add06d" Name="Name" Type="String(64) Null">
		<Description>Название.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="7476828a-e11a-4f76-a349-3ad0cb84f0aa" Name="Description" Type="String(512) Null">
		<Description>Описание.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="b8ace0a9-e7cf-4d79-acb7-75e3fe49bdca" Name="Order" Type="Int16 Not Null">
		<Description>Порядок.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="14eff0cd-ba2f-4cca-b89e-6f7d8ed7e0f1" Name="df_AiToolFieldMappings_Order" Value="0" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="54b30f43-f5ba-0048-5000-0891bb352352" Name="pk_AiToolFieldMappings">
		<SchemeIndexedColumn Column="54b30f43-f5ba-0048-3100-0891bb352352" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="54b30f43-f5ba-0048-7000-0891bb352352" Name="idx_AiToolFieldMappings_ID" IsClustered="true">
		<SchemeIndexedColumn Column="54b30f43-f5ba-0148-4000-0891bb352352" />
	</SchemeIndex>
</SchemeTable>