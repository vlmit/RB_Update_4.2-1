<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="91e83dd3-0932-404d-b744-fab67214777e" Name="AiPromptTests" Group="AI">
	<Description>Информация о тестах механизма тестирования промптов.</Description>
	<SchemePhysicalColumn ID="15abc721-1bcd-488e-9c15-d0cd43a79c74" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="441f2f76-4ed3-4a46-8d0a-2ad2b3a08709" Name="RequestID" Type="Guid Not Null">
		<Description>Идентификатор запроса.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="6e0a1c1f-d10b-47bd-8d0f-d5143f46a99c" Name="User" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<Description>Пользователь, который отправил запрос.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="6e0a1c1f-d10b-00bd-4000-05143f46a99c" Name="UserID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="fc3b0e01-3325-4284-b2c1-ec937dcbb8ef" Name="Created" Type="DateTime Not Null">
		<Description>Дата и время создания запроса.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f949dc39-a3c2-40b8-9995-c8096d39bb17" Name="Finished" Type="DateTime Null">
		<Description>Дата и время окончания тестирования.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="34df2939-0d91-4229-8e36-d01bf74d28d7" Name="Error" Type="String(512) Null">
		<Description>Ошибка выполнения (если есть).</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="ae4ba334-4880-42c5-9623-d093e6605aea" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="c6c447cc-adc6-4ee5-bf1e-2c65537162e6" WithForeignKey="false">
		<Description>Состояние запроса на тестирование.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ae4ba334-4880-00c5-4000-0093e6605aea" Name="StateID" Type="Int16 Not Null" ReferencedColumn="5e2cd114-db8f-4bb9-9776-09cc9d8e1e45" />
	</SchemeComplexColumn>
	<SchemePrimaryKey ID="26295b66-f89b-4248-b921-a396242cde27" Name="pk_AiPromptTests">
		<SchemeIndexedColumn Column="15abc721-1bcd-488e-9c15-d0cd43a79c74" />
	</SchemePrimaryKey>
	<SchemeIndex ID="4bf0329f-f4ef-43ac-98f4-6fc46e6f7d61" Name="ndx_AiPromptTests_RequestIDUserID">
		<SchemeIndexedColumn Column="441f2f76-4ed3-4a46-8d0a-2ad2b3a08709" />
		<SchemeIndexedColumn Column="6e0a1c1f-d10b-00bd-4000-05143f46a99c" />
	</SchemeIndex>
</SchemeTable>