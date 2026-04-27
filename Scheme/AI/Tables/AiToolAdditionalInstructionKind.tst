<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="1be04a56-b91c-406f-b96c-fa13fda9ead8" Name="AiToolAdditionalInstructionKind" Group="AI">
	<Description>Дополнительные данные, которые могут быть добавлены в инструкцию (промт) ИИ.</Description>
	<SchemePhysicalColumn ID="5e208774-faf3-46db-9762-555c428d523e" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор записи.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="bcac2631-5d8c-424a-9ccf-ab298a0661d5" Name="Name" Type="String(64) Not Null">
		<Description>Название контекста.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="f222258d-5d50-4ae9-a497-089819260031" Name="pk_AiToolAdditionalInstructionKind">
		<SchemeIndexedColumn Column="5e208774-faf3-46db-9762-555c428d523e" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="5e208774-faf3-46db-9762-555c428d523e">2e3f9d6d-67aa-471e-b37a-0d6fef261b3b</ID>
		<Name ID="bcac2631-5d8c-424a-9ccf-ab298a0661d5">$Ai_AdditionalInstructionKind_Employee</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="5e208774-faf3-46db-9762-555c428d523e">8eb00c3f-b5bf-45f4-9652-3b0e6b4f8d5e</ID>
		<Name ID="bcac2631-5d8c-424a-9ccf-ab298a0661d5">$Ai_AdditionalInstructionKind_Department</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="5e208774-faf3-46db-9762-555c428d523e">fe4f17ac-b34b-4831-b218-66889690f0ee</ID>
		<Name ID="bcac2631-5d8c-424a-9ccf-ab298a0661d5">$Ai_AdditionalInstructionKind_CurrentDatetime</Name>
	</SchemeRecord>
</SchemeTable>