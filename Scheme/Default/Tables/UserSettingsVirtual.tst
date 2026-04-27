<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="3c8a5e77-c4da-45f5-b974-170af387ce26" Name="UserSettingsVirtual" Group="System" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<Description>Таблица с настройками сотрудника, предоставляемыми системой.</Description>
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="3c8a5e77-c4da-00f5-2000-070af387ce26" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3c8a5e77-c4da-01f5-4000-070af387ce26" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="513c2dcc-e677-4781-86b6-fa7412536722" Name="PreferPdfPagingPreview" Type="Boolean Not Null">
		<Description>Признак того, что для встроенного предпросмотра PDF предпочитается использование постраничного просмотра.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="91d58fdf-e49f-4c16-b0d2-99f946132e63" Name="df_UserSettingsVirtual_PreferPdfPagingPreview" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="73d4ccb7-7ceb-4a23-a75b-7ba291060653" Name="TaskColor" Type="Int32 Null">
		<Description>Цвет заданий по умолчанию, которые не подходят по функциональным ролям. NULL, если используется цвет из темы.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="e6f6ce2f-44be-4e2b-8e5c-4de63eb99b04" Name="FrequentlyUsedEmoji" Type="String(2048) Null" />
	<SchemePhysicalColumn ID="dcb85e2a-71f0-4e5f-af69-d7f37d7771f3" Name="CardCompactMode" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="567ddadb-6f18-419d-bb68-5562809f9eb1" Name="df_UserSettingsVirtual_CardCompactMode" Value="true" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="19d8ffe7-1ca8-40c1-a170-9030f8a3b34d" Name="CompactMode" Type="Boolean Not Null">
		<Description>Признак, отвечающий за включение компактного дизайна в ЛК</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="d7649039-23dd-4342-934b-3d0117bd5654" Name="df_UserSettingsVirtual_CompactMode" Value="true" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="dc915626-58af-413d-b08a-bbd68a76e3d4" Name="OfferTransitionToMobileClientEnabled" Type="Boolean Not Null">
		<Description>Флаг, отвечающий за включение показа перехода из ЛК в Mobile Client</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="c8590092-da32-465b-9138-29d26e62b9bf" Name="df_UserSettingsVirtual_OfferTransitionToMobileClientEnabled" Value="true" />
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="2897e073-da40-42d7-96fc-241fc67a64fb" Name="DashboardDisplayMode" Type="Reference(Typified) Not Null" ReferencedTable="aaf62c79-936b-4517-8435-317f61c5407f">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="2897e073-da40-00d7-4000-041fc67a64fb" Name="DashboardDisplayModeID" Type="Int16 Not Null" ReferencedColumn="34ea740b-3ee1-4ad0-b8d7-b4ce42cb6be3" />
		<SchemeReferencingColumn ID="b96168b5-f4bb-4306-bb69-8c0ad312e8a8" Name="DashboardDisplayModeName" Type="String(128) Not Null" ReferencedColumn="26c5b825-2735-44a3-a83f-7f4ef48cb3e1" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="6d471a65-fa2d-42d5-8e39-005c038bc51e" Name="DoNotHideTasksTakenByOthers" Type="Boolean Not Null">
		<Description>Не скрывать задания, взятые в работу другими пользователями</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="c7776f47-844d-40a2-a8b5-d8e467763c88" Name="df_UserSettingsVirtual_DoNotHideTasksTakenByOthers" Value="false" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="24bb42ec-ad34-4a33-b576-f5dbb7d0fceb" Name="TaskInProgressByMeColor" Type="Int32 Null" />
	<SchemePhysicalColumn ID="a5b8c542-ea72-4a91-b0e9-0052604f364c" Name="TaskInProgressByOthersColor" Type="Int32 Null" />
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="3c8a5e77-c4da-00f5-5000-070af387ce26" Name="pk_UserSettingsVirtual" IsClustered="true">
		<SchemeIndexedColumn Column="3c8a5e77-c4da-01f5-4000-070af387ce26" />
	</SchemePrimaryKey>
</SchemeTable>