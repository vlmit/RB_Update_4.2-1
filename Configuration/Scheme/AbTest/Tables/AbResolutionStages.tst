<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="5c4efc57-f311-4292-9cb1-7be629984589" ID="ae17aa42-0478-40af-9fde-1e075a388026" Name="AbResolutionStages" Group="AbTest" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="ae17aa42-0478-00af-2000-0e075a388026" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ae17aa42-0478-01af-4000-0e075a388026" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="ae17aa42-0478-00af-3100-0e075a388026" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="160630ce-110a-48c1-aa73-493fd065aa1f" Name="Name" Type="String(Max) Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="f334116e-2df9-4e0a-91ef-9e22e959f438" Name="df_AbResolutionStages_Name" Value="" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="ef19fd3d-8c1f-4075-b9eb-930d00e63c0e" Name="Comment" Type="String(Max) Not Null" IsVirtual="true" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="ae17aa42-0478-00af-5000-0e075a388026" Name="pk_AbResolutionStages">
		<SchemeIndexedColumn Column="ae17aa42-0478-00af-3100-0e075a388026" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="ae17aa42-0478-00af-7000-0e075a388026" Name="idx_AbResolutionStages_ID" IsClustered="true">
		<SchemeIndexedColumn Column="ae17aa42-0478-01af-4000-0e075a388026" />
	</SchemeIndex>
</SchemeTable>