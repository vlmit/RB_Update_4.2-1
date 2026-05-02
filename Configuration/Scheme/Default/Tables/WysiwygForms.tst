<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="08fa2157-d17f-4e45-b9b4-a5857a09f46e" Name="WysiwygForms" Group="WysiwygEditor">
	<SchemePhysicalColumn ID="88610ff7-3a4c-43b9-a36a-11b038bf68d8" Name="Version" Type="Int16 Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="d40e45b2-4e75-4032-997a-6f82d40a4aa0" Name="df_WysiwygForms_Version" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="07d482a1-4a4f-43ec-b893-04eee7426caf" Name="Alias" Type="String(256) Not Null" />
	<SchemeComplexColumn ID="1db08491-ca05-4914-bd8c-e08cb8d90981" Name="CardType" Type="Reference(Typified) Null" ReferencedTable="b0538ece-8468-4d0b-8b4e-5a1d43e024db">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1db08491-ca05-0014-4000-008cb8d90981" Name="CardTypeID" Type="Guid Null" ReferencedColumn="a628a864-c858-4200-a6b7-da78c8e6e1f4" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="950b2866-30db-4213-ac67-d7eaa8202aa7" Name="Child" Type="Json Null" />
	<SchemePhysicalColumn ID="ae313cb6-7531-417d-81cd-830603756765" Name="ID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="8b2fcf14-99ea-49f0-91d5-e314ba9e2863" Name="Bundle" Type="Reference(Typified) Null" ReferencedTable="2093688e-4577-42be-847a-bd2891902ffe">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="8b2fcf14-99ea-00f0-4000-0314ba9e2863" Name="BundleID" Type="Guid Null" ReferencedColumn="462bce31-81dd-47d5-be7a-1ed159367586" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="928c50ed-9be3-48c1-96a4-4fcf7ac1ab33" Name="ViewPortWidth" Type="Int32 Null" />
	<SchemePhysicalColumn ID="cef280b8-2603-4201-9a79-3d5dda9a3395" Name="ViewPortHeight" Type="Int32 Null" />
	<SchemePhysicalColumn ID="f9e19e06-ba0a-4dad-b7d8-9c37a634bd88" Name="Caption" Type="String(256) Null" />
	<SchemePhysicalColumn ID="c54af565-e2d7-475a-9e10-f50564eb2c5a" Name="BackgroundType" Type="String(32) Null" />
	<SchemeComplexColumn ID="2625a103-655d-4b3b-9418-3c1066d1ffae" Name="TaskType" Type="Reference(Typified) Null" ReferencedTable="b0538ece-8468-4d0b-8b4e-5a1d43e024db">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="2625a103-655d-003b-4000-0c1066d1ffae" Name="TaskTypeID" Type="Guid Null" ReferencedColumn="a628a864-c858-4200-a6b7-da78c8e6e1f4" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="d97dea54-683a-4f8c-aaed-775a5582c372" Name="Description" Type="String(1024) Null" />
	<SchemePrimaryKey ID="ad6cebbb-afdc-4eb5-b106-ab587af2170d" Name="pk_WysiwygForms">
		<SchemeIndexedColumn Column="ae313cb6-7531-417d-81cd-830603756765" />
	</SchemePrimaryKey>
	<SchemeIndex ID="ed08c6b5-e7a1-4bcc-b303-75159f48ea01" Name="ndx_WysiwygForms_CardTypeIDAlias">
		<SchemeIndexedColumn Column="1db08491-ca05-0014-4000-008cb8d90981" />
		<SchemeIndexedColumn Column="07d482a1-4a4f-43ec-b893-04eee7426caf" />
	</SchemeIndex>
</SchemeTable>