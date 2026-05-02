<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="a6435575-79d4-44b6-b755-b9a026431556" Name="OperationUpdates" Group="System">
	<Description>Active operations: mutable fields</Description>
	<SchemePhysicalColumn ID="dce48725-e8f0-4c9e-9efc-b42579f32ba4" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор операции.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="d07a1ff5-834c-413a-962c-4b26c5d7bd63" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="e726339c-e2fc-4d7c-a9b4-011577ff2106">
		<Description>Operation's state</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d07a1ff5-834c-003a-4000-0b26c5d7bd63" Name="StateID" Type="Int16 Not Null" ReferencedColumn="3f1b4bb9-16db-4c3a-b735-b41c3fd51bdf">
			<Description>Идентификатор состояния.</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="386ee7c4-a3c6-4373-925b-a1255ac4bf2b" Name="InProgress" Type="DateTime Null">
		<Description>Date and time (UTC) when an operation's processing was started, or Null if it hasn't been started yet</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="174f529f-3fe2-4ce6-adc7-c183cc18eaf5" Name="Completed" Type="DateTime Null">
		<Description>Date and time (UTC) when an operation's processing was completed, or Null if it hasn't been completed yet</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="39ad8955-7d74-410d-a968-b9d98bb260a8" Name="Response" Type="BinaryJson Null">
		<Description>Serialized response for an operation, or Null if an operation hasn't been completed yet, or if it doesn't provide a response</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ef4e0edf-1f29-4b23-a2c3-3f93ca428407" Name="JsonStatus" Type="BinaryJson Null">
		<Description>Operation status in json format of arbitrary structure.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="b147d3ac-8cba-4e96-bdd7-dba206354929" Name="pk_OperationUpdates" IsClustered="true">
		<SchemeIndexedColumn Column="dce48725-e8f0-4c9e-9efc-b42579f32ba4" />
	</SchemePrimaryKey>
</SchemeTable>