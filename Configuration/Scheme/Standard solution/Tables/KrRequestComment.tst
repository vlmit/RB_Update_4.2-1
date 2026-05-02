<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="d1b372f3-7565-4309-9037-5e5a0969d94e" ID="db361bb6-d8d1-4645-8d9c-f296ce939c4b" Name="KrRequestComment" Group="Kr" InstanceType="Tasks" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="db361bb6-d8d1-0045-2000-0296ce939c4b" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="5bfa9936-bb5a-4e8f-89a9-180bfd8f75f8">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="db361bb6-d8d1-0145-4000-0296ce939c4b" Name="ID" Type="Guid Not Null" ReferencedColumn="5bfa9936-bb5a-008f-3100-080bfd8f75f8" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="63f422ed-d643-4341-9787-e9ce07d180fb" Name="Comment" Type="String(Max) Null">
		<Description>Ответ комментирующего на вопрос согласанта</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="2609f469-f0c3-4994-9659-51e8d5774c8e" Name="AuthorComment" Type="String(Max) Null">
		<Description>Комментарий автора или вопрос</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="db361bb6-d8d1-0045-5000-0296ce939c4b" Name="pk_KrRequestComment" IsClustered="true">
		<SchemeIndexedColumn Column="db361bb6-d8d1-0145-4000-0296ce939c4b" />
	</SchemePrimaryKey>
</SchemeTable>