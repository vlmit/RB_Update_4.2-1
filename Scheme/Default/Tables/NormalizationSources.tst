<?xml version="1.0" encoding="utf-8"?>
<SchemeTable IsSystem="true" IsPermanent="true" ID="82f8a67d-3017-49e7-8bb6-9b8d2ec1fdf9" Name="NormalizationSources" Group="System">
	<Description>Normalization sources</Description>
	<SchemePhysicalColumn IsSealed="true" ID="758c0b69-9a1e-4b88-bc27-9463ab81cddf" Name="ID" Type="Guid Not Null" IsRowGuidColumn="true">
		<Description>Identifier of a normalization source</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn IsSealed="true" ID="2a4ff75a-ece0-4c69-af87-c3482001b08c" Name="Name" Type="String(128) Not Null">
		<Description>Unique name of a normalization source</Description>
	</SchemePhysicalColumn>
	<SchemePhysicalColumn ID="654293a1-b7e8-4f1b-85be-fa27ee7aadce" Name="Description" Type="String(Max) Not Null" IsVirtual="true">
		<SchemeDefaultConstraint IsPermanent="true" ID="3c4907be-f5b4-468b-bba8-d7cce257ffbe" Name="df_NormalizationSources_Description" Value="" />
	</SchemePhysicalColumn>
	<SchemePrimaryKey IsSealed="true" ID="0b3d9217-f30e-4b45-b1d5-4c1527453bf5" Name="pk_NormalizationSources" IsClustered="true">
		<SchemeIndexedColumn Column="758c0b69-9a1e-4b88-bc27-9463ab81cddf" />
	</SchemePrimaryKey>
	<SchemeUniqueKey IsSealed="true" ID="37e51eb0-b250-440a-a797-180c362f7156" Name="ndx_NormalizationSources_Name">
		<SchemeIndexedColumn Column="2a4ff75a-ece0-4c69-af87-c3482001b08c" />
	</SchemeUniqueKey>
	<SchemeRecord>
		<ID ID="758c0b69-9a1e-4b88-bc27-9463ab81cddf">58e79fc4-a1d3-4739-b2c2-44812b44c82a</ID>
		<Name ID="2a4ff75a-ece0-4c69-af87-c3482001b08c">Roles</Name>
		<Description ID="654293a1-b7e8-4f1b-85be-fa27ee7aadce">All roles (including PersonalRole) except for Task (temporary) and NestedRole</Description>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="758c0b69-9a1e-4b88-bc27-9463ab81cddf">35531951-3d12-4a93-b7f7-e9826a6e4875</ID>
		<Name ID="2a4ff75a-ece0-4c69-af87-c3482001b08c">Types</Name>
		<Description ID="654293a1-b7e8-4f1b-85be-fa27ee7aadce">All types from card metadata, including Cards, Dialogs, Files, Tasks</Description>
	</SchemeRecord>
	<SchemeRecord>
		<ID ID="758c0b69-9a1e-4b88-bc27-9463ab81cddf">77e9c8bb-bb2f-4636-99df-c91a0c07c72c</ID>
		<Name ID="2a4ff75a-ece0-4c69-af87-c3482001b08c">Users</Name>
		<Description ID="654293a1-b7e8-4f1b-85be-fa27ee7aadce">PersonalRole cards (i.e. users)</Description>
	</SchemeRecord>
</SchemeTable>