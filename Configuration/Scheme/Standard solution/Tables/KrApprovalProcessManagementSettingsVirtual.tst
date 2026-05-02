<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="028b9ba1-7b9e-4f57-a933-3b018aa333b9" Name="KrApprovalProcessManagementSettingsVirtual" Group="KrStageTypes" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="028b9ba1-7b9e-0057-2000-0b018aa333b9" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="028b9ba1-7b9e-0157-4000-0b018aa333b9" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="6e7647f3-12f6-4dbd-b118-a7facca9129d" Name="ControlType" Type="Reference(Typified) Not Null" ReferencedTable="86bc6d2f-dce4-4382-ab7c-204bc9c0ec77" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="6e7647f3-12f6-00bd-4000-07facca9129d" Name="ControlTypeID" Type="Guid Not Null" ReferencedColumn="e15cff17-155b-4997-8552-ef76d42f7801" />
		<SchemeReferencingColumn ID="a6a5d977-cabf-415c-bdd2-93a76ba795a9" Name="ControlTypeName" Type="String(128) Not Null" ReferencedColumn="e3729b05-c5f0-4d79-9612-e6ceec0ec4aa" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="07cf38e4-0882-40fd-b451-9ff45d956713" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="7845ed70-a6bc-4b62-8d3d-7a219df2f0fb" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="07cf38e4-0882-00fd-4000-0ff45d956713" Name="StateID" Type="Int32 Not Null" ReferencedColumn="fb6cce1d-cb7f-457a-a82f-42efe9ae9a7c" />
		<SchemeReferencingColumn ID="238a29c6-53c4-4446-aa81-b32b34f3ac00" Name="StateName" Type="String(128) Not Null" ReferencedColumn="39a2a196-7dac-42c4-b2ac-3492da8e5139" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="6fe6ad4a-d008-4d89-bc6a-f2adeea9ea55" Name="UpdateHistoryGroup" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="ee3e40db-6a5f-4ce9-a0ad-ed61f3877c95" Name="df_KrApprovalProcessManagementSettingsVirtual_UpdateHistoryGroup" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="26f2e1ba-3969-4659-8dd8-65ac0ad87f3a" Name="ShowRevokeButton" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="3bbf0ccc-626f-4f52-918b-8c82977bf273" Name="df_KrApprovalProcessManagementSettingsVirtual_ShowRevokeButton" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="4219654d-49de-4267-abcc-cafe53b2a74e" Name="SecondaryRevokeProcess" Type="Reference(Typified) Null" ReferencedTable="caac66aa-0cbb-4e2b-83fd-7c368e814d64">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="4219654d-49de-0067-4000-0afe53b2a74e" Name="SecondaryRevokeProcessID" Type="Guid Null" ReferencedColumn="caac66aa-0cbb-012b-4000-0c368e814d64" />
		<SchemeReferencingColumn ID="c5b9e0a8-2183-4f1b-80c0-8571c654791e" Name="SecondaryRevokeProcessName" Type="String(255) Null" ReferencedColumn="444b8925-572a-449b-901e-8660ddeb3b6c" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="9b61f09e-54e9-49a7-80e3-8dc68cd50714" Name="InfoMode" Type="Reference(Typified) Null" ReferencedTable="83f77ddf-94f8-4322-918f-151c32bf58b1">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9b61f09e-54e9-00a7-4000-0dc68cd50714" Name="InfoModeID" Type="Int32 Null" ReferencedColumn="92f68dcb-2ade-417a-8325-499647bfdfe1" />
		<SchemeReferencingColumn ID="23c73cf1-78ec-4b98-8835-a14bd2e9ce4f" Name="InfoModeName" Type="String(128) Null" ReferencedColumn="27821845-4b78-480f-a0ed-11f691752b7e" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="028b9ba1-7b9e-0057-5000-0b018aa333b9" Name="pk_KrApprovalProcessManagementSettingsVirtual" IsClustered="true">
		<SchemeIndexedColumn Column="028b9ba1-7b9e-0157-4000-0b018aa333b9" />
	</SchemePrimaryKey>
</SchemeTable>