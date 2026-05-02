<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="f1278826-59c5-4728-85a0-3d1907db4dc8" Name="AiToolVirtual" Group="AI" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="f1278826-59c5-0028-2000-0d1907db4dc8" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="f1278826-59c5-0128-4000-0d1907db4dc8" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn Partition="cc57f9b4-e75d-4c83-a1da-b650fb8e8149" ID="31c8c133-ca51-4185-810e-6f51e222970a" Name="Disabled" Type="Boolean Not Null">
		<Description>Признак, что инструмент отключен</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="4489f913-fb24-4377-817b-23653016e268" Name="df_AiToolVirtual_Disabled" Value="false" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="f1278826-59c5-0028-5000-0d1907db4dc8" Name="pk_AiToolVirtual" IsClustered="true">
		<SchemeIndexedColumn Column="f1278826-59c5-0128-4000-0d1907db4dc8" />
	</SchemePrimaryKey>
</SchemeTable>