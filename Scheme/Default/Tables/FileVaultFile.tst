<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c9011e78-9e42-4445-a2f1-5558e935407c" Name="FileVaultFile" Group="FileVault">
	<SchemeComplexColumn ID="4240d735-0243-40ef-bc78-d340cafbd143" Name="Delete" Type="Reference(Typified) Null" ReferencedTable="b894fba8-c6c4-4083-a838-686af6e02057">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="fa8b966b-cc7b-417f-97ed-65008345637a" Name="DeleteID" Type="Guid Null" ReferencedColumn="c00f6c69-d0ae-4955-ab15-07edbb2e268a" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="588356eb-396a-449a-b9f9-abe1f9bc25bd" Name="Folder" Type="Reference(Typified) Not Null" ReferencedTable="3b62928a-f3b8-4637-845d-1578b0bea42d">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="588356eb-396a-009a-4000-0be1f9bc25bd" Name="FolderID" Type="Guid Not Null" ReferencedColumn="ce1ab9f4-e7a4-4101-92d5-b71a5b4fc087" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="563bc428-6262-4313-abde-e04b0972138d" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="a8996ecb-50cf-4386-9109-cd286e2eace1" Name="CreatedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a8996ecb-50cf-0086-4000-0d286e2eace1" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePrimaryKey ID="b942aa61-2cba-4f79-8044-8638bf040d1f" Name="pk_FileVaultFile">
		<SchemeIndexedColumn Column="563bc428-6262-4313-abde-e04b0972138d" />
	</SchemePrimaryKey>
	<SchemeIndex ID="8f495e9b-ed18-4f7b-9c06-ef915646cebc" Name="ndx_FileVaultFile_FolderID">
		<SchemeIndexedColumn Column="588356eb-396a-009a-4000-0be1f9bc25bd" />
	</SchemeIndex>
	<SchemeIndex ID="b8979f4a-1c61-4489-885b-e214e0c4ff39" Name="ndx_FileVaultFile">
		<Predicate Dbms="SqlServer">[DeleteID] IS NOT NULL</Predicate>
		<Predicate Dbms="PostgreSql">"DeleteID" IS NOT NULL</Predicate>
	</SchemeIndex>
</SchemeTable>