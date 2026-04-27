<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="4fda8c4e-1b71-45f0-96f1-5c785cea7509" Name="AiTool" Group="AI" InstanceType="Cards" ContentType="Entries">
	<Description>Описание карточного инструмента ИИ.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="4fda8c4e-1b71-00f0-2000-0c785cea7509" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="4fda8c4e-1b71-01f0-4000-0c785cea7509" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="1cdec9c6-467e-48c1-a7f1-fcf43f9d8abd" Name="Name" Type="String(64) Not Null">
		<Description>Название действия.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="8318375f-83ad-45bb-9cbe-32da7b407c9b" Name="Code" Type="String(64) Not Null">
		<Description>Код действия.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="122ca8ce-66ea-416a-b38d-d5bda3a66a45" Name="AdditionalDescription" Type="String(Max) Null">
		<Description>Дополнительные параметры, которые будут вставляться в запрос на определение инструмента.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="99d5c8aa-8cf6-46b3-aa37-0d49664c9814" Name="Description" Type="String(Max) Null">
		<Description>Описание действия.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ffe5de34-069d-4252-92df-383e19ad83cb" Name="Prompt" Type="String(Max) Null">
		<Description>Промт.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="b3012467-ec58-49bc-9958-051987b8b563" Name="ResponseIfDataMissing" Type="String(Max) Null">
		<Description>Ответ, если данных не хватает.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="38c1cb97-00fa-4d1c-a03d-dc6ede1eb121" Name="Mode" Type="Reference(Typified) Not Null" ReferencedTable="e2b5dc2d-707d-452e-940c-29ddaabefc49">
		<Description>Режим работы инструмента.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="38c1cb97-00fa-001c-4000-0c6ede1eb121" Name="ModeID" Type="Guid Not Null" ReferencedColumn="53d6d13f-c1df-4365-822d-7c8a96d8a041" />
		<SchemeReferencingColumn ID="b569dac2-ba12-44aa-b5f9-1e2b77105a88" Name="ModeName" Type="String(128) Not Null" ReferencedColumn="5e68f51b-bfc2-49ed-aa22-c60a240fe1f8" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="c79abc42-54f2-4fea-9904-f74f33afc7ac" Name="Action" Type="Reference(Typified) Null" ReferencedTable="78a63577-af61-494e-984a-b5c6ae2c3f71">
		<Description>Выполняемое действие.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c79abc42-54f2-00ea-4000-074f33afc7ac" Name="ActionID" Type="Guid Null" ReferencedColumn="520b6ee2-07c2-42b7-ad8b-49f4d72693ee" />
		<SchemeReferencingColumn ID="f9ca1a21-acb0-4790-985c-58119a772358" Name="ActionName" Type="String(64) Null" ReferencedColumn="7d879c5a-2e3e-4eb1-afa6-eb2d4f3a1e1e" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="c6aa5160-4b78-461c-a3ce-ce797ee25250" Name="Type" Type="Reference(Typified) Null" ReferencedTable="a90baecf-c9ce-4cba-8bb0-150a13666266" WithForeignKey="false">
		<Description>Тип документа.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c6aa5160-4b78-001c-4000-0e797ee25250" Name="TypeID" Type="Guid Null" ReferencedColumn="a90baecf-c9ce-01ba-4000-050a13666266" />
		<SchemeReferencingColumn ID="c69e89d8-1dd1-4f15-8108-65a730723a43" Name="TypeCaption" Type="String(128) Null" ReferencedColumn="447f7cb1-76ae-4703-b3bb-16a57d4e7ab1" />
		<SchemePhysicalColumn ID="00a96a46-0097-4f68-964d-426145093770" Name="TypeIsDocType" Type="Boolean Null">
			<Description>Признак того, что указанный тип - это тип документа (а не карточки).</Description>
		</SchemePhysicalColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="8d603439-2c73-4a04-afbd-4db05db56cdc" Name="ProcessID" Type="String(128) Null">
		<Description>Идентификатор процесса (доступно только для "внести данные в TESSA" и действия "создать карточку и запустить процесс").</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="2afb766c-d905-4961-b124-8d56ec7b24e1" Name="ActionConfirmation" Type="String(Max) Null">
		<Description>Подтверждение выполнения действия.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ec979b1e-f375-494c-9916-9c8e207ec292" Name="SuccessResponse" Type="String(Max) Null">
		<Description>Ответ при успешном выполнении.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1d466cbc-0e82-439a-be0b-669be6c60170" Name="ActionFailureText" Type="String(Max) Null">
		<Description>Текст ошибки, если не удалось выполнить действие.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="569d04a4-e36e-4404-8b86-4610ec1a0038" Name="View" Type="Reference(Typified) Null" ReferencedTable="3519b63c-eea0-48f4-b70a-544e58ece5fc" WithForeignKey="false">
		<Description>Представление.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="569d04a4-e36e-0004-4000-0610ec1a0038" Name="ViewID" Type="Guid Null" ReferencedColumn="8e4c45ad-ca6f-4f0f-be25-9a9e37a4cfd6" />
		<SchemeReferencingColumn ID="107e4d49-4e4c-406e-a7b1-ad05bcb8f749" Name="ViewAlias" Type="String(128) Null" ReferencedColumn="827d19f5-a1aa-4e74-92c0-8bb9dcbceb7d" />
		<SchemeReferencingColumn ID="41e5c8ed-175c-437d-be8b-31d157140420" Name="ViewCaption" Type="String(256) Null" ReferencedColumn="1fa10dd2-e0f5-449a-9a96-6cc4e497ef6e" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="62e60eff-7620-4237-9885-7e500a1b1ff6" Name="MaxRows" Type="Int16 Null">
		<Description>Отправлять AI не более строк.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="e2061e3a-cf22-4b26-a0fb-37488b2649dc" Name="CardIDColumn" Type="Reference(Abstract) Null" WithForeignKey="false">
		<Description>Колонка с идентификатором карточки.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e2061e3a-cf22-0026-4000-07488b2649dc" Name="CardIDColumnID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="377d90ae-0b2f-46f4-9bcb-3cfc8357fa92" Name="CardIDColumnAlias" Type="String(128) Null" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="7afdffbc-ebb7-4214-b958-b71a6e0291a6" Name="AutoPrompt" Type="Boolean Not Null">
		<Description>Автопромт.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="ba805731-89b9-4b4b-a13b-368014b4d069" Name="df_AiTool_AutoPrompt" Value="true" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="0b25fd22-9f68-4371-bd03-a36f3f436b0b" Name="ProcessType" Type="Reference(Typified) Null" ReferencedTable="0b8beefb-4da7-492c-b5d5-2864e2797f27">
		<Description>Тип запускаемого процесса.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="0b25fd22-9f68-0071-4000-036f3f436b0b" Name="ProcessTypeID" Type="Guid Null" ReferencedColumn="e5689715-659a-4f00-80ef-5ffcb80e469e" />
		<SchemeReferencingColumn ID="321e3ecc-e67c-42e5-b087-71c38f841e72" Name="ProcessTypeName" Type="String(64) Null" ReferencedColumn="d0da662b-f05d-4de6-a14f-7da77ecbc645" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="4fda8c4e-1b71-00f0-5000-0c785cea7509" Name="pk_AiTool" IsClustered="true">
		<SchemeIndexedColumn Column="4fda8c4e-1b71-01f0-4000-0c785cea7509" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="bac80e5d-7094-45fe-939c-1ce289c91b7f" Name="ndx_AiTool_Code">
		<SchemeIndexedColumn Column="8318375f-83ad-45bb-9cbe-32da7b407c9b" />
	</SchemeUniqueKey>
</SchemeTable>