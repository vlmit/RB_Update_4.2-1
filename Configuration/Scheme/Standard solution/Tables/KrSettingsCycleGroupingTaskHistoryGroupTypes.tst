<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="d3a3055e-de16-4465-92ae-0ce25cea9812" Name="KrSettingsCycleGroupingTaskHistoryGroupTypes" Group="Kr" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="d3a3055e-de16-0065-2000-0ce25cea9812" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d3a3055e-de16-0165-4000-0ce25cea9812" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="d3a3055e-de16-0065-3100-0ce25cea9812" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="d65d3677-9c60-4634-9f70-dd65b83d0bcc" Name="GroupType" Type="Reference(Typified) Not Null" ReferencedTable="319be329-6cd3-457a-b792-41c26a266b95" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d65d3677-9c60-0034-4000-0d65b83d0bcc" Name="GroupTypeID" Type="Guid Not Null" ReferencedColumn="319be329-6cd3-017a-4000-01c26a266b95" />
		<SchemeReferencingColumn ID="ce3172ab-04f9-44ce-bd94-666962ce39db" Name="GroupTypeCaption" Type="String(128) Not Null" ReferencedColumn="bf5a5121-9947-45f6-a8a0-2608885b4e19" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="d3a3055e-de16-0065-5000-0ce25cea9812" Name="pk_KrSettingsCycleGroupingTaskHistoryGroupTypes">
		<SchemeIndexedColumn Column="d3a3055e-de16-0065-3100-0ce25cea9812" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="d3a3055e-de16-0065-7000-0ce25cea9812" Name="idx_KrSettingsCycleGroupingTaskHistoryGroupTypes_ID" IsClustered="true">
		<SchemeIndexedColumn Column="d3a3055e-de16-0165-4000-0ce25cea9812" />
	</SchemeIndex>
</SchemeTable>