<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="37d8cfe8-6b12-4b5e-a537-c77d7e168f4f" Name="ApprovalProcessSettings" Group="ApprovalProcess" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="37d8cfe8-6b12-005e-2000-077d7e168f4f" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="37d8cfe8-6b12-015e-4000-077d7e168f4f" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="7d3e0dae-9b74-4faf-b9e9-d202b2839345" Name="DefaultDuration" Type="Double Not Null">
		<Description>Срок задания в рабочих днях по умолчанию</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="6341ebcc-8682-44f1-b303-da302f6757fe" Name="df_ApprovalProcessSettings_DefaultDuration" Value="1" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="37d8cfe8-6b12-005e-5000-077d7e168f4f" Name="pk_ApprovalProcessSettings" IsClustered="true">
		<SchemeIndexedColumn Column="37d8cfe8-6b12-015e-4000-077d7e168f4f" />
	</SchemePrimaryKey>
</SchemeTable>