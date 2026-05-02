<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="4ae0856c-dd1d-4da8-80b4-e6d232be8d94" Name="Operations" Group="System">
	<Description>Active operations: immutable fields</Description>
	<SchemePhysicalColumn ID="f9c22998-e7f0-4366-b92e-740c643e48b7" Name="ID" Type="Guid Not Null">
		<Description>Идентификатор операции.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="05308900-cee4-47bf-b23c-c21e85c79a22" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="b23fccd5-5ba1-45b6-a0ad-e9d0cf730da0">
		<Description>Operation's type</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="05308900-cee4-00bf-4000-021e85c79a22" Name="TypeID" Type="Guid Not Null" ReferencedColumn="6096f85d-06b5-433a-9219-d0ec5f045561">
			<Description>Идентификатор типа операции.</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="25996422-d677-4fbb-9a2b-a0584dbcdec7" Name="CreatedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<Description>User who created an operation</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="25996422-d677-00bb-4000-00584dbcdec7" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="85b5ccfb-1be8-4d93-970e-3198450770dc" Name="Created" Type="DateTime Not Null">
		<Description>Date and time (UTC) when an operation was created</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="487d31be-d41a-48f6-ac0f-b3038477092e" Name="Digest" Type="String(128) Null">
		<Description>Short description of an operation</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e9eae0f1-d0d5-48c5-868c-3501d055e865" Name="Request" Type="BinaryJson Null">
		<Description>Serialized request for an operation, or Null if an operation doesn't require a request</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3517f5e4-3f79-42c1-849a-752f426ba088" Name="Hash" Type="Binary(32) Null">
		<Description>Operation's hash based on its TypeID, ObjectID and Request, or Null for legacy operations. Default hashing algorithm is HMAC-SHA256 with hash size of 256 bits (32 bytes)</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="b8207671-529d-43c5-b352-1520ef0a9ece" Name="Postponed" Type="DateTime Null" IsSparse="true">
		<Description>Date and time (UTC) until when operation's processing is postponed, or Null if an operation can be processed right after it was created</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3bf2fe3e-aabc-4403-968a-9023843fe75b" Name="SessionID" Type="Guid Null" IsSparse="true">
		<Description>Identifier of a session used to create an operation, or Null if it's a legacy operation, or if it was created outside of a session</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d7debd98-0f6b-4d8d-abd2-105432bf87ac" Name="CreationFlags" Type="Int16 Not Null">
		<Description>Flags describing operation's behaviour, determined when an operation is created</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ad765270-26d9-439b-a3f7-273441fa81fb" Name="ObjectID" Type="Guid Null">
		<Description>Identifier of an object associated with an operation (usually card id or file id). Can be absent. Affects Hash value calculation</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="7fa4433a-360b-4581-8730-0f1d5911d00f" Name="pk_Operations" IsClustered="true">
		<SchemeIndexedColumn Column="f9c22998-e7f0-4366-b92e-740c643e48b7" />
	</SchemePrimaryKey>
	<SchemeIndex ID="144c458c-f977-4f48-97e0-779f70453847" Name="ndx_Operations_Created">
		<Description>Used to delete old (stuck) operations. Also used in Operations view</Description>
		<SchemeIndexedColumn Column="85b5ccfb-1be8-4d93-970e-3198450770dc" SortOrder="Descending" />
	</SchemeIndex>
	<SchemeIndex ID="7cb51fd3-eaa5-4da6-a96e-3831c8fb29b4" Name="ndx_Operations_TypeID">
		<Description>Used to get operation's type. Used to determine hash collisions when creating an operation. Used to get operation's hash via IOperationRepository.TryGetOperationIDByHashAsync method (called during file conversion, and in ACL and smart roles API)</Description>
		<SchemeIndexedColumn Column="05308900-cee4-00bf-4000-021e85c79a22" />
		<SchemeIncludedColumn Column="3517f5e4-3f79-42c1-849a-752f426ba088" />
	</SchemeIndex>
</SchemeTable>