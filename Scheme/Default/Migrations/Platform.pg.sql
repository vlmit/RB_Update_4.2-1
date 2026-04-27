-- Загружаем pgcrypto для генерации uuid
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Типы карточек для ролевой модели
DO $$
DECLARE
	type_id uuid;
	type_name varchar(128);
	type_caption varchar(128);
	type_group varchar(128);
	type_instance_type_id int;
	type_flags bigint;
	type_metadata jsonb;
BEGIN
	type_id = '929AD23C-8A22-09AA-9000-398BF13979B2';
	type_name = 'PersonalRole';
	type_caption = '$CardTypes_TypesNames_PersonalRole';
	type_group = 'Roles';
	type_instance_type_id = 0;
	type_flags = 3;
	type_metadata = '{"Name":"PersonalRole","Forms":null,"Group":"Roles","Caption":"$CardTypes_TypesNames_PersonalRole","ID::uid":"929ad23c-8a22-09aa-9000-398bf13979b2","Extensions":null,"Flags::int":4099,"Validators":null,"SchemeItems":[{"ColumnIDList":[{"::single_type":"uid"},"b3d43f3c-601f-4536-8c30-a18bb865b1ff","e1207f82-93dc-4a66-822e-cc54bc3c5ecf"],"SectionID::uid":"0958f50b-8fd2-4e65-9531-fd540f3150ab"},{"ColumnIDList":[{"::single_type":"uid"},"a0e909bd-8bed-485c-a8fd-cdae3a26e3f2","196a16fc-8a98-4d80-9d4d-5e533db012d8","d4ae267c-ded8-41b3-9cd5-70f05f0e21c5","e7309100-7651-4064-9a68-fb25fdc86924"],"SectionID::uid":"0f489948-bc16-42a6-8953-b92100807296"},{"ColumnIDList":[{"::single_type":"uid"},"b27bd1a3-53ed-410e-98a3-9909291aefa7"],"SectionID::uid":"122da60d-3efc-42a2-bd84-510c0819807b"},{"ColumnIDList":[{"::single_type":"uid"},"8e5beca9-e479-492e-b4d8-1e3ff242d83d"],"SectionID::uid":"21566803-b822-42b2-ab11-2c20e72a0de4"},{"ColumnIDList":[{"::single_type":"uid"},"26817409-6cc0-43f4-8d59-080698e61a63","ebbfc71e-01d0-43c1-8b34-dfd8e3039590","1e283e65-4325-4f1f-9129-0f87c049962e","e50bb005-a8f5-430c-b436-a5452e970baa"],"SectionID::uid":"3937aa4f-0658-4e8b-a25a-911802f1fa82"},{"ColumnIDList":[{"::single_type":"uid"},"1782f76a-4743-4aa4-920c-7edaee860964","e89b6dc3-7932-4d74-a99f-91b402029536","13f4845d-0125-44cf-9ca3-dac793d881e5","c969e755-cb4a-4414-bd35-d2541e656606","ea756075-b3ab-40df-89fc-a0e196b1c2a1","c05ae347-af13-468e-aeda-7c6b541c06b6","6ca55e0c-c14c-465f-8fdc-f585ae35df09","e88d2f12-ef36-4b7d-b588-673d9cfdd12a","e32de09d-a5dd-4de0-af63-5a9565cda555","ea4fe04c-048e-4655-83f0-9a154da8eb34","4f4e4fe1-c9cf-4070-a4aa-f5a6d4aeca78","808715d0-565a-46f7-8246-af4204c5bd29","17fae6da-858d-4baa-ba6b-5d423b06f81b","188210b1-93b0-4088-b9b7-1c5749c72f66","fd567104-cff3-4c99-a464-5aa02a43660d","fe3406b3-0aba-4d1d-a917-751af37fa17c","668b47c4-f8a7-48d3-a9c7-03b3d0eb9bba","27568b38-af0a-4071-97c6-c217ffb71396","4d1eac5b-1b41-4bff-bbe3-29765f0b9012","c0f21cf2-fee1-4f95-afdb-41442d3c6e34","8089d6be-77fb-4a84-93a4-fc74aefce9d6","954e6a89-3bb4-44f5-a796-03e36cf450a2","4aaece20-d957-4973-8912-2d854bec4c6c","f95f9256-9ba9-41cd-bedf-0786a561e183"],"SectionID::uid":"6c977939-bbfc-456f-a133-f1c2244e3cc3"},{"ColumnIDList":[{"::single_type":"uid"},"77ce216a-1243-4d7a-afc6-74bb65164acb","dee2e9bd-0893-4a3c-82a0-5a1f0560c5fa"],"SectionID::uid":"6d0cfd99-aa36-4992-9231-eea478138fe6"},{"ColumnIDList":[{"::single_type":"uid"},"f2d0c4af-b21e-4290-ae30-a10198b45a41","db20084b-baf8-452b-a5fa-bc95c34b8fe5","af4632ee-a8bb-4718-a080-394c2e61c2e0","c774b850-1bb0-46b6-8891-ec057efda4b8","3ee11741-80b2-41ce-a611-95e730f684d6"],"SectionID::uid":"79dca225-d99c-4dfd-94d9-27ed3ab15046"},{"ColumnIDList":[{"::single_type":"uid"},"4f9c20f4-5bcb-4687-980b-da71b32b79fc"],"SectionID::uid":"81f28cf8-709b-4dde-8c9e-505d3d7870e0"},{"ColumnIDList":[{"::single_type":"uid"},"616d6b2e-35d5-424d-846b-618eb25962d0","54a93a9b-cd03-4852-b6f1-7a76bbe2de35","76725824-fe4f-4fce-9ab2-be0a858967f6","57834e06-f4c2-468d-9957-7b0e6b31dcde","622624d0-7407-4230-af3a-e8ee4e80b551","998f51ac-ffcb-413a-ac2c-a30bff142fe8","1963f578-3da3-4bd4-82d4-c7a02d7e62a5","225e6a6b-2904-4fda-a900-41caedde34b4","c1b7fb89-29b7-4336-84e9-23f651651552","266cf427-b85a-4a1d-80fd-18cd22e21968","988e92e6-5188-4061-b105-44915e0f53f7","363d8307-9f5b-4307-a235-1a6e21d9c6c1","1813800b-d336-44a5-825a-fb637f2b9265","fd05bfeb-303a-4314-b142-edec61da848b","53bb3083-0dbf-4fd4-b317-405d501375da","4cc896b0-b6fa-49a3-b25e-9c536138d5f9","5abec0d3-9fdc-4def-b638-5330fcf98029"],"SectionID::uid":"81f6010b-9641-4aa5-8897-b8e8603fbf4b"},{"ColumnIDList":[{"::single_type":"uid"},"6a8e6bab-e51f-4c17-884b-666fb22ead61","bfd2c3de-3d3c-4b69-ad05-c77a474ea876","a0cbd36b-f4e4-4ccd-9412-0112290e835d","32a7cbe2-5745-413c-b58f-cc3c8f8c8f7c","f1409939-4142-4dee-a35a-93b91b483407","2c50d350-e091-43fb-98db-71a485cda01d","9b6562a1-d679-41c0-8204-485b2968500b","93485556-351e-4d06-879a-0cc3c01ee6cc","f570743f-7e8f-4bdc-b9c5-b1a13e8b6c93","39d1962e-90d4-46fd-9ac7-45b375a78119"],"SectionID::uid":"900bdbcd-1e87-451c-8b4b-082d8f7efd48"},{"ColumnIDList":[{"::single_type":"uid"},"0eb392fb-b8eb-43c6-b685-2623aa63f8ea","d3ca15b7-7d9d-4f7d-adbd-96a4cd79da2d"],"SectionID::uid":"91acf9b9-8476-4dc8-a239-ac6b8f250077"},{"ColumnIDList":[{"::single_type":"uid"},"6b98cc49-b57d-4c00-86cc-9735c0913f0e","992db3b3-e02e-4339-97a8-663ca39e2d63"],"SectionID::uid":"9650456c-27ea-4f62-9073-95b9be1d49ba"},{"ColumnIDList":[{"::single_type":"uid"},"21b99bb1-fe6f-4dd4-8e2e-f55b7065ff63","dda1d970-7904-43bb-b490-3e3c950996b3"],"SectionID::uid":"997561ee-218f-4f22-946b-87a78755c3e6"},{"ColumnIDList":[{"::single_type":"uid"},"330145fc-ae8f-48fb-9257-61009c0d52e1","70e434c0-e236-4997-925c-d5091ff4189d"],"SectionID::uid":"9dc135b0-21b8-4deb-ab65-bdda57a3fbb5"},{"ColumnIDList":[{"::single_type":"uid"},"f9cfc275-c4c0-4f8a-bbe6-d1e1ab56ba64","adbdbd82-9c4f-4265-b061-2dd1ede5b277","1657490d-4041-443d-9e74-1673ea0b68de"],"SectionID::uid":"a3a271db-3ce6-47c7-b75e-87dcc9dc052a"},{"ColumnIDList":[{"::single_type":"uid"},"e18c31f6-4fef-4a1e-bb26-ad336e9e69ed","ae4f8272-282d-4e94-b565-bcfe4167f1c6"],"SectionID::uid":"a8c71408-d1a3-4dbc-abcb-287dd7b7c648"},{"ColumnIDList":[{"::single_type":"uid"},"36b19609-dfe9-4424-ae05-c1e668f4d4b6","ef765f08-078b-4051-8364-f55ff263285f"],"SectionID::uid":"b8f9c863-22fd-4d63-a7cf-b9f0de519b47"},{"ColumnIDList":[{"::single_type":"uid"},"29063885-2127-42c0-86d5-50a94773078c","154d9547-6522-451a-9670-78244ac36c64","fe93e377-859a-4930-aa1e-8c0bfd670d02","5bda4e28-63ef-44ca-9e86-648c92113995","07020625-b544-4132-9ae6-52b8d8a96324","9da3ac60-9b9c-4fd1-a1d3-0c4142477cac"],"SectionID::uid":"c55a7921-d82d-4f8b-b801-f1c693c4c2e3"},{"ColumnIDList":[{"::single_type":"uid"},"310def44-dcf9-4d32-a169-354e72d12a2c","a0dfe997-69e4-4bbe-bd91-ff4f9c50ce88"],"SectionID::uid":"c9bf7542-de37-4fad-9cda-6b1a5a4964b7"},{"ColumnIDList":[{"::single_type":"uid"},"7ad7917f-2a17-4d58-b98b-0476597bb7c8","bad61683-b0fa-4726-8df1-6a2a103f10cd","a2188cb7-17d1-4da0-9a5d-59e589e65a7b","25f5c4c1-1989-4750-9866-74e0c6a4217f"],"SectionID::uid":"dd329f32-adf0-4336-bd9e-fa084c0fe494"},{"ColumnIDList":[{"::single_type":"uid"},"fe22f80e-eea9-463b-a6fb-be3740629a67"],"SectionID::uid":"e631fc2a-7628-4d7e-a118-99d310fa12b8"},{"ColumnIDList":[{"::single_type":"uid"},"a97a0420-e82e-4908-aa18-fa44be513dd7","d2be5468-013f-4fbd-8e00-730a9e29ea57","3ac6b21e-0f15-46c5-a4e2-494c99354399","04496793-f41a-4e0e-ba2b-49ffe8abef50","951e2083-a75d-44ad-b3f5-59f49691e9a1"],"SectionID::uid":"e86b07e5-da20-487b-a55e-0ed56606bddf"},{"ColumnIDList":[{"::single_type":"uid"},"5e8ffd83-3bd5-463a-9bf1-3179ac39719d"],"SectionID::uid":"edbc91b1-dd36-43c2-867a-67c74ed7f403"}],"DigestFormat":null,"MainFormName":null,"CardTypeSections":null,"CompletionOptions":null,"InstanceType::int":0,".formatVersion::int":3}';

	IF NOT EXISTS (SELECT NULL FROM "Types" WHERE "ID" = type_id) THEN
		INSERT INTO "Types" ("ID", "Name", "Caption", "Group", "InstanceTypeID", "Flags", "Metadata")
		VALUES (type_id, type_name, type_caption, type_group, type_instance_type_id, type_flags, type_metadata);
	END IF;
