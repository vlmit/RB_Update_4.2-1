<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="e365a9cd-cc76-4e88-a3c6-1b307e45db99" ID="191e5dcd-69a6-4ac0-8e1e-77a4224aba34" Name="ApprovalProcessTemplateTypes" Group="ApprovalProcess" InstanceType="Cards" ContentType="Collections">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="191e5dcd-69a6-00c0-2000-07a4224aba34" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="191e5dcd-69a6-01c0-4000-07a4224aba34" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="191e5dcd-69a6-00c0-3100-07a4224aba34" Name="RowID" Type="Guid Not Null" />
	<SchemeComplexColumn ID="8bccc2f4-4d97-4681-982c-12bf08f7e90b" Name="Type" Type="Reference(Typified) Not Null" ReferencedTable="b0538ece-8468-4d0b-8b4e-5a1d43e024db" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="8bccc2f4-4d97-0081-4000-02bf08f7e90b" Name="TypeID" Type="Guid Not Null" ReferencedColumn="a628a864-c858-4200-a6b7-da78c8e6e1f4" />
		<SchemeReferencingColumn ID="7c72da2b-5269-46b8-9b66-129b2e934cc0" Name="TypeCaption" Type="String(128) Not Null" ReferencedColumn="0a02451e-2e06-4001-9138-b4805e641afa" />
	</SchemeComplexColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="191e5dcd-69a6-00c0-5000-07a4224aba34" Name="pk_ApprovalProcessTemplateTypes">
		<SchemeIndexedColumn Column="191e5dcd-69a6-00c0-3100-07a4224aba34" />
	</SchemePrimaryKey>
	<SchemeIndex IsSystem="true" IsPermanent="true" IsSealed="true" ID="191e5dcd-69a6-00c0-7000-07a4224aba34" Name="idx_ApprovalProcessTemplateTypes_ID" IsClustered="true">
		<SchemeIndexedColumn Column="191e5dcd-69a6-01c0-4000-07a4224aba34" />
	</SchemeIndex>
</SchemeTable>