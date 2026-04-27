<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="dd8eeaba-9042-4fb5-9e8e-f7544463464f" ID="318965a6-fcec-432d-8ba3-3b972fb2b750" Name="WeTimerAction" Group="WorkflowEngine" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<Description>Секция для действия Таймер</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="318965a6-fcec-002d-2000-0b972fb2b750" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="318965a6-fcec-012d-4000-0b972fb2b750" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="9d40c2a2-5de1-49b6-b695-8f10c22b4aab" Name="RunOnce" Type="Boolean Not Null">
		<Description>Флаг определяет, нужно ли запускать таймер один раз или несколько</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="37254b5a-4a4c-4e43-93c0-33752e2083b2" Name="df_WeTimerAction_RunOnce" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c8e94cc4-efb0-4b74-b2e0-4d8ca4ea1246" Name="Period" Type="Int32 Null">
		<Description>Период запуска таймера в секундах</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="6f4653a5-1a45-429b-aad6-9c8f8524ff22" Name="Cron" Type="String(Max) Null">
		<Description>Cron-выражение для периода запуска таймера</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="06e4142c-a3d8-44fa-9900-159ac8e2199c" Name="Date" Type="DateTime Null">
		<Description>Конкретная дата запуска таймера</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="dfeb0d7c-4d8c-4146-b748-e82a4f0baf3e" Name="StopCondition" Type="String(Max) Null">
		<Description>Условие остановки плагина</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="d60c4f38-bc54-475d-b6db-c19e18a8ed8d" Name="LockProcess" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="74b452e0-4dba-4375-a278-057465df3e77" Name="df_WeTimerAction_LockProcess" Value="true" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="aa063541-63a4-45ed-9326-8a9a8803f128" Name="RetryAllowed" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="24a96aa9-a59b-421d-bab1-469b5a588628" Name="df_WeTimerAction_RetryAllowed" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="339b3932-4ba7-43b6-9c87-0f3ded343cae" Name="SyncProcessing" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="55822b7b-8280-44de-a94b-2b95a3eebc27" Name="df_WeTimerAction_SyncProcessing" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="a2fa5373-c9e9-41fe-a48f-9eecd5270992" Name="SyncObject" Type="String(Max) Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="318965a6-fcec-002d-5000-0b972fb2b750" Name="pk_WeTimerAction" IsClustered="true">
		<SchemeIndexedColumn Column="318965a6-fcec-012d-4000-0b972fb2b750" />
	</SchemePrimaryKey>
</SchemeTable>