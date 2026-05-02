<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="9c6ca046-eec4-4fb8-be98-652788bbc64d" Name="AiToolAdditionalInstructions" Group="AI" InstanceType="Cards" ContentType="Collections">
	<Description>Дополнительные данные,  добавляемые в инструкцию (промт) ИИ.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9c6ca046-eec4-00b8-2000-052788bbc64d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9c6ca046-eec4-01b8-4000-052788bbc64d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9c6ca046-eec4-00b8-3100-052788bbc64d" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="dc2656d0-6dee-4242-8283-dcfb5459cd86" Name="Kind" Type="Reference(Typified) Null" ReferencedTable="1be04a56-b91c-406f-b96c-fa13fda9ead8">
		<Description>Контекст (дополнительные данные для промта).</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="dc2656d0-6dee-0042-4000-0cfb5459cd86" Name="KindID" Type="Guid Null" ReferencedColumn="5e208774-faf3-46db-9762-555c428d523e" />
		<SchemeReferencingColumn ID="94a1b56d-ba0e-498a-a6b2-459a26e3263f" Name="KindName" Type="String(64) Null" ReferencedColumn="bcac2631-5d8c-424a-9ccf-ab298a0661d5" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="9c6ca046-eec4-00b8-5000-052788bbc64d" Name="pk_AiToolAdditionalInstructions">
		<SchemeIndexedColumn Column="9c6ca046-eec4-00b8-3100-052788bbc64d" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="9c6ca046-eec4-00b8-7000-052788bbc64d" Name="idx_AiToolAdditionalInstructions_ID" IsClustered="true">
		<SchemeIndexedColumn Column="9c6ca046-eec4-01b8-4000-052788bbc64d" />
	</SchemeIndex>
</SchemeTable>