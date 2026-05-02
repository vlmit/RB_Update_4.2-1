<?xml version="1.0" encoding="utf-8"?>
<SchemeTable Partition="5c4efc57-f311-4292-9cb1-7be629984589" ID="3e3bd0fb-1faf-4311-b26b-17f01c844fdb" Name="AbResolutions" Group="AbTest" InstanceType="Cards" ContentType="Entries">
	<SchemeComplexColumn IsSystem="true" IsPermanent="true" IsSealed="true" ID="3e3bd0fb-1faf-0011-2000-07f01c844fdb" Name="ID" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3e3bd0fb-1faf-0111-4000-07f01c844fdb" Name="ID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="5aefa5dc-08f8-42ef-9801-b19c42983c3e" Name="Name" Type="String(128) Not Null" />
	<SchemeComplexColumn ID="d17eed41-494c-4b31-88a7-5ccefe081663" Name="Author" Type="Reference(Typified) Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" NormalizationSourceID="77e9c8bb-bb2f-4636-99df-c91a0c07c72c">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d17eed41-494c-0031-4000-0ccefe081663" Name="AuthorID" Type="Guid Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
		<SchemeReferencingColumn ID="4c3f1586-114b-4ce8-8162-5bdb4379404c" Name="AuthorName" Type="String(128) Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="31da5377-a6da-4780-8fb1-104e3d8a5a00" Name="Department" Type="Reference(Typified) Null" ReferencedTable="d43dace1-536f-4c9f-af15-49a8892a7427" NormalizationSourceID="58e79fc4-a1d3-4739-b2c2-44812b44c82a">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="31da5377-a6da-0080-4000-004e3d8a5a00" Name="DepartmentID" Type="Guid Null" ReferencedColumn="d43dace1-536f-019f-4000-09a8892a7427" />
		<SchemePhysicalColumn ID="1f45443e-88c8-4597-a0df-6a3c08ee3be3" Name="DepartmentName" Type="String(Max) Null" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="3e10ea64-d4b1-4fe9-9546-027c5ab0745e" Name="Currency" Type="Reference(Typified) Null" ReferencedTable="3612e150-032f-4a68-bf8e-8e094e5a3a73" NormalizationSourceID="c9f65066-bf62-4693-b612-6b97564deb07">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="3e10ea64-d4b1-00e9-4000-027c5ab0745e" Name="CurrencyID" Type="Guid Null" ReferencedColumn="3612e150-032f-0168-4000-0e094e5a3a73" />
		<SchemeReferencingColumn ID="c68e6fc4-a033-44b1-bdf0-721985366aa7" Name="CurrencyName" Type="String(128) Null" ReferencedColumn="60b11ca9-a5b7-48f7-a5c6-6233d166b19a" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="9a782823-5efb-4b2f-8755-6ec25335d874" Name="Amount" Type="Decimal(18, 2) Null" />
	<SchemeComplexColumn ID="83b0a460-37bc-4582-8bc2-6f7ae6ffbe19" Name="Category" Type="Reference(Typified) Null" ReferencedTable="f939aa52-dc1a-40b2-af4a-cb2757e8390a" NormalizationSourceID="7eef3af0-fd2c-4402-b900-33942eb3d5ef">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="83b0a460-37bc-0082-4000-0f7ae6ffbe19" Name="CategoryID" Type="Guid Null" ReferencedColumn="f939aa52-dc1a-01b2-4000-0b2757e8390a" />
		<SchemeReferencingColumn ID="bd15d4d0-4e86-413d-a878-07019d0063e8" Name="CategoryName" Type="String(128) Null" ReferencedColumn="3dd39fa6-b8bd-4084-8aeb-f129f796f450" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="e2ef1b93-e06d-4d2f-b6cb-664e340a7cc6" Name="State" Type="Reference(Typified) Not Null" ReferencedTable="47107d7a-3a8c-47f0-b800-2a45da222ff4" NormalizationSourceID="a9ed1258-d9e3-49e2-8f5a-54be0a4ae3dd">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e2ef1b93-e06d-002f-4000-064e340a7cc6" Name="StateID" Type="Int16 Not Null" ReferencedColumn="502209b0-233f-4e1f-be01-35a50f53414c">
			<SchemeDefaultConstraint IsPermanent="true" ID="b023d092-132d-4aac-beeb-714c8d3dc10f" Name="df_AbResolutions_StateID" Value="0" />
		</SchemeReferencingColumn>
		<SchemeReferencingColumn ID="e89c4b69-e887-472f-acf1-9feb16c7dee9" Name="StateName" Type="String(128) Not Null" ReferencedColumn="4c1a8dd7-72ed-4fc9-b559-b38ae30dccb9" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="1ddba0f5-5be4-4cac-a405-c4cad9824dad" Name="RefType" Type="Reference(Typified) Null" ReferencedTable="a90baecf-c9ce-4cba-8bb0-150a13666266" WithForeignKey="false" NormalizationSourceID="8f4430ab-0530-42e3-9045-54e3b33bbb72">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="1ddba0f5-5be4-00ac-4000-04cad9824dad" Name="RefTypeID" Type="Guid Null" ReferencedColumn="a90baecf-c9ce-01ba-4000-050a13666266" />
		<SchemeReferencingColumn ID="5787ffec-f5f3-4182-8e9b-7e92db958a68" Name="RefTypeCaption" Type="String(128) Null" ReferencedColumn="447f7cb1-76ae-4703-b3bb-16a57d4e7ab1" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="2e7d6ca0-1e3b-42b0-bca2-11984e9b09e0" Name="Partner" Type="Reference(Typified) Null" ReferencedTable="5d47ef13-b6f4-47ef-9815-3b3d0e6d475a" NormalizationSourceID="f5656402-a3ee-4365-9074-98cf472d7717">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="2e7d6ca0-1e3b-00b0-4000-01984e9b09e0" Name="PartnerID" Type="Guid Null" ReferencedColumn="5d47ef13-b6f4-01ef-4000-0b3d0e6d475a" />
		<SchemeReferencingColumn ID="c90ff955-38d2-41b3-a223-023e916ae1e5" Name="PartnerName" Type="String(255) Null" ReferencedColumn="f1c960e0-951e-4837-8474-bb61d98f40f0" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="a71149d3-86be-48b3-a10b-cf418c3ce562" Name="TaskKind" Type="Reference(Typified) Null" ReferencedTable="856068b1-0e78-4aa8-8e7a-4f53d91a7298" NormalizationSourceID="50ad0f28-2cc9-4c68-b0d4-d3d86e2346d6">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a71149d3-86be-00b3-4000-0f418c3ce562" Name="TaskKindID" Type="Guid Null" ReferencedColumn="856068b1-0e78-01a8-4000-0f53d91a7298" />
		<SchemeReferencingColumn ID="b38f307c-c284-44ee-af6f-89611be3cd0a" Name="TaskKindCaption" Type="String(128) Null" ReferencedColumn="63d9110b-7628-4bf9-9dae-750c3035e48d" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="e48e8a55-87b0-4585-a310-f9535f14c70b" Name="Initiator" Type="Reference(Typified) Null" IsVirtual="true" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e48e8a55-87b0-0085-4000-09535f14c70b" Name="InitiatorID" Type="Guid Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
		<SchemeReferencingColumn ID="1741fc30-afc2-4151-bb2c-2d45674dcab2" Name="InitiatorName" Type="String(128) Null" ReferencedColumn="1782f76a-4743-4aa4-920c-7edaee860964" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="37f801a7-3989-4874-b824-603c4c861350" Name="InitiatorComment" Type="String(Max) Not Null" IsVirtual="true">
		<SchemeDefaultConstraint IsPermanent="true" ID="bcb6bf46-4ad1-4bab-99a2-4d9905a2bb56" Name="df_AbResolutions_InitiatorComment" Value="---" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSystem="true" IsPermanent="true" IsSealed="true" ID="3e3bd0fb-1faf-0011-5000-07f01c844fdb" Name="pk_AbResolutions" IsClustered="true">
		<SchemeIndexedColumn Column="3e3bd0fb-1faf-0111-4000-07f01c844fdb" />
	</SchemePrimaryKey>
	<SchemeIndex ID="74fcabf4-5724-4ebc-b9d6-3d8fa88441b7" Name="ndx_AbResolutions_Name" IsUnique="true">
		<SchemeIndexedColumn Column="5aefa5dc-08f8-42ef-9801-b19c42983c3e" />
	</SchemeIndex>
</SchemeTable>