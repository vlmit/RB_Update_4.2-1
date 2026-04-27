<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="7a791775-ad11-4b9b-a793-ed0cc0aaf29d" Name="ApprovalProcessControlAction" Group="ApprovalProcess" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="7a791775-ad11-009b-2000-0d0cc0aaf29d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7a791775-ad11-019b-4000-0d0cc0aaf29d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="d8adf3e0-ba26-4eab-8edf-5a5ccc14dee6" Name="ControlType" Type="Reference(Typified) Not Null" ReferencedTable="86bc6d2f-dce4-4382-ab7c-204bc9c0ec77">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d8adf3e0-ba26-00ab-4000-0a5ccc14dee6" Name="ControlTypeID" Type="Guid Not Null" ReferencedColumn="e15cff17-155b-4997-8552-ef76d42f7801" />
		<SchemeReferencingColumn ID="f591ab3e-0592-46f2-bb0b-d8c21c36d726" Name="ControlTypeName" Type="String(128) Not Null" ReferencedColumn="e3729b05-c5f0-4d79-9612-e6ceec0ec4aa" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="de413ed8-e009-41ed-918c-df0aeac34d1a" Name="ControlMethod" Type="Reference(Typified) Not Null" ReferencedTable="31d12e30-acd4-4c46-87ba-0bc3fc4febe3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="de413ed8-e009-00ed-4000-0f0aeac34d1a" Name="ControlMethodID" Type="Guid Not Null" ReferencedColumn="b47f144f-2244-487b-b7e5-7aa510cbe851">
			<SchemeDefaultConstraint IsPermanent="true" ID="f216ea2f-c3ba-4641-9df9-8dabd9a94042" Name="df_ApprovalProcessControlAction_ControlMethodID" Value="d363a5e1-c0e0-4ca4-9b2c-dc8c438774d1" />
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="4d06bede-8fd3-4ffd-a2ee-c526e5d1a38e" Name="ControlMethodName" Type="String(128) Not Null" ReferencedColumn="90221ceb-5b2f-4e2b-b6c7-980acec3e2e1">
			<SchemeDefaultConstraint IsPermanent="true" ID="38f489f8-cfc5-464d-846c-c2981dbd7418" Name="df_ApprovalProcessControlAction_ControlMethodName" Value="$ApprovalProcess_ControlMethods_ByLink" />
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="93a2b331-702d-4699-9230-169e2134fc03" Name="State" Type="Reference(Typified) Null" ReferencedTable="7845ed70-a6bc-4b62-8d3d-7a219df2f0fb">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="93a2b331-702d-0099-4000-069e2134fc03" Name="StateID" Type="Int32 Null" ReferencedColumn="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c" />
		<SchemeReferencingColumn ID="68417fdd-dac0-4f74-a28b-3c2485774a9f" Name="StateName" Type="String(128) Null" ReferencedColumn="39a2a196-7dac-42c4-b2ac-3492da8e5139" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="95b4a948-9391-4f32-8bff-a17b69184230" Name="UpdateHistoryGroup" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="c47dcca7-4dfc-404b-9aae-e977704ec06c" Name="df_ApprovalProcessControlAction_UpdateHistoryGroup" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="783fbdd2-f403-448a-9f86-3cdb345a7c3c" Name="ShowRevokeButton" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="b0730992-5e07-41b3-bc84-522ef18b4080" Name="df_ApprovalProcessControlAction_ShowRevokeButton" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="e26fb627-5619-4780-8011-e04f6ff41b1b" Name="InfoMode" Type="Reference(Typified) Null" ReferencedTable="83f77ddf-94f8-4322-918f-151c32bf58b1">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e26fb627-5619-0080-4000-004f6ff41b1b" Name="InfoModeID" Type="Int32 Null" ReferencedColumn="92f68dcb-2ade-417a-8325-499647bfdfe1" />
		<SchemeReferencingColumn ID="6778cc31-439a-4928-9535-d64909a2df70" Name="InfoModeName" Type="String(128) Null" ReferencedColumn="27821845-4b78-480f-a0ed-11f691752b7e" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="7a791775-ad11-009b-5000-0d0cc0aaf29d" Name="pk_ApprovalProcessControlAction" IsClustered="true">
		<SchemeIndexedColumn Column="7a791775-ad11-019b-4000-0d0cc0aaf29d" />
	</SchemePrimaryKey>
</SchemeTable>