<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="2e2c35e9-9be1-418d-8aec-612a7340e00c" Name="FileVaultFolderType" Group="FileVault">
	<SchemePhysicalColumn ID="4839ea7b-bc41-41bd-a896-46fc778d3402" Name="ID" Type="Int32 Not Null" />
	<SchemePhysicalColumn ID="25cf25fc-2e38-4785-af9e-ccded100c7a2" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="93db2c07-a7c3-4d69-bbd7-38b5d2830350" Name="pk_FileVaultFolderType">
		<SchemeIndexedColumn Column="4839ea7b-bc41-41bd-a896-46fc778d3402" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="4839ea7b-bc41-41bd-a896-46fc778d3402">0</ID>
		<Name ID="25cf25fc-2e38-4785-af9e-ccded100c7a2">Personal</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="4839ea7b-bc41-41bd-a896-46fc778d3402">1</ID>
		<Name ID="25cf25fc-2e38-4785-af9e-ccded100c7a2">Shared</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="4839ea7b-bc41-41bd-a896-46fc778d3402">2</ID>
		<Name ID="25cf25fc-2e38-4785-af9e-ccded100c7a2">Home</Name>
	</SchemeRecord>
</SchemeTable>