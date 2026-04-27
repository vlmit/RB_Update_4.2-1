<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="3b62928a-f3b8-4637-845d-1578b0bea42d" Name="FileVaultFolder" Group="FileVault">
	<SchemePhysicalColumn ID="ce1ab9f4-e7a4-4101-92d5-b71a5b4fc087" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="0a25b7a2-0d27-41f9-a21c-0b5cb98aca4d" Name="Owner" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="0a25b7a2-0d27-00f9-4000-0b5cb98aca4d" Name="OwnerID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="3cfde9bf-2e70-472d-8b4f-4419305ea0b0" Name="Name" Type="String(128) Not Null" />
	<SchemeComplexColumn ID="da16e9e2-5473-4712-b0b8-205be793a2a5" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="2e2c35e9-9be1-418d-8aec-612a7340e00c">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="da16e9e2-5473-0012-4000-005be793a2a5" Name="TypeID" Type="Int32 Not Null" ReferencedColumn="4839ea7b-bc41-41bd-a896-46fc778d3402" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="80d88145-4991-47d1-9d1b-51bfec65d306" Name="Parent" Type="Reference(Typified) Null" ReferencedTable="3b62928a-f3b8-4637-845d-1578b0bea42d">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="80d88145-4991-00d1-4000-01bfec65d306" Name="ParentID" Type="Guid Null" ReferencedColumn="ce1ab9f4-e7a4-4101-92d5-b71a5b4fc087" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="cc33b71d-b082-4d5a-a036-3e5a8d747f43" Name="Delete" Type="Reference(Typified) Null" ReferencedTable="b894fba8-c6c4-4083-a838-686af6e02057">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9f5efb09-bc0d-48f7-ac30-295f26662a1f" Name="DeleteID" Type="Guid Null" ReferencedColumn="c00f6c69-d0ae-4955-ab15-07edbb2e268a" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="a4608c20-d47b-4e27-9faf-51e15aefb952" Name="InheritPermissions" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="baa53923-472e-461e-a252-c2748d452cf3" Name="df_FileVaultFolder_InheritPermissions" Value="true" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="45d360ca-a0ee-4dbe-b280-54b626c44611" Name="pk_FileVaultFolder">
		<SchemeIndexedColumn Column="ce1ab9f4-e7a4-4101-92d5-b71a5b4fc087" />
	</SchemePrimaryKey>
	<SchemeIndex ID="dcb3639d-1bbd-4621-85a3-d5a583ae7545" Name="ndx_FileVaultFolder_ParentID">
		<SchemeIndexedColumn Column="80d88145-4991-00d1-4000-01bfec65d306" />
	</SchemeIndex>
	<SchemeIndex ID="4043cb71-ec97-45a3-a185-6ed51ffd399e" Name="ndx_FileVaultFolder_DeleteID">
		<Predicate Dbms="SqlServer">[DeleteID] IS NOT NULL</Predicate>
		<Predicate Dbms="PostgreSql">"DeleteID" IS NOT NULL</Predicate>
		<SchemeIndexedColumn Column="9f5efb09-bc0d-48f7-ac30-295f26662a1f" />
	</SchemeIndex>
</SchemeTable>