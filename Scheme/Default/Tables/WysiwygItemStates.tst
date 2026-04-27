<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="c343ffd3-b299-4535-a335-c310aebde83e" Name="WysiwygItemStates" Group="WysiwygEditor">
	<SchemePhysicalColumn ID="ae0badc5-98e3-4a4d-a266-8f85754d280e" Name="ID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="0c2ee3fb-41f6-40b3-a89e-d57dc30fc931" Name="RowID" Type="Guid Not Null" />
	<SchemePhysicalColumn ID="ee1a9d2c-3ed5-43a5-9739-964f841fb2c9" Name="Content" Type="Json Not Null" />
	<SchemePhysicalColumn ID="4345b2b4-08d6-4b0c-9817-2893739caee5" Name="ItemID" Type="Guid Not Null" />
	<SchemePrimaryKey ID="bd35e0b2-39b2-469f-af30-6d895ddb0a2e" Name="pk_WysiwygItemStates">
		<SchemeIndexedColumn Column="0c2ee3fb-41f6-40b3-a89e-d57dc30fc931" />
	</SchemePrimaryKey>
</SchemeTable>