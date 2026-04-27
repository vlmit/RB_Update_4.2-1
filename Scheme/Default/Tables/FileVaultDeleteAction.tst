<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="b894fba8-c6c4-4083-a838-686af6e02057" Name="FileVaultDeleteAction" Group="FileVault">
	<SchemePhysicalColumn ID="c00f6c69-d0ae-4955-ab15-07edbb2e268a" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="d0cc2449-56fd-4c1e-8d11-d588240b8b61" Name="DeletedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d0cc2449-56fd-001e-4000-0588240b8b61" Name="DeletedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="532c9773-cf66-4395-b4b2-cf7c822224ae" Name="DeletedAt" Type="DateTime Not Null" />
	<SchemeComplexColumn ID="0827a29a-e844-4cb2-a99c-fefa7397eadf" Name="Folder" Type="Reference(Typified) Null" ReferencedTable="3b62928a-f3b8-4637-845d-1578b0bea42d" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="0827a29a-e844-00b2-4000-0efa7397eadf" Name="FolderID" Type="Guid Null" ReferencedColumn="ce1ab9f4-e7a4-4101-92d5-b71a5b4fc087" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="40965e9c-c367-48ed-920b-2f7596b37ad2" Name="File" Type="Reference(Typified) Null" ReferencedTable="c9011e78-9e42-4445-a2f1-5558e935407c" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="2ca5e0f9-039f-472d-ac81-b103d49fcbd8" Name="FileID" Type="Guid Null" ReferencedColumn="563bc428-6262-4313-abde-e04b0972138d" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="28ff7d8e-0b99-4520-b129-68bbdf4c4385" Name="FullPath" Type="String(Max) Not Null" />
	<SchemePrimaryKey ID="cf35ff1c-6e3c-44c0-9759-3e7ae5ab3160" Name="pk_FileVaultDeleteAction">
		<SchemeIndexedColumn Column="c00f6c69-d0ae-4955-ab15-07edbb2e268a" />
	</SchemePrimaryKey>
	<SchemeIndex ID="89d50cab-bc51-4304-b2a0-ce8a2f1e07f7" Name="ndx_FileVaultDeleteAction_FolderID">
		<Predicate Dbms="SqlServer">[FolderID] IS NOT NULL</Predicate>
		<Predicate Dbms="PostgreSql">"FolderID" IS NOT NULL</Predicate>
		<SchemeIndexedColumn Column="0827a29a-e844-00b2-4000-0efa7397eadf" />
	</SchemeIndex>
	<SchemeIndex ID="0313519d-027f-4ae9-9772-49bf614d00ea" Name="ndx_FileVaultDeleteAction_FileID">
		<Predicate Dbms="SqlServer">[FolderID] IS NOT NULL</Predicate>
		<Predicate Dbms="PostgreSql">"FileID" IS NOT NULL</Predicate>
		<SchemeIndexedColumn Column="2ca5e0f9-039f-472d-ac81-b103d49fcbd8" />
	</SchemeIndex>
</SchemeTable>