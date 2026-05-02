<?xml version="1.0" encoding="utf-8"?>
<SchemeTable ID="e23f6523-6eb3-4ad1-b377-03fbcee70b5a" Name="AvatarCache" Group="System">
	<Description>Stores cached avatar images.</Description>
	<SchemePhysicalColumn ID="66fd939b-5008-4a4b-824d-e80d7a1eee43" Name="ID" Type="Guid Not Null">
		<Description>Unique identifier of the avatar entity.</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="82290487-03a0-4c17-81af-fc5d3f616849" Name="KindID" Type="Int32 Not Null">
		<Description>Kind identifier for the avatar:
0 - avatar (default),
1 - photo.</Description>
		<SchemeDefaultConstraint IsPermanent="true" ID="89f2b719-7db8-4d65-b043-282942deaa56" Name="df_AvatarCache_KindID" Value="0" />
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c91ed542-fab4-447b-a807-a58c898a1653" Name="Content" Type="Binary(Max) Not Null">
		<Description>Binary content of the cached avatar image</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="c7bb7f0d-24b0-4933-99e2-f7f08efdfd69" Name="ContentType" Type="String(128) Not Null">
		<Description>MIME type of the image content (e.g., image/png, image/jpeg).</Description>
	</SchemePhysicalColumn>
	<SchemePrimaryKey ID="f9173bb6-4780-44b1-b959-92786438e355" Name="pk_AvatarCache" IsClustered="true">
		<SchemeIndexedColumn Column="66fd939b-5008-4a4b-824d-e80d7a1eee43" />
		<SchemeIndexedColumn Column="82290487-03a0-4c17-81af-fc5d3f616849" />
	</SchemePrimaryKey>
</SchemeTable>