END; $$
LANGUAGE PLPGSQL;

-- Персональная роль "System"
DO $$
DECLARE
	id								uuid;
	type_id							uuid;
	name							varchar(128);
	kind_id							int;
	parent_id						uuid;
	parent_name					varchar(128);
	modified						timestamptz(6);
	modified_by_id					uuid;
	full_name						varchar(256);
	login							varchar(256);
	access_level_id					int;
	access_level_name				varchar(128);
	login_type_id					int;
	login_type_name				varchar(128);
	time_zone_id					smallint;
	time_zone_short_name			varchar(256);
	time_zone_code_name			varchar(20);
	time_zone_utc_offset_minutes	int;
BEGIN
	id = '11111111-1111-1111-1111-111111111111';

	IF NOT EXISTS (SELECT NULL FROM "Instances" WHERE "ID" = id) THEN
		type_id							= '929ad23c-8a22-09aa-9000-398bf13979b2';
		name							= 'System';
		kind_id							= 1;
		parent_id						= NULL;
		parent_name					= NULL;
		modified						= current_timestamp AT TIME ZONE 'UTC';
		modified_by_id					= id;
		full_name						= name;
		login							= NULL;
		access_level_id					= 1;
		access_level_name				= '$Enum_AccessLevels_Administrator';
		login_type_id					= 0;
		login_type_name				= '$Enum_LoginTypes_None';
		time_zone_id					= 0;
		time_zone_short_name			= 'Default';
		time_zone_code_name			= 'UTC+00:00';
		time_zone_utc_offset_minutes	= 0;

		INSERT INTO "Instances" ("ID", "TypeID", "Version", "Created", "CreatedByID", "Modified", "ModifiedByID")
		VALUES (id, type_id, 1, modified, modified_by_id, modified, modified_by_id);

		INSERT INTO "Roles" ("ID", "Name", "TypeID", "ParentID", "ParentName", "Hidden", "TimeZoneID", "TimeZoneShortName", "TimeZoneCodeName", "TimeZoneUtcOffsetMinutes")
		VALUES (id, name, kind_id, parent_id, parent_name, true, time_zone_id, time_zone_short_name, time_zone_code_name, time_zone_utc_offset_minutes);

		INSERT INTO "PersonalRoles" ("ID", "Name", "FirstName", "FullName", "Login", "AccessLevelID", "AccessLevelName", "LoginTypeID", "LoginTypeName")
		VALUES (id, name, name, full_name, login, access_level_id, access_level_name, login_type_id, login_type_name);
		
		INSERT INTO "RoleUsers" ("RowID", "ID", "TypeID", "IsDeputy", "UserID", "UserName")
		VALUES (gen_random_uuid(), id, 1, false, id, name);
	ELSE
		UPDATE "Roles"
		SET "Hidden" = true
		WHERE "ID" = id;
	END IF;
