<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="f43cfdae-62af-42fe-9b72-62fdf76a8c8c" Name="Tokens" Group="System">
	<Description>Contains tokens for accessing various system resources</Description>
	<SchemePhysicalColumn ID="6222eb53-4417-4cbb-b62b-46a0a56ad226" Name="ID" Type="String(128) Not Null">
		<Description>The identifier of the token</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c830f703-f660-4ac9-bbac-0c2c53fa46b5" Name="Scope" Type="String(256) Not Null">
		<Description>The scope of the token</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="aabcf160-b783-4a0e-a3d8-f3d9a9443a5d" Name="Created" Type="DateTime Not Null">
		<Description>The creation date and time of the token</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="82bcc4ad-8f27-460b-96ae-85d40dee5ac2" Name="Expires" Type="DateTime Not Null">
		<Description>The expiration date and time of the token</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="38f8e4a7-d5cc-4f78-b632-f8183c5915e1" Name="CreatedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<Description>The employee who created the token</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="38f8e4a7-d5cc-0078-4000-08183c5915e1" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>The identifier of the employee who created the token</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="fe830926-5732-47cc-9666-ef1dd31aec25" Name="RefID" Type="Guid Null">
		<Description>The identifier of the resource for which the token was created</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="61e281e8-f67c-493a-b7ff-19a1b6f1bb5c" Name="User" Type="Reference(Typified) Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<Description>The user for whom the token was created</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="61e281e8-f67c-003a-4000-09a1b6f1bb5c" Name="UserID" Type="Guid Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>The identifier of the user for whom the token was created</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="6759ecd3-7a3b-4f8c-8a2b-90783830873d" Name="TokenHash" Type="Binary(32) Not Null">
		<Description>The hash of the token</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="19b9bd63-d4db-495e-94df-50f52edec2c6" Name="Signature" Type="Binary(32) Null">
		<Description>The signature of the token.
Signature contains: FormatID, TypeID, Scope, Description, Created, Expires, RefID, UserID,  Options.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="92ee4454-3acb-4877-83f2-2ba393f291ee" Name="Description" Type="String(Max) Not Null">
		<Description>A description of the token</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c382b8a8-843c-002d-4000-09b859266039" Name="FormatID" Type="Int32 Not Null">
		<Description>Token storage format identifier</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c08808c1-7a18-0024-4000-0b78152a3e18" Name="TypeID" Type="Int32 Not Null">
		<Description>Token type identifier</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="7e4c2e77-4131-4389-8c2f-21e7d2f555cf" Name="df_Tokens_TypeID" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1b32cc8e-d11d-49b8-8e67-f423dc5607cd" Name="Options" Type="Json Null">
		<Description>Additional token parameters a JSON object</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="e9f3d516-f631-44e8-b8d0-0059f12ea3aa" Name="pk_Tokens">
		<SchemeIndexedColumn Column="6222eb53-4417-4cbb-b62b-46a0a56ad226" />
	</SchemePrimaryKey>
	<SchemeIndex ID="f6425338-e5c1-4fb2-bff3-ea01d133933b" Name="ndx_Tokens_Expires">
		<SchemeIndexedColumn Column="82bcc4ad-8f27-460b-96ae-85d40dee5ac2" />
	</SchemeIndex>
	<SchemeIndex ID="9223c054-ff1c-4e54-b56c-7c5ca2e732f2" Name="ndx_Tokens_TokenHash" IsUnique="true">
		<SchemeIndexedColumn Column="6759ecd3-7a3b-4f8c-8a2b-90783830873d" />
	</SchemeIndex>
	<SchemeIndex ID="2bb859f9-546d-40d0-9801-cc067320813e" Name="ndx_Tokens_CreatedByIDUserID">
		<SchemeIndexedColumn Column="38f8e4a7-d5cc-0078-4000-08183c5915e1" />
		<SchemeIndexedColumn Column="61e281e8-f67c-003a-4000-09a1b6f1bb5c" />
	</SchemeIndex>
	<SchemeIndex ID="997fe94f-78a6-4918-8274-33013b462a32" Name="ndx_Tokens_RefID">
		<Predicate Dbms="SqlServer">[RefID] IS NOT NULL</Predicate>
		<Predicate Dbms="PostgreSql">"RefID" IS NOT NULL</Predicate>
		<SchemeIndexedColumn Column="fe830926-5732-47cc-9666-ef1dd31aec25" />
	</SchemeIndex>
	<SchemeIndex ID="2114f848-f7c0-4046-9caf-d53de4c53e09" Name="ndx_Tokens_Scope" SupportsSqlServer="false" Type="GIN">
		<SchemeIndexedColumn Column="c830f703-f660-4ac9-bbac-0c2c53fa46b5">
			<Expression Dbms="PostgreSql">lower("Scope") gin_trgm_ops</Expression>
		</SchemeIndexedColumn>
	</SchemeIndex>
	<SchemeIndex ID="a3f4fc6f-75d3-4368-af3a-217080ddb3af" Name="ndx_Tokens_Scope_a3f4fc6f" SupportsPostgreSql="false">
		<SchemeIndexedColumn Column="c830f703-f660-4ac9-bbac-0c2c53fa46b5" />
	</SchemeIndex>
	<SchemeIndex ID="d742ffd3-7ad9-4bdf-be80-36e6670a960e" Name="ndx_Tokens_FormatID">
		<SchemeIndexedColumn Column="c382b8a8-843c-002d-4000-09b859266039" />
	</SchemeIndex>
	<SchemeIndex ID="15b70863-c401-421e-9a3f-f5a7460f42e4" Name="ndx_Tokens_TypeID">
		<SchemeIndexedColumn Column="c08808c1-7a18-0024-4000-0b78152a3e18" />
	</SchemeIndex>
</SchemeTable>