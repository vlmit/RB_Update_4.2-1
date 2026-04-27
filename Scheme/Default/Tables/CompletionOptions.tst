<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="08cf782d-4130-4377-8a49-3e201a05d496" Name="CompletionOptions" Group="System">
	<Description>Список возможных варианты завершения.</Description>
	<SchemePhysicalColumn ID="132dc5f5-ce87-4dd0-acce-b4a02acf7715" Name="ID" Type="Guid Not Null" IsRowGuidColumn="true">
		<Description>Идентификатор варианта завершения.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="aa6a7122-8384-4c81-9553-386f2c05e96c" Name="Name" Type="String(128) Not Null">
		<Description>Имя варианта завершения.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="6762309a-b0ff-4b2f-9cce-dd111116e554" Name="Caption" Type="String(128) Not Null">
		<Description>Отображаемое пользователю имя варианта завершения.</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="1f42080e-2563-4a39-a5e2-c775ca6ff286" Name="pk_CompletionOptions" IsClustered="true">
		<SchemeIndexedColumn Column="132dc5f5-ce87-4dd0-acce-b4a02acf7715" />
	</SchemePrimaryKey>
	<SchemeIndex ID="5a5e4beb-cf6a-4770-af07-2c41ea7688e8" Name="ndx_CompletionOptions_Name" IsUnique="true">
		<SchemeIndexedColumn Column="aa6a7122-8384-4c81-9553-386f2c05e96c">
			<Expression Dbms="PostgreSql">lower("Name")</Expression>
		</SchemeIndexedColumn>
	</SchemeIndex>
</SchemeTable>