<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="1ee2b87c-6ea7-40be-a1e2-6373d72e88d0" Name="DeletedPdfAnnotations" Group="PdfAnnotations">
	<Description>Информация об удалённых аннотациях для файла PDF</Description>
	<SchemePhysicalColumn ID="1c890c8f-c1d6-4687-a209-eea6e943f153" Name="ID" Type="Guid Not Null">
		<Description>Уникальный идентификатор записи</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="598a3075-d44d-4a64-b946-102b5284ebb7" Name="Card" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e" WithForeignKey="false">
		<Description>Карточка</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="598a3075-d44d-0064-4000-002b5284ebb7" Name="CardID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747">
			<Description>Идентификатор карточки</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="e57a963d-d83e-4628-b535-cd731947de31" Name="File" Type="Reference(Typified) Not Null" ReferencedTable="71fe0eb4-d04d-4141-aa3c-8210209bfb8f" WithForeignKey="false">
		<Description>Удалённый файл</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="e57a963d-d83e-0028-4000-0d731947de31" Name="FileID" Type="Guid Not Null" ReferencedColumn="a67204f3-0709-4186-a101-88932accfbd0">
			<Description>Идентификатор файла</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="71152170-d596-4855-a22e-3b8de46997bb" Name="FileVersion" Type="Reference(Typified) Not Null" ReferencedTable="e17fd270-5c61-49af-955d-ed6bb983f0d8" WithForeignKey="false">
		<Description>Удалённая версия файла</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="71152170-d596-0055-4000-0b8de46997bb" Name="FileVersionRowID" Type="Guid Not Null" ReferencedColumn="e17fd270-5c61-00af-3100-0d6bb983f0d8">
			<Description>Идентификатор версии файла</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="4334406a-ec36-4eac-83c3-5a1d8a10c50a" Name="Annotations" Type="BinaryJson Not Null">
		<Description>Аннотации по файлу</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="3cf95c38-e774-4d91-93e8-3ab7d4cd84a4" Name="Version" Type="Int32 Not Null">
		<Description>Версия аннотаций по файлу</Description>
	</SchemePhysicalColumn>
	<SchemeComplexColumn ID="b94cc560-e078-494b-9c13-d0819dd8d51e" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<Description>Сотрудник, изменивший аннотации</Description>
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b94cc560-e078-004b-4000-00819dd8d51e" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3">
			<Description>Идентификатор сотрудника</Description>
		</SchemeReferencingColumn>
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="2bb40805-599d-472d-80f9-3dad4677d1d9" Name="Modified" Type="DateTime Not Null">
		<Description>Дата и время изменения аннотаций</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="8db6bf78-541f-4445-b1ff-8ba665f08d77" Name="pk_DeletedPdfAnnotations">
		<SchemeIndexedColumn Column="1c890c8f-c1d6-4687-a209-eea6e943f153" />
	</SchemePrimaryKey>
	<SchemeIndex ID="8060ac60-395c-433c-97fd-18f96d06ee99" Name="ndx_DeletedPdfAnnotations_CardID">
		<SchemeIndexedColumn Column="598a3075-d44d-0064-4000-002b5284ebb7" />
	</SchemeIndex>
	<SchemeIndex ID="98180f5a-7b27-4920-9587-fc90342eb3af" Name="ndx_DeletedPdfAnnotations_FileID">
		<SchemeIndexedColumn Column="e57a963d-d83e-0028-4000-0d731947de31" />
	</SchemeIndex>
</SchemeTable>