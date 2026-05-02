<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="9ab531d3-cd3a-4461-9413-6ca311dab1f5" Name="KrApprovalProcessSettingsVirtual" Group="KrStageTypes" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9ab531d3-cd3a-0061-2000-0ca311dab1f5" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9ab531d3-cd3a-0161-4000-0ca311dab1f5" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="3b5e185b-e6a4-42c7-ac16-c4821c4453fc" Name="Template" Type="Reference(Typified) Null" ReferencedTable="5cfc5ed0-ba1e-4068-8385-c2e7e8178a93" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3b5e185b-e6a4-00c7-4000-04821c4453fc" Name="TemplateID" Type="Guid Null" ReferencedColumn="5cfc5ed0-ba1e-0168-4000-02e7e8178a93" />
		<SchemeReferencingColumn ID="eb0ca0ae-d5df-4b24-9da1-3261db82b5af" Name="TemplateName" Type="String(128) Null" ReferencedColumn="08289207-172a-4899-ba7c-4f47967af3a4" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="8ea43920-f415-4745-ac66-98fa2444e966" Name="ReturnAfterDisapproval" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="50679c05-96a4-43f2-a360-cbb59a1f23f2" Name="df_KrApprovalProcessSettingsVirtual_ReturnAfterDisapproval" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="27d2525b-accd-4dfb-83b4-22960091975d" Name="ChangeState" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="538a4dfa-cb19-4d50-86e5-0aa5f6b124b8" Name="df_KrApprovalProcessSettingsVirtual_ChangeState" Value="true" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="511a5a99-11f8-45d8-8185-6400191420a0" Name="UseProcessFromCard" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="aa57f4d4-9841-44f9-acc4-7538631c647f" Name="df_KrApprovalProcessSettingsVirtual_UseProcessFromCard" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="0e57102f-5240-418c-9e38-13d183d60391" Name="ShowRevokeButton" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="bbef5fad-d4c7-4a2c-94b0-fda66902ed20" Name="df_KrApprovalProcessSettingsVirtual_ShowRevokeButton" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="0d7365b3-a8f1-477f-99db-b8150641f633" Name="NotReturnEdit" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="545928f8-5297-4756-923e-dc7d24cf3319" Name="df_KrApprovalProcessSettingsVirtual_NotReturnEdit" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="3a8ac70a-edd6-494c-bdbf-411fbe3cb094" Name="SecondaryRevokeProcess" Type="Reference(Typified) Null" ReferencedTable="caac66aa-0cbb-4e2b-83fd-7c368e814d64">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3a8ac70a-edd6-004c-4000-011fbe3cb094" Name="SecondaryRevokeProcessID" Type="Guid Null" ReferencedColumn="caac66aa-0cbb-012b-4000-0c368e814d64" />
		<SchemeReferencingColumn ID="72e934a8-0b92-42f8-88bc-4d03fb0b8ca5" Name="SecondaryRevokeProcessName" Type="String(255) Null" ReferencedColumn="444b8925-572a-449b-901e-8660ddeb3b6c" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="7cd84994-127d-40e2-8546-a45fa684a6ca" Name="InfoMode" Type="Reference(Typified) Not Null" ReferencedTable="83f77ddf-94f8-4322-918f-151c32bf58b1">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7cd84994-127d-00e2-4000-045fa684a6ca" Name="InfoModeID" Type="Int32 Not Null" ReferencedColumn="92f68dcb-2ade-417a-8325-499647bfdfe1">
			<SchemeDefaultConstraint IsPermanent="true" ID="50774b6d-93c3-4e06-8fdb-0e1a4270ab43" Name="df_KrApprovalProcessSettingsVirtual_InfoModeID" Value="2" />
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="f4d430c1-4554-426c-8da5-a7e54b8d6290" Name="InfoModeName" Type="String(128) Not Null" ReferencedColumn="27821845-4b78-480f-a0ed-11f691752b7e">
			<SchemeDefaultConstraint IsPermanent="true" ID="23afe10c-a3d3-4eae-ba05-dd04aacc4515" Name="df_KrApprovalProcessSettingsVirtual_InfoModeName" Value="$ApprovalProcess_InfoModes_ShowIfActive" />
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="9ab531d3-cd3a-0061-5000-0ca311dab1f5" Name="pk_KrApprovalProcessSettingsVirtual" IsClustered="true">
		<SchemeIndexedColumn Column="9ab531d3-cd3a-0161-4000-0ca311dab1f5" />
	</SchemePrimaryKey>
</SchemeTable>