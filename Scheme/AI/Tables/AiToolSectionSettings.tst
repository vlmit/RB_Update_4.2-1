<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="6d6fc9b0-e7ca-4b61-9721-9830af32cd88" Name="AiToolSectionSettings" Group="AI" InstanceType="Cards" ContentType="Collections">
	<Description>Таблица настроек секций в карточке инструмента.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="6d6fc9b0-e7ca-0061-2000-0830af32cd88" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="6d6fc9b0-e7ca-0161-4000-0830af32cd88" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="6d6fc9b0-e7ca-0061-3100-0830af32cd88" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="8a2cdf2f-909d-43f4-935e-ebec68c02ba7" Name="Section" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Секция.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="8a2cdf2f-909d-00f4-4000-0bec68c02ba7" Name="SectionID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="88b6d931-fb39-43a1-975c-34ecef02f7a7" Name="SectionName" Type="String(Max) Null" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="059a3617-521c-4f94-bad3-d47ff07cc43d" Name="Name" Type="String(64) Null">
		<Description>Название.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="39e80a09-a586-4d89-bf8f-0193e511fc66" Name="IsRequired" Type="Boolean Null">
		<Description>Обязательность.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d746c6a3-028e-4942-aa5c-84482abf6bbe" Name="IsTable" Type="Boolean Null">
		<Description>Флаг, является ли секция таблицей.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="f3bce4e0-3913-4a6f-b6e1-01f92c2e810a" Name="DisplayMethod" Type="Reference(Typified) Null" ReferencedTable="924e1a98-cdb4-49bb-9b83-2729cd787bea">
		<Description>Способ вывода списка.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="f3bce4e0-3913-006f-4000-01f92c2e810a" Name="DisplayMethodID" Type="Int16 Null" ReferencedColumn="c6567a18-a108-4114-9ec8-2d4218d16ad2" />
		<SchemeReferencingColumn ID="804b909b-991c-46b6-b5f8-bd2238f08302" Name="DisplayMethodName" Type="String(64) Null" ReferencedColumn="4fe309ee-179a-4d49-8883-e708a1edc760" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="21c0a9ee-dce6-44be-8211-a84507c91724" Name="IntField" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Колонка с целочисленным идентификатором.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="21c0a9ee-dce6-00be-4000-084507c91724" Name="IntFieldID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="1abb8fb7-fb5a-4648-9fb2-10e01e9b19f3" Name="IntFieldName" Type="String(128) Null" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="96fb7819-c6f8-4f11-94c5-7f42c83b6fb7" Name="Order" Type="Int16 Not Null">
		<Description>Порядок</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="31de686c-49d6-4863-b955-588dd9080eba" Name="df_AiToolSectionSettings_Order" Value="0" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="6d6fc9b0-e7ca-0061-5000-0830af32cd88" Name="pk_AiToolSectionSettings">
		<SchemeIndexedColumn Column="6d6fc9b0-e7ca-0061-3100-0830af32cd88" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="6d6fc9b0-e7ca-0061-7000-0830af32cd88" Name="idx_AiToolSectionSettings_ID" IsClustered="true">
		<SchemeIndexedColumn Column="6d6fc9b0-e7ca-0161-4000-0830af32cd88" />
	</SchemeIndex>
</SchemeTable>