<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="21f74d15-3eb4-41bb-ae78-07977c63734f" Name="Dashboards" Group="Dashboards" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="21f74d15-3eb4-00bb-2000-07977c63734f" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="21f74d15-3eb4-01bb-4000-07977c63734f" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="82649a13-ba29-464e-905d-799792a11208" Name="Owner" Type="Reference(Typified) Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<Description>Владелец дашборда</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="82649a13-ba29-004e-4000-099792a11208" Name="OwnerID" Type="Guid Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>Идентификатор владельца дашборда</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="b56da0d4-7167-4920-a6ca-46fa9a67f62b" Name="LayoutSettings" Type="BinaryJson Null" />
	<SchemePhysicalColumn ID="d88d7f2d-0cb1-4710-8ba1-4b097df06c2f" Name="Version" Type="Int32 Not Null" />
	<SchemeComplexColumn ID="570b92be-0c51-4437-b31f-2d8bbd6f3ec9" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="e9e0a007-c818-4813-8f39-c3b44fc74edd">
		<Description>Тип дашборда</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="570b92be-0c51-0037-4000-0d8bbd6f3ec9" Name="TypeID" Type="Int32 Not Null" ReferencedColumn="dc238801-e2f9-482c-a1f8-3555c845bce6">
			<Description>Идентификатор типа</Description>
			<SchemeDefaultConstraint IsPermanent="true" ID="c3506009-dc32-4d98-a210-9933625c96e3" Name="df_Dashboards_TypeID" Value="0" />
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="21f74d15-3eb4-00bb-5000-07977c63734f" Name="pk_Dashboards" IsClustered="true">
		<SchemeIndexedColumn Column="21f74d15-3eb4-01bb-4000-07977c63734f" />
	</SchemePrimaryKey>
	<SchemeIndex ID="118a14fb-5cae-438b-a129-9b0617243f0c" Name="ndx_Dashboards_OwnerIDTypeID" IsUnique="true">
		<Predicate Dbms="SqlServer">[TypeID] = 0</Predicate>
		<Predicate Dbms="PostgreSql">"TypeID" = 0</Predicate>
		<SchemeIndexedColumn Column="82649a13-ba29-004e-4000-099792a11208" />
		<SchemeIndexedColumn Column="570b92be-0c51-0037-4000-0d8bbd6f3ec9" />
		<SchemeIncludedColumn Column="21f74d15-3eb4-01bb-4000-07977c63734f" />
		<SchemeIncludedColumn Column="d88d7f2d-0cb1-4710-8ba1-4b097df06c2f" />
	</SchemeIndex>
</SchemeTable>