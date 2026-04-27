<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="4b9735f4-7db4-46e1-bbdd-4bd71f5234bd" Name="KrTaskTypeConditionSettings" Group="Kr" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="4b9735f4-7db4-00e1-2000-0bd71f5234bd" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="4b9735f4-7db4-01e1-4000-0bd71f5234bd" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="14b9ba40-3ed4-43e5-ad61-080257a4817b" Name="InProgress" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="35b88f96-1e07-4a18-a9d3-b6dc2ccca9ca" Name="df_KrTaskTypeConditionSettings_InProgress" Value="false" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="4b9735f4-7db4-00e1-5000-0bd71f5234bd" Name="pk_KrTaskTypeConditionSettings" IsClustered="true">
		<SchemeIndexedColumn Column="4b9735f4-7db4-01e1-4000-0bd71f5234bd" />
	</SchemePrimaryKey>
</SchemeTable>