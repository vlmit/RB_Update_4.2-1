<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="80c75718-d41b-498a-bd68-50794ae02a83" Name="FileVaultFolderAccess" Group="FileVault">
	<SchemePhysicalColumn ID="3b406faa-89c6-485b-be19-082acf5c8eb6" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="55fb0765-952d-43ab-9584-2f11d7892005" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="bda9569f-1c20-4c7a-8afd-b9444b220edc" Name="Role" Type="Reference(Typified) Not Null" ReferencedTable="81f6010b-9641-4aa5-8897-b8e8603fbf4b">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="bda9569f-1c20-007a-4000-09444b220edc" Name="RoleID" Type="Guid Not Null" ReferencedColumn="81f6010b-9641-01a5-4000-08e8603fbf4b" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="fd52b10c-a7a6-4725-89ea-f2608cebefe8" Name="CanEdit" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="2a4d4e2e-9fe8-4deb-9e92-4a456477470b" Name="df_FileVaultFolderAccess_CanEdit" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ac8139f1-32dd-451a-986f-9f7ddade5b0e" Name="CanDelete" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="9b0017fe-de46-4ae2-a176-d1721d7bb536" Name="df_FileVaultFolderAccess_CanDelete" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="2fa43842-4f5c-45ff-8e49-1fa02fe69ed6" Name="Inherited" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="c40335e3-3c09-4d16-b0f2-230690f95ecf" Name="df_FileVaultFolderAccess_Inherited" Value="false" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="89282562-51ac-4e4e-8d3b-213cfee00da3" Name="pk_FileVaultFolderAccess">
		<SchemeIndexedColumn Column="55fb0765-952d-43ab-9584-2f11d7892005" />
	</SchemePrimaryKey>
	<SchemeIndex ID="674b37b0-c4d9-4f99-ad9f-a565bcc7d58b" Name="ndx_FileVaultFolderAccess_ID">
		<SchemeIndexedColumn Column="3b406faa-89c6-485b-be19-082acf5c8eb6" />
	</SchemeIndex>
	<SchemeIndex ID="05698615-87e1-4ce0-a9ba-88509f42fd42" Name="ndx_FileVaultFolderAccess_RoleID">
		<SchemeIndexedColumn Column="bda9569f-1c20-007a-4000-09444b220edc" />
	</SchemeIndex>
</SchemeTable>