END; $$
LANGUAGE PLPGSQL;

-- Персональная роль "Admin", логин "admin", пароль "admin"
DO $$
DECLARE
	id								uuid;
	type_id							uuid;
	name							varchar(128);
	kind_id							int;
	parent_id						uuid;
	parent_name					varchar(128);
	modified						timestamptz(6);
	modified_by_id					uuid;
	full_name						varchar(256);
	login							varchar(256);
	access_level_id					int;
	access_level_name				varchar(128);
	login_type_id					int;
	login_type_name				varchar(128);
	password_hash					bytea;
	password_key					bytea;
	time_zone_id					smallint;
	time_zone_short_name			varchar(256);
	time_zone_code_name			varchar(20);
	time_zone_utc_offset_minutes	int;
BEGIN
	id = '3db19fa0-228a-497f-873a-0250bf0a4ccb';

	IF NOT EXISTS (SELECT NULL FROM "Instances" WHERE "ID" = id) THEN
		type_id							= '929ad23c-8a22-09aa-9000-398bf13979b2';
		name							= 'Admin';
		kind_id							= 1;
		parent_id						= NULL;
		parent_name					= NULL;
		modified						= current_timestamp AT TIME ZONE 'UTC';
		modified_by_id					= '11111111-1111-1111-1111-111111111111';
		full_name						= 'Admin';
		login							= 'admin';
		access_level_id					= 1;
		access_level_name				= '$Enum_AccessLevels_Administrator';
		login_type_id					= 1;
		login_type_name				= '$Enum_LoginTypes_Internal';
		password_hash					= decode('03F1248837F9EC9FFCAD4A619990EA945DA0217F384A296E627C215C1060560F', 'hex');
		password_key					= decode('770753EC700152EA21DD7DC9E3574979B9D6BAD20003A21BB8EA1A8D2B082FB1D7822C7E133EEAF35D6AED82B9C88F8C0383206B9D72A67464383486E6C4BF19', 'hex');
		time_zone_id					= 0;
		time_zone_short_name			= 'Default';
		time_zone_code_name			= 'UTC+0:00';
		time_zone_utc_offset_minutes	= 0;

		INSERT INTO "Instances" ("ID", "TypeID", "Version", "Created", "CreatedByID", "Modified", "ModifiedByID")
		VALUES (id, type_id, 1, modified, modified_by_id, modified, modified_by_id);

		INSERT INTO "Roles" ("ID", "Name", "TypeID", "ParentID", "ParentName", "Hidden", "TimeZoneID", "TimeZoneShortName", "TimeZoneCodeName", "TimeZoneUtcOffsetMinutes")
		VALUES (id, name, kind_id, parent_id, parent_name, false, time_zone_id, time_zone_short_name, time_zone_code_name, time_zone_utc_offset_minutes);

		INSERT INTO "PersonalRoles" ("ID", "Name", "FirstName", "FullName", "Login", "AccessLevelID", "AccessLevelName", "LoginTypeID", "LoginTypeName", "PasswordHash", "PasswordKey")
		VALUES (id, name, name, full_name, login, access_level_id, access_level_name, login_type_id, login_type_name, password_hash, password_key);

		INSERT INTO "RoleUsers" ("RowID", "ID", "TypeID", "IsDeputy", "UserID", "UserName")
		VALUES (gen_random_uuid(), id, 1, false, id, name);
	END IF;
END; $$
LANGUAGE PLPGSQL;