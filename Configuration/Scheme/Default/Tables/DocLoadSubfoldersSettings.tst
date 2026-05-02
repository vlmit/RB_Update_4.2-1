<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="a55a3478-3e0d-4fb2-b41b-7fdc40ffe06a" Name="DocLoadSubfoldersSettings" Group="System" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="a55a3478-3e0d-00b2-2000-0fdc40ffe06a" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a55a3478-3e0d-01b2-4000-0fdc40ffe06a" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="a55a3478-3e0d-00b2-3100-0fdc40ffe06a" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="71e44f97-bfbb-4a63-81b3-72983617507d" Name="Path" Type="String(Max) Not Null">
		<Description>Из данной подпапки система будет брать файлы для обработки, после перемещать в обработанные успешно или с ошибками.</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="c82ebc8f-6422-4915-8048-fd6c6a4a1ea3" Name="Behavior" Type="Reference(Typified) Not Null" ReferencedTable="dd625440-b34b-4c48-9991-3a45b2028809" WithForeignKey="false">
		<SchemeReferencingColumn ID="bfd2c692-7b38-4573-8906-10e3566c5b72" Name="BehaviorID" Type="Guid Not Null" ReferencedColumn="956b50a8-ad12-4ecc-b857-45695862a63d" />
		<SchemeReferencingColumn ID="959b17f9-79d2-4499-8865-d357f142e40b" Name="BehaviorName" Type="String(Max) Not Null" ReferencedColumn="493f3054-2152-44e0-af09-ea92493c6164" />
		<SchemeReferencingColumn ID="675fbafb-413b-48cd-81e4-a39e3c5ad61f" Name="BehaviorAlias" Type="String(Max) Not Null" ReferencedColumn="46ddaca5-efee-4370-b9f1-2bd7efdac4ff" />
		<SchemeReferencingColumn ID="3f24a78d-bdae-45dd-8b18-14a20b6c595e" Name="BehaviorSettings" Type="Json Null" ReferencedColumn="f5230fa6-b16a-418b-84c7-4e62d392d328" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="a55a3478-3e0d-00b2-5000-0fdc40ffe06a" Name="pk_DocLoadSubfoldersSettings">
		<SchemeIndexedColumn Column="a55a3478-3e0d-00b2-3100-0fdc40ffe06a" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="a55a3478-3e0d-00b2-7000-0fdc40ffe06a" Name="idx_DocLoadSubfoldersSettings_ID" IsClustered="true">
		<SchemeIndexedColumn Column="a55a3478-3e0d-01b2-4000-0fdc40ffe06a" />
	</SchemeIndex>
</SchemeTable>