<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="c86d86d2-56df-4658-ba97-6504675f6daa" Name="WeNotificationTaskRoles" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c86d86d2-56df-0058-2000-0504675f6daa" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c86d86d2-56df-0158-4000-0504675f6daa" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="c86d86d2-56df-0058-3100-0504675f6daa" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="442cfde5-1133-4ad8-9ebf-721f4042deae" Name="TaskRole" Type="Reference(Typified) Not Null" ReferencedTable="a59078ce-8acf-4c45-a49a-503fa88a0580">
		<Description>Функциональная роль</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="442cfde5-1133-00d8-4000-021f4042deae" Name="TaskRoleID" Type="Guid Not Null" ReferencedColumn="bd4fdcea-8042-488a-94c9-770b49357cfe" />
		<SchemeReferencingColumn ID="b698259d-0ea6-4f62-93c7-4b8b95896de0" Name="TaskRoleCaption" Type="String(128) Not Null" ReferencedColumn="f8b3afc6-cea7-4a98-b907-e716e0a426c6" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="ed8eb7ca-3e7d-4ea8-bcb2-d08696ef14c7" Name="CompletionNotification" Type="Reference(Typified) Null" ReferencedTable="7d6ef28b-afc2-4de0-a34b-2ace26a88d2a" IsReferenceToOwner="true" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="ed8eb7ca-3e7d-00a8-4000-008696ef14c7" Name="CompletionNotificationRowID" Type="Guid Null" ReferencedColumn="7d6ef28b-afc2-00e0-3100-0ace26a88d2a" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="fe6eb99d-e7dc-4a3c-89bf-4cad40dd4b62" Name="Notification" Type="Reference(Typified) Null" ReferencedTable="9bfb39f5-2d15-4c23-82df-dcfaeb13fde4" IsReferenceToOwner="true" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="fe6eb99d-e7dc-003c-4000-0cad40dd4b62" Name="NotificationRowID" Type="Guid Null" ReferencedColumn="9bfb39f5-2d15-0023-3100-0cfaeb13fde4" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="c86d86d2-56df-0058-5000-0504675f6daa" Name="pk_WeNotificationTaskRoles">
		<SchemeIndexedColumn Column="c86d86d2-56df-0058-3100-0504675f6daa" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="c86d86d2-56df-0058-7000-0504675f6daa" Name="idx_WeNotificationTaskRoles_ID" IsClustered="true">
		<SchemeIndexedColumn Column="c86d86d2-56df-0158-4000-0504675f6daa" />
	</SchemeIndex>
</SchemeTable>