<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="033a363a-e183-4084-83cb-4672841a2a90" Name="BusinessProcessButtonsVirtual" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Секция с описанием кнопок бизнес-процесса (как запускающих сам процесс, так и отправляющих команду для процесса), в которую добавляются колонки из расширений на кнопки процесса.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="033a363a-e183-0084-2000-0672841a2a90" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="033a363a-e183-0184-4000-0672841a2a90" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="033a363a-e183-0084-3100-0672841a2a90" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="9ffb7afb-973a-4ece-a7d9-7d4ae6592b92" Name="Caption" Type="String(256) Not Null" />
	<SchemePhysicalColumn ID="89e4025d-8606-4dc1-b8e9-ee3147226638" Name="StartProcess" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="0548e610-298a-446f-93ee-1a537fcb2abe" Name="df_BusinessProcessButtonsVirtual_StartProcess" Value="true" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="caaa8f8c-b74c-4bc6-bdc4-0af9cf967e17" Name="Signal" Type="Reference(Typified) Null" ReferencedTable="53dc8c0b-391a-4fbd-86c0-3da697abf065" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="caaa8f8c-b74c-00c6-4000-0af9cf967e17" Name="SignalID" Type="Guid Null" ReferencedColumn="cabbc72d-b093-43be-a645-8503664980d6" />
		<SchemeReferencingColumn ID="8af797e2-e706-49f9-807d-748377cf234c" Name="SignalName" Type="String(128) Null" ReferencedColumn="2e7c413d-0de6-4900-ac97-68ce16e3da11" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="aecf6686-5ebd-4185-995e-ed6fcd8ebeb6" Name="Group" Type="String(256) Null" />
	<SchemePhysicalColumn ID="7716b667-fe35-4b1d-8693-3be66c7a42e5" Name="Icon" Type="String(128) Null" />
	<SchemePhysicalColumn ID="fbb8f526-9cca-4561-85f1-77902543bcc5" Name="Description" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="fe601e1b-8ce8-4217-925d-2fca8804e0d1" Name="Condition" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="5e02520a-5c08-4c61-b3fb-a9df37045995" Name="Tooltip" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="3004585e-14a7-485c-8192-79564696d414" Name="AskConfirmation" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="89bd9ecf-a1c5-4907-a696-bfe4bf290f56" Name="df_BusinessProcessButtonsVirtual_AskConfirmation" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="fe50d837-f56a-47ba-a422-0f132d3110be" Name="ConfirmationMessage" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="15d24f93-16de-41ad-a391-724732c5f4dd" Name="ActionGrouping" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="31c2ca81-3e9d-4b90-afe0-f7b61b86a4aa" Name="df_BusinessProcessButtonsVirtual_ActionGrouping" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="afa4f82e-1492-4dfb-be5f-f031611ce3dd" Name="DisplaySettings" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="4b19f78a-0b78-45a4-8322-89a8f4f356cc" Name="ButtonHotkey" Type="String(256) Null" />
	<SchemePhysicalColumn ID="fe1b6b3f-6437-4eb3-be6d-321cfefdee5f" Name="AccessDeniedMessage" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="8d128d10-5886-4e5c-be0c-c9137c353d1e" Name="Order" Type="Int32 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="cb4697a6-9fe5-4e95-9af9-f995f646535e" Name="df_BusinessProcessButtonsVirtual_Order" Value="0" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="8174724c-b26e-4b79-8284-778f3787617e" Name="ToolbarVisibilityMode" Type="Reference(Typified) Not Null" ReferencedTable="08dfa806-532d-462a-959f-5c864af55f02">
		<Description>Режим отображения на тулбаре</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="8174724c-b26e-0079-4000-078f3787617e" Name="ToolbarVisibilityModeID" Type="Int32 Not Null" ReferencedColumn="f516ae6d-c644-4a68-b4ca-20d162510516">
			<SchemeDefaultConstraint IsPermanent="true" ID="a0c1467f-08be-412b-9843-f0b970ccebc6" Name="df_BusinessProcessButtonsVirtual_ToolbarVisibilityModeID" Value="0" />
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="68239bcd-831c-450a-adec-9b1732a1e992" Name="ToolbarVisibilityModeName" Type="String(Max) Not Null" ReferencedColumn="6d25a78b-9ae8-45e2-85e6-e45a2986725a">
			<SchemeDefaultConstraint IsPermanent="true" ID="92012185-16d8-488e-ab48-22ce69bd2225" Name="df_BusinessProcessButtonsVirtual_ToolbarVisibilityModeName" Value="$WorkflowEngine_BusinessProcessButtonToolbarVisibilityModes_DoNotShowOnToolbar" />
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="b6a4d5dc-d4f6-4324-843a-644b8e3048ad" Name="AllowedVersions" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="0792aff5-ad32-4c1e-940b-902b886f30eb" Name="Alias" Type="String(Max) Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="17945673-4fc4-45c4-8d0e-f981d355112c" Name="df_BusinessProcessButtonsVirtual_Alias" Value="" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e578b17c-98d0-4178-a72e-f348a7392eff" Name="Hidden" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="5957b9e2-8270-46f7-961c-06c25f8439ed" Name="df_BusinessProcessButtonsVirtual_Hidden" Value="false" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="850f5d7e-6db0-455b-bbe6-97b8c7ef9d32" Name="TileHandler" Type="Reference(Abstract) Null" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="850f5d7e-6db0-005b-4000-07b8c7ef9d32" Name="TileHandlerID" Type="Guid Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
		<SchemePhysicalColumn ID="610c8102-f1bc-4054-98cb-1cc15ed44568" Name="TileHandlerName" Type="String(128) Null" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="033a363a-e183-0084-5000-0672841a2a90" Name="pk_BusinessProcessButtonsVirtual">
		<SchemeIndexedColumn Column="033a363a-e183-0084-3100-0672841a2a90" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="033a363a-e183-0084-7000-0672841a2a90" Name="idx_BusinessProcessButtonsVirtual_ID" IsClustered="true">
		<SchemeIndexedColumn Column="033a363a-e183-0184-4000-0672841a2a90" />
	</SchemeIndex>
</SchemeTable>