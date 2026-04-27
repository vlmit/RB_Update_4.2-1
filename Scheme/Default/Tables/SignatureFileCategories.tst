<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="ea2f1419-4843-46a7-9886-1436dece7b8b" Name="SignatureFileCategories" Group="System">
	<Description>Signature file categories</Description>
	<SchemePhysicalColumn ID="6f95178d-9a42-4045-8502-262fa87ee36c" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="0930b1ad-8bcc-423f-a07b-391040d7b042" Name="Name" Type="String(128) Not Null" />
	<SchemePrimaryKey ID="cd8edcba-bca6-449a-9412-34254f605db0" Name="pk_SignatureFileCategories">
		<SchemeIndexedColumn Column="6f95178d-9a42-4045-8502-262fa87ee36c" />
	</SchemePrimaryKey>
	<SchemeRecord>
		<ID ID="6f95178d-9a42-4045-8502-262fa87ee36c">6e61f5c3-f67a-4e1a-877a-3e23fe06d12c</ID>
		<Name ID="0930b1ad-8bcc-423f-a07b-391040d7b042">$Enum_SignatureFileCategory_CRL</Name>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="6f95178d-9a42-4045-8502-262fa87ee36c">82aa2204-d92e-4780-bdb3-43ecf8bb2607</ID>
		<Name ID="0930b1ad-8bcc-423f-a07b-391040d7b042">$Enum_SignatureFileCategory_Certificate</Name>
	</SchemeRecord>
</SchemeTable>