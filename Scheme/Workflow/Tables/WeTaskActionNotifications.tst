<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="9bfb39f5-2d15-4c23-82df-dcfaeb13fde4" Name="WeTaskActionNotifications" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Уведомления о задании</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9bfb39f5-2d15-0023-2000-0cfaeb13fde4" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="9bfb39f5-2d15-0123-4000-0cfaeb13fde4" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="9bfb39f5-2d15-0023-3100-0cfaeb13fde4" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="d97f4b8b-55cf-4044-8895-5de5735c36eb" Name="Notification" Type="Reference(Typified) Not Null" ReferencedTable="18145bb5-fd4e-4795-aa1f-9e1cd9b4ee5a" WithForeignKey="false">
		<Description>Уведомление</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d97f4b8b-55cf-0044-4000-0de5735c36eb" Name="NotificationID" Type="Guid Not Null" ReferencedColumn="18145bb5-fd4e-0195-4000-0e1cd9b4ee5a" />
		<SchemeReferencingColumn ID="9f030919-ee24-49d2-8f1f-03e7b35bfca5" Name="NotificationName" Type="String(256) Not Null" ReferencedColumn="265d4336-6764-4db8-8874-0e5fa92cbd5d" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="b8ca4196-16d2-4645-a2c8-f34edd9ab947" Name="ExcludeDeputies" Type="Boolean Not Null">
		<Description>Не отправлять заместителям</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="f8734b62-0254-4c89-ab01-e1e43aac0d97" Name="df_WeTaskActionNotifications_ExcludeDeputies" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="1fd14066-8b65-4868-8b18-917f3e340842" Name="ExcludeSubscribers" Type="Boolean Not Null">
		<Description>Не отправлять подписчикам</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="32b07dc5-3b6f-4f5e-a1de-d1c114889fbc" Name="df_WeTaskActionNotifications_ExcludeSubscribers" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="41e05716-551f-40ad-8250-ff2e82877f17" Name="NotificationScript" Type="String(Max) Null">
		<Description>Скрипт для изменения уведомления</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="9bfb39f5-2d15-0023-5000-0cfaeb13fde4" Name="pk_WeTaskActionNotifications">
		<SchemeIndexedColumn Column="9bfb39f5-2d15-0023-3100-0cfaeb13fde4" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="9bfb39f5-2d15-0023-7000-0cfaeb13fde4" Name="idx_WeTaskActionNotifications_ID" IsClustered="true">
		<SchemeIndexedColumn Column="9bfb39f5-2d15-0123-4000-0cfaeb13fde4" />
	</SchemeIndex>
</SchemeTable>