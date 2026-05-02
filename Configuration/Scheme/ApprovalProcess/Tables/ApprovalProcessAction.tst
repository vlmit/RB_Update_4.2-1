<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="26484dc5-6148-49a5-9010-f1dd0e46ba6d" Name="ApprovalProcessAction" Group="ApprovalProcess" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="26484dc5-6148-00a5-2000-01dd0e46ba6d" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="26484dc5-6148-01a5-4000-01dd0e46ba6d" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="9491bb9b-f625-40ff-a117-15627a20c6fb" Name="Template" Type="Reference(Typified) Null" ReferencedTable="5cfc5ed0-ba1e-4068-8385-c2e7e8178a93" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9491bb9b-f625-00ff-4000-05627a20c6fb" Name="TemplateID" Type="Guid Null" ReferencedColumn="5cfc5ed0-ba1e-0168-4000-02e7e8178a93" />
		<SchemeReferencingColumn ID="7db0235e-6da5-4ee2-a5c4-9c40951a9122" Name="TemplateName" Type="String(128) Null" ReferencedColumn="08289207-172a-4899-ba7c-4f47967af3a4" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="314ef809-cc18-450a-9f94-44aecfb49a32" Name="InstanceID" Type="Guid Null" />
	<SchemePhysicalColumn ID="32a12b91-ac97-40de-b159-1e53ce27e97d" Name="UseProcessFromCard" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="2dde640b-f4d0-4592-8aae-c60071056b8e" Name="df_ApprovalProcessAction_UseProcessFromCard" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e86e68c6-46ee-4479-87c5-7808277014d7" Name="ReturnAfterDisapproval" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="3456ca0e-7b8b-4d0a-a1d7-16e98a27a6e9" Name="df_ApprovalProcessAction_ReturnAfterDisapproval" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="57021369-e307-46c2-a47f-9ec8e418452e" Name="ChangeState" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="ac77ba97-baf6-4890-8c73-8b716482e622" Name="df_ApprovalProcessAction_ChangeState" Value="true" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="00d395e8-6f11-48fa-8662-8cf04ba95af9" Name="ShowRevokeButton" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="ca525ff3-dbef-454d-9863-a2be2e06fa83" Name="df_ApprovalProcessAction_ShowRevokeButton" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="090ca87e-a448-4d0d-9a78-0a5509877d55" Name="InfoMode" Type="Reference(Typified) Not Null" ReferencedTable="83f77ddf-94f8-4322-918f-151c32bf58b1">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="090ca87e-a448-000d-4000-0a5509877d55" Name="InfoModeID" Type="Int32 Not Null" ReferencedColumn="92f68dcb-2ade-417a-8325-499647bfdfe1">
			<SchemeDefaultConstraint IsPermanent="true" ID="27a8a634-7c5a-4661-851a-cc6f16fa0d0a" Name="df_ApprovalProcessAction_InfoModeID" Value="2" />
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="b2f7fb29-545d-49bd-a436-94aa1f9a5d70" Name="InfoModeName" Type="String(128) Not Null" ReferencedColumn="27821845-4b78-480f-a0ed-11f691752b7e">
			<SchemeDefaultConstraint IsPermanent="true" ID="5e45a979-5000-425e-a6ab-33a4f4879889" Name="df_ApprovalProcessAction_InfoModeName" Value="$ApprovalProcess_InfoModes_ShowIfActive" />
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="26484dc5-6148-00a5-5000-01dd0e46ba6d" Name="pk_ApprovalProcessAction" IsClustered="true">
		<SchemeIndexedColumn Column="26484dc5-6148-01a5-4000-01dd0e46ba6d" />
	</SchemePrimaryKey>
</SchemeTable>