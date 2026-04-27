<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="bff8459d-fdff-42e4-b546-13ad6142251a" Name="PdfAnnotations" Group="PdfAnnotations">
	<SchemeComplexColumn ID="edfb41c8-251e-4205-9935-653f6bcf1671" Name="Card" Type="Reference(Typified) Not Null" ReferencedTable="1074eadd-21d7-4925-98c8-40d1e5f0ca0e" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="edfb41c8-251e-0005-4000-053f6bcf1671" Name="CardID" Type="Guid Not Null" ReferencedColumn="9a58123b-b2e9-4137-9c6c-5dab0ec02747" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="b7971d09-882c-472e-b568-1e539d8e772d" Name="File" Type="Reference(Typified) Not Null" ReferencedTable="dd716146-b177-4920-bc90-b1196b16347c" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="b7971d09-882c-002e-4000-0e539d8e772d" Name="FileID" Type="Guid Not Null" ReferencedColumn="dd716146-b177-0020-3100-01196b16347c" />
	</SchemeComplexColumn>
	<SchemeComplexColumn ID="a54153b9-a3c8-46dd-948e-5d1ed480c2d3" Name="FileVersion" Type="Reference(Typified) Not Null" ReferencedTable="e17fd270-5c61-49af-955d-ed6bb983f0d8" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="a54153b9-a3c8-00dd-4000-0d1ed480c2d3" Name="FileVersionRowID" Type="Guid Not Null" ReferencedColumn="e17fd270-5c61-00af-3100-0d6bb983f0d8" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="0698adc5-057e-49ea-a155-731c4bf4d8d7" Name="Annotations" Type="BinaryJson Not Null" />
	<SchemePhysicalColumn ID="ac63b523-8ff5-40d6-919d-1b31a80f96e3" Name="Version" Type="Int32 Not Null" />
	<SchemeComplexColumn ID="d68e934a-d030-4ad3-bb77-fcd3f44c7751" Name="ModifiedBy" Type="Reference(Typified) Not Null" ReferencedTable="6c977939-bbfc-456f-a133-f1c2244e3cc3" WithForeignKey="false">
		<SchemeReferencingColumn IsSystem="true" IsPermanent="true" ID="d68e934a-d030-00d3-4000-0cd3f44c7751" Name="ModifiedByID" Type="Guid Not Null" ReferencedColumn="6c977939-bbfc-016f-4000-01c2244e3cc3" />
	</SchemeComplexColumn>
	<SchemePhysicalColumn ID="53d05652-07a0-48c0-8907-6cb2153afed8" Name="Modified" Type="DateTime Not Null" />
	<SchemePhysicalColumn ID="c3b6f5db-732b-4304-8a08-37f33fd8a5d9" Name="ID" Type="Guid Not Null" />
	<SchemePrimaryKey ID="5916338f-a245-4a95-9bc5-fae3aa9ed9a9" Name="pk_PdfAnnotations">
		<SchemeIndexedColumn Column="c3b6f5db-732b-4304-8a08-37f33fd8a5d9" />
	</SchemePrimaryKey>
	<SchemeUniqueKey ID="1373f018-7e56-40f2-bf30-811298535a01" Name="ndx_PdfAnnotations_FileVersionRowID">
		<SchemeIndexedColumn Column="a54153b9-a3c8-00dd-4000-0d1ed480c2d3" />
	</SchemeUniqueKey>
	<SchemeIndex ID="22bc1785-8d74-4111-80e3-84693440d36b" Name="ndx_PdfAnnotations_CardID">
		<SchemeIndexedColumn Column="edfb41c8-251e-0005-4000-053f6bcf1671" />
	</SchemeIndex>
	<SchemeIndex ID="7b9c4596-9c71-4726-a7f0-9f614cd1b6e9" Name="ndx_PdfAnnotations_FileID">
		<SchemeIndexedColumn Column="b7971d09-882c-002e-4000-0e539d8e772d" />
	</SchemeIndex>
</SchemeTable>