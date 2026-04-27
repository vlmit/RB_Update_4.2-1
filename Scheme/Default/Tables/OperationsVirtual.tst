<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="a87294b1-0c66-41bc-98e8-765dfb8dcf56" Name="OperationsVirtual" Group="System" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<Description>Virtual card "Operation". Filled using tables Operations and OperationUpdates, and using HSET from Redis</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="a87294b1-0c66-00bc-2000-065dfb8dcf56" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a87294b1-0c66-01bc-4000-065dfb8dcf56" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="6b19d85c-1449-4b01-b159-1f8d11c367f0" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="b23fccd5-5ba1-45b6-a0ad-e9d0cf730da0">
		<Description>Operation's type</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="6b19d85c-1449-0001-4000-0f8d11c367f0" Name="TypeID" Type="Guid Not Null" ReferencedColumn="6096f85d-06b5-433a-9219-d0ec5f045561" />
		<SchemeReferencingColumn ID="01187895-b277-4854-80c2-e8ac238d0205" Name="TypeName" Type="String(128) Not Null" ReferencedColumn="35985e42-276a-4ed3-84f4-aa154ac3a4df" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="625ae9e3-dc50-4726-904b-e717fb3f6dfd" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="e726339c-e2fc-4d7c-a9b4-011577ff2106">
		<Description>Operation's state</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="625ae9e3-dc50-0026-4000-0717fb3f6dfd" Name="StateID" Type="Int16 Not Null" ReferencedColumn="3f1b4bb9-16db-4c3a-b735-b41c3fd51bdf" />
		<SchemeReferencingColumn ID="366e3438-e0ea-49ec-ab49-26c6280578bb" Name="StateName" Type="String(128) Not Null" ReferencedColumn="3c2e9608-7121-4117-b448-b6a3ada082b7" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="28376bfc-db5c-4c98-92f1-9b6f8c444cde" Name="CreatedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<Description>User who created an operation</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="28376bfc-db5c-0098-4000-0b6f8c444cde" Name="CreatedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
		<SchemeReferencingColumn ID="a3f27ae5-ec48-42ae-9bbe-3cb1c73df106" Name="CreatedByName" Type="String(128) Not Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="1b5857aa-c656-42d9-ae07-36c427ecef7e" Name="Created" Type="DateTime Not Null">
		<Description>Date and time (UTC) when an operation was created</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3af45a19-13c0-4f40-a25e-0829009fc495" Name="InProgress" Type="DateTime Null">
		<Description>Date and time (UTC) when an operation's processing was started, or Null if it hasn't been started yet</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="86d02075-eb67-4e64-a863-67b9b59990ac" Name="Completed" Type="DateTime Null">
		<Description>Date and time (UTC) when an operation's processing was completed, or Null if it hasn't been completed yet</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ceaa6337-08db-403e-9a77-1d853e3d4311" Name="Progress" Type="Double Null">
		<Description>Progress percentage (number from 0 to 100) of an operation's processing, or Null if operation doesn't support reporting progress</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="65cfca14-0e68-4835-9f13-08227fe4c9e4" Name="Digest" Type="String(128) Null">
		<Description>Short description of an operation</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f9398bdf-d134-4ac0-be7b-cd12dec1ee86" Name="Request" Type="String(Max) Null">
		<Description>Serialized request for an operation in text format for output in controls, or Null if an operation doesn't require a request</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="dff5b666-8025-46ff-b125-1d32163b65a6" Name="RequestJson" Type="BinaryJson Null">
		<Description>Serialized request for an operation, or Null if an operation doesn't require a request</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="4f14d4df-6fc0-4c78-aa3a-539f49a3e14d" Name="Response" Type="String(Max) Null">
		<Description>Serialized response for an operation in text format for output in controls, or Null if an operation hasn't been completed yet, or if it doesn't provide a response</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="37728d3b-0e3b-4ba3-a232-498eb8031060" Name="ResponseJson" Type="BinaryJson Null">
		<Description>Serialized response for an operation, or Null if an operation hasn't been completed yet, or if it doesn't provide a response</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="2ce35a67-5f4b-41eb-a8a9-f55ff4360ad3" Name="OperationID" Type="Guid Not Null">
		<Description>Identifier of an operation. It's equal to identifier of a virtual card. Used for output in controls</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="f0647358-0843-4f0f-bbf2-e711bead26ac" Name="SessionID" Type="Guid Null">
		<Description>Identifier of a session used to create an operation, or Null if it's a legacy operation, or if it was created outside of a session</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="cb5721a5-3dc9-472b-abff-e6e63d0d7f03" Name="ObjectID" Type="Guid Null">
		<Description>Identifier of an object associated with an operation (usually card id or file id). Can be absent. Affects Hash value calculation</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="a87294b1-0c66-00bc-5000-065dfb8dcf56" Name="pk_OperationsVirtual" IsClustered="true">
		<SchemeIndexedColumn Column="a87294b1-0c66-01bc-4000-065dfb8dcf56" />
	</SchemePrimaryKey>
</SchemeTable>