<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="741301fd-f38a-4cca-bab9-df1328d53b53" Name="HelpSections" Group="System" InstanceType="Cards" ContentType="Entries">
	<Description>Разделы справки.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="741301fd-f38a-00ca-2000-0f1328d53b53" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="741301fd-f38a-01ca-4000-0f1328d53b53" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="a10e8dcf-87c7-46c2-ab7f-8efe74cd4016" Name="Code" Type="String(128) Not Null">
		<Description>Уникальная строка для адресации из настроек. Например, CONTRACTS_DUE_DATE.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="0864dbe9-b490-4a87-b352-f0878ea82668" Name="Name" Type="String(Max) Not Null">
		<Description>Строковое описание этого раздела для заголовка окна.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1f115761-97a5-448a-a3cd-0430290b7d24" Name="RichText" Type="BinaryJson Not Null">
		<Description>Форматированный текст с собственно данными раздела справки.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="6cfe8eb4-82de-4094-8864-58b00edc8bbc" Name="PlainText" Type="String(Max) Null">
		<Description>Для полнотекстового поиска. Для этой колонки на сервере извлекается текст из RichText-а при каждом его изменении.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="832d7ae8-13d6-4146-bfeb-e358ad0e81b0" Name="Language" Type="Reference(Typified) Null" ReferencedTable="1ed36bf1-2ebf-43da-acb2-1ddb3298dbd8">
		<Description>Card language.
Leave empty if help is default and applicable to all languages.
Set to specific language if help appliable or contains textual materials on given language.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="832d7ae8-13d6-0046-4000-0358ad0e81b0" Name="LanguageID" Type="Int16 Null" ReferencedColumn="f13de4a3-34d7-4e7b-95b6-f34372ed724c" />
		<SchemeReferencingColumn ID="c3844d97-b685-400e-bc8e-a78e110d655c" Name="LanguageCaption" Type="String(256) Null" ReferencedColumn="40a3d47c-40f7-48bd-ab8e-edef2f84094d" />
		<SchemeReferencingColumn ID="d4f38d19-08ea-4660-8835-31f5a164b6a6" Name="LanguageCode" Type="AnsiString(3) Null" ReferencedColumn="9e7084bb-c1dc-4ace-90c9-800dbcf7f3c2" />
	</SchemeComplexColumn>
	<SchemeUniqueKey ID="e9e6a90c-73c1-4591-81b5-37322462d510" Name="ndx_HelpSections_CodeLanguageIDLanguageCode">
		<SchemeIndexedColumn Column="a10e8dcf-87c7-46c2-ab7f-8efe74cd4016" />
		<SchemeIndexedColumn Column="832d7ae8-13d6-0046-4000-0358ad0e81b0" />
		<SchemeIndexedColumn Column="d4f38d19-08ea-4660-8835-31f5a164b6a6" />
	</SchemeUniqueKey>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="741301fd-f38a-00ca-5000-0f1328d53b53" Name="pk_HelpSections" IsClustered="true">
		<SchemeIndexedColumn Column="741301fd-f38a-01ca-4000-0f1328d53b53" />
	</SchemePrimaryKey>
</SchemeTable>