<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="7d6ef28b-afc2-4de0-a34b-2ace26a88d2a" Name="WeTaskActionCompletionNotifications" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Collections">
	<Description>Уведомления о завершении задания</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="7d6ef28b-afc2-00e0-2000-0ace26a88d2a" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="7d6ef28b-afc2-01e0-4000-0ace26a88d2a" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="7d6ef28b-afc2-00e0-3100-0ace26a88d2a" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="101d4bb6-1d27-4521-9ec6-412856674639" Name="Notification" Type="Reference(Typified) Not Null" ReferencedTable="18145bb5-fd4e-4795-aa1f-9e1cd9b4ee5a">
		<Description>Уведомление</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="101d4bb6-1d27-0021-4000-012856674639" Name="NotificationID" Type="Guid Not Null" ReferencedColumn="18145bb5-fd4e-0195-4000-0e1cd9b4ee5a" />
		<SchemeReferencingColumn ID="c3c8eba2-83be-4fde-8efc-733afe225c22" Name="NotificationName" Type="String(256) Not Null" ReferencedColumn="265d4336-6764-4db8-8874-0e5fa92cbd5d" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="d3aaa9fb-9b98-4054-b784-e2ad884222d1" Name="ExcludeDeputies" Type="Boolean Not Null">
		<Description>Не отправлять заместителям</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="fec58073-5b9d-499f-9bc3-a3ee78b6f237" Name="df_WeTaskActionCompletionNotifications_ExcludeDeputies" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="a47a95c9-805e-496f-924d-a4bce392f27d" Name="ExcludeSubscribers" Type="Boolean Not Null">
		<Description>Не отправлять подписчикам</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="f0329d3b-628c-40d0-aa11-fa37e1605631" Name="df_WeTaskActionCompletionNotifications_ExcludeSubscribers" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="215c8eb4-fe43-408d-a292-a8695a522e5e" Name="NotificationScript" Type="String(Max) Not Null">
		<Description>Скрипт для изменения уведомления</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="c5b2d558-fd1b-4258-a9e8-15f79179e0aa" Name="TaskOption" Type="Reference(Typified) Not Null" ReferencedTable="e30dcb0a-2a63-4f52-82f9-a12b0038d70d" IsReferenceToOwner="true" WithForeignKey="false">
		<Description>Ссылка на родительскую запись в WeTaskActionOptions.</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="c5b2d558-fd1b-0058-4000-05f79179e0aa" Name="TaskOptionRowID" Type="Guid Not Null" ReferencedColumn="e30dcb0a-2a63-0052-3100-012b0038d70d" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="448b8003-9eef-476d-87ac-4ff06abb02e4" Name="TaskGroupOption" Type="Reference(Typified) Not Null" ReferencedTable="dee05376-8267-42b9-8cc9-1ff5bb58bb06" IsReferenceToOwner="true" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="448b8003-9eef-006d-4000-0ff06abb02e4" Name="TaskGroupOptionRowID" Type="Guid Not Null" ReferencedColumn="dee05376-8267-00b9-3100-0ff5bb58bb06" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="7d6ef28b-afc2-00e0-5000-0ace26a88d2a" Name="pk_WeTaskActionCompletionNotifications">
		<SchemeIndexedColumn Column="7d6ef28b-afc2-00e0-3100-0ace26a88d2a" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="7d6ef28b-afc2-00e0-7000-0ace26a88d2a" Name="idx_WeTaskActionCompletionNotifications_ID" IsClustered="true">
		<SchemeIndexedColumn Column="7d6ef28b-afc2-01e0-4000-0ace26a88d2a" />
	</SchemeIndex>
</SchemeTable>