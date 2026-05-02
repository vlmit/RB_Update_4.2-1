<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c6b5c416-c0b3-4d77-8ad3-7d8e4a041c5c" Name="MobileApplication" Group="System" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c6b5c416-c0b3-0077-2000-0d8e4a041c5c" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c6b5c416-c0b3-0177-4000-0d8e4a041c5c" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="59aa76d9-fcc8-44a1-88ae-0f9ad1ca20c1" Name="Release" Type="String(255) Not Null" />
	<SchemePhysicalColumn ID="d0a239ad-6da0-412a-86b8-c8ebd1360505" Name="PlatformVersion" Type="String(255) Not Null" />
	<SchemePhysicalColumn ID="242157bd-dd08-4bff-aafc-d12a8623e294" Name="BundleVersion" Type="String(255) Not Null" />
	<SchemePhysicalColumn ID="ac87ee6d-b23f-4b5d-acab-a07164152d36" Name="MinSupportNativeVersion" Type="String(255) Not Null" />
	<SchemePhysicalColumn ID="3b0d458e-f95c-4b39-b5f4-a242a7bc8711" Name="BundleStrategy" Type="String(255) Not Null" />
	<SchemePhysicalColumn ID="51231b21-a374-45ee-83bb-b7e013f32a1b" Name="PublicKey" Type="String(255) Not Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="c6b5c416-c0b3-0077-5000-0d8e4a041c5c" Name="pk_MobileApplication" IsClustered="true">
		<SchemeIndexedColumn Column="c6b5c416-c0b3-0177-4000-0d8e4a041c5c" />
	</SchemePrimaryKey>
</SchemeTable>