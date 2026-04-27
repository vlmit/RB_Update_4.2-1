<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="9aa9fb54-ed55-4828-81fa-31e4b4e52833" Name="AiToolInfoCache" Group="AI">
	<Description>Кеш информации о настройках инструмента (только в случае их изменения с дефолтных)</Description>
	<SchemePhysicalColumn ID="07b541c2-64d5-4d1c-9d8f-2ff43fd7a484" Name="ID" Type="String(512) Not Null">
		<Description>Уникальный строковый идентификатор инструмента (иногда называют кодом инструмента).
По факту - имя инструмента для LLM.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ac390cd4-330e-470a-afd1-53af8fb09137" Name="Name" Type="String(Max) Null">
		<Description>Имя инструмента (входит в определение инструмента для LLM)</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="27fc5bc9-8bc0-45cf-985d-55ca55c94a4c" Name="Description" Type="String(Max) Null">
		<Description>Описание инструмента (входит в определение инструмента для LLM).
Может отсутствовать даже в настройках по умолчанию.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="5fd54307-0955-4514-b626-7c1cfc169626" Name="Enabled" Type="Boolean Null">
		<Description>Признак, что инструмент включен или выключен.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="5f0337fa-9b65-4927-9a9c-e0bd52b8f915" Name="pk_AiToolInfoCache">
		<SchemeIndexedColumn Column="07b541c2-64d5-4d1c-9d8f-2ff43fd7a484" />
	</SchemePrimaryKey>
</SchemeTable>