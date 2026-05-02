<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="9ba19a3a-ddfd-4d68-9af0-fe7f87345962" Name="RefGroups" Group="RefGroups" InstanceType="Cards" ContentType="Entries">
	<Description>Для механизма "Групп ссылок" позволяющего в системе выбирать в качестве некого значения списка предопределенную группу значений.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9ba19a3a-ddfd-0068-2000-0e7f87345962" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9ba19a3a-ddfd-0168-4000-0e7f87345962" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="df8c6029-ee14-488b-b935-2a3b8e4a067c" Name="Name" Type="String(128) Not Null">
		<Description>"Название" – значение, которое должно отображаться при выборе данной группы.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="46e6cd0c-28a3-4547-87e7-867d0014acc4" Name="RefGroupType" Type="Reference(Typified) Not Null" ReferencedTable="7f8d57be-1ea4-4884-84d1-d7d9bfe1fa11">
		<Description>"Тип значений" – значение, определяющее тип значений текущей группы. Например "Состояние", "Тип документа" и т.д.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="46e6cd0c-28a3-0047-4000-067d0014acc4" Name="RefGroupTypeID" Type="Guid Not Null" ReferencedColumn="7f8d57be-1ea4-0184-4000-07d9bfe1fa11" />
		<SchemeReferencingColumn ID="ec928828-1c04-4317-b3d5-30b979dee674" Name="RefGroupTypeName" Type="String(128) Not Null" ReferencedColumn="949b46b6-fb6f-4e95-8624-908efdaf1db9" />
		<SchemePhysicalColumn ID="a07ef504-caf1-43f0-ac2e-ca4e7d2943cb" Name="RefGroupTypeKeyTypeID" Type="Int32 Not Null" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="7f356985-e711-4940-883b-db0fe25b6c09" Name="IntID" Type="Int32 Null">
		<Description>"Числовой идентификатор" – число, определяющее числовой идентификатор данной группы. Используется для типов групп, которые имеют в качестве идентификатор – число (например состояния). Данный идентификатор следует генерировать при первом сохранении и дать возможность ручного ввода.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d46f806d-87c5-417c-bdff-2a8aa9cdd2f3" Name="Description" Type="String(Max) Null">
		<Description>"Описание" – текстовое описание группы.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="55cd3077-95a2-4961-9403-917af83ad0a2" Name="SQL" Type="String(Max) Null">
		<Description>"SQL-запрос" – запрос для получения значений для данной группы из базы данных. Допускает пустое значение, если предполагается полный список. Показываем, если используется режим заполнения "SQL-запрос".</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e2334111-eaa8-43f2-a4dc-3e9edad0c723" Name="Calculated" Type="DateTime Null">
		<Description>"Дата последнего перерасчёта" – дата, когда перерасчёт группы был выполнен успешно последний раз.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e44ab37a-7cf3-47b8-a65a-6301d0b00ce4" Name="LastError" Type="DateTime Null">
		<Description>Дата и время последней ошибки перерасчёта.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="4337deaa-f854-4edb-953e-9d1887b92db3" Name="Settings" Type="Json Null">
		<Description>Поле с JSON настройками карточки.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e2c2d367-75f4-4e9e-9ab0-f81021265e7a" Name="Script" Type="String(Max) Null">
		<Description>"Скрипт заполнения" - скрипт на языке C#, возвращающий список значений. Показываем, если используется режим заполнения "Скрипт".</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="c6fc4f8d-553e-44e9-ac05-0464e7de8fb0" Name="Mode" Type="Reference(Typified) Not Null" ReferencedTable="b0e55702-728d-42ec-a9cc-015dad02ab53">
		<Description>"Режим заполнения" – список режимов заполнение группы.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c6fc4f8d-553e-00e9-4000-0464e7de8fb0" Name="ModeID" Type="Int32 Not Null" ReferencedColumn="1d584515-e168-46df-b0f0-2f9d80963c70" />
		<SchemeReferencingColumn ID="9bb8b307-3cc3-4f62-adc4-33afa978ed1f" Name="ModeName" Type="String(128) Not Null" ReferencedColumn="4e1eeb14-dece-41a2-9aa7-eaa98ea93f79" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="2f33fca1-c3b1-42c5-bc22-26c3344a2588" Name="MessageText" Type="String(Max) Null">
		<Description>Текст ошибки или предупреждения, возникших в ходе последнего перерасчёта группы.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="9ba19a3a-ddfd-0068-5000-0e7f87345962" Name="pk_RefGroups" IsClustered="true">
		<SchemeIndexedColumn Column="9ba19a3a-ddfd-0168-4000-0e7f87345962" />
	</SchemePrimaryKey>
</SchemeTable>