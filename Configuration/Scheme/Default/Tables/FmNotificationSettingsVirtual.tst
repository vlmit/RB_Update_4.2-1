<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="353a4eb9-e9ba-4098-9d5a-4df92df6604b" Name="FmNotificationSettingsVirtual" Group="Fm" IsVirtual="true" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="353a4eb9-e9ba-0098-2000-0df92df6604b" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="353a4eb9-e9ba-0198-4000-0df92df6604b" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="8e46719f-4d86-4051-a682-b63b524916eb" Name="SubscribeToAllTopics" Type="Boolean Not Null">
		<SchemeDefaultConstraint IsPermanent="true" ID="43613410-5908-4817-bf56-d819b748b4e8" Name="df_FmNotificationSettingsVirtual_SubscribeToAllTopics" Value="false" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="353a4eb9-e9ba-0098-5000-0df92df6604b" Name="pk_FmNotificationSettingsVirtual" IsClustered="true">
		<SchemeIndexedColumn Column="353a4eb9-e9ba-0198-4000-0df92df6604b" />
	</SchemePrimaryKey>
</SchemeTable>