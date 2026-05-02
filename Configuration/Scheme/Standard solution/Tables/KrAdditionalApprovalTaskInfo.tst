<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="e0361d36-e2fd-48f9-875a-7ba9548932e5" Name="KrAdditionalApprovalTaskInfo" Group="KrStageTypes" InstanceType="Tasks" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="e0361d36-e2fd-00f9-2000-0ba9548932e5" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e0361d36-e2fd-01f9-4000-0ba9548932e5" Name="ID" Type="Guid Not Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="47cd87a0-1753-46d0-be1c-bb4c30677dbc" Name="Comment" Type="String(Max) Null" />
	<SchemePhysicalColumn ID="471d2240-713a-4551-803c-80ba62ddb846" Name="IsResponsible" Type="Boolean Not Null">
		<Description>Признак, что исполнитель установлен, как "Первый - ответсвенный".</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="c8dbbc65-8771-41dd-9cfb-7f2b623dd3bb" Name="df_KrAdditionalApprovalTaskInfo_IsResponsible" Value="false" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="e0361d36-e2fd-00f9-5000-0ba9548932e5" Name="pk_KrAdditionalApprovalTaskInfo" IsClustered="true">
		<SchemeIndexedColumn Column="e0361d36-e2fd-01f9-4000-0ba9548932e5" />
	</SchemePrimaryKey>
</SchemeTable>