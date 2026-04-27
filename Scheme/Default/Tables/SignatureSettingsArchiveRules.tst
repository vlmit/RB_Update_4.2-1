<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="9cb70cb0-ef71-4546-9308-b7043af02e5d" Name="SignatureSettingsArchiveRules" Group="System" InstanceType="Cards" ContentType="Collections">
	<Description>Правила создания архивных подписей.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9cb70cb0-ef71-0046-2000-07043af02e5d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9cb70cb0-ef71-0146-4000-07043af02e5d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9cb70cb0-ef71-0046-3100-07043af02e5d" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="c9dc8743-2ab7-4e7b-a0f2-761c1ea14c89" Name="Conditions" Type="BinaryJson Null">
		<Description>Сериализованные данные с условиями к правилу.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e9aa7413-cb79-45dd-9672-8e30138e7534" Name="DaysBeforeCertExpiry" Type="Int32 Not Null">
		<Description>Дней до истечения срока сертификата.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="b40655b1-7248-4100-bd24-ecb0b5ca660f" Name="Order" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="ca5b78d9-3cb0-4a17-8859-ea6566e79fbd" Name="Disabled" Type="Boolean Not Null">
		<Description>Признак того, что правило отключено.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="87a938bf-d804-41ef-8f24-572bf6ceb412" Name="df_SignatureSettingsArchiveRules_Disabled" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3aec6484-8d12-4739-9768-a37c32f09fa1" Name="SqlCondition" Type="String(Max) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="9cb70cb0-ef71-0046-5000-07043af02e5d" Name="pk_SignatureSettingsArchiveRules">
		<SchemeIndexedColumn Column="9cb70cb0-ef71-0046-3100-07043af02e5d" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="9cb70cb0-ef71-0046-7000-07043af02e5d" Name="idx_SignatureSettingsArchiveRules_ID" IsClustered="true">
		<SchemeIndexedColumn Column="9cb70cb0-ef71-0146-4000-07043af02e5d" />
	</SchemeIndex>
</SchemeTable>