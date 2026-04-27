CREATE FUNCTION "Localization"
(
	name TEXT,
	culture VARCHAR(8)
)
RETURNS TABLE
(
	"Value" TEXT
)
STABLE PARALLEL SAFE
AS $$
DECLARE
	fallbackCulture VARCHAR(8);
BEGIN
	SELECT "FallbackCode"
	INTO fallbackCulture
	FROM "Languages"
	WHERE "Code" = culture;

	IF fallbackCulture IS NULL THEN
		RETURN QUERY
		SELECT COALESCE("ls"."Value", name) AS "Value"
		FROM (SELECT NULL AS "Temp") AS "t"
		LEFT JOIN LATERAL (
			SELECT "le"."ID"
			FROM "LocalizationEntries" AS "le"
			WHERE 
				LEFT(name, 1) = '$'
				AND "le"."Name" = RIGHT(name, CASE WHEN LENGTH(name) > 0 THEN LENGTH(name) - 1 ELSE 0 END)
				AND "le"."Overridden" = FALSE
			LIMIT 1
		) AS "le" ON TRUE
		LEFT JOIN LATERAL (
			SELECT "ls"."Value"
			FROM "LocalizationStrings" AS "ls"
			WHERE 
				"ls"."EntryID" = "le"."ID"
				AND "ls"."Culture" = culture
			LIMIT 1
		) AS "ls" ON TRUE
		LIMIT 1;
	ELSE
		RETURN QUERY
		SELECT COALESCE("ls"."Value", name) AS "Value"
		FROM (SELECT NULL AS "Temp") AS "t"
		LEFT JOIN LATERAL (
			SELECT "le"."ID"
			FROM "LocalizationEntries" AS "le"
			WHERE 
				LEFT(name, 1) = '$'
				AND "le"."Name" = RIGHT(name, CASE WHEN LENGTH(name) > 0 THEN LENGTH(name) - 1 ELSE 0 END)
				AND "le"."Overridden" = FALSE
			LIMIT 1
		) AS "le" ON TRUE
		LEFT JOIN LATERAL (
			SELECT "r"."Value"
			FROM 
			(
				SELECT "ls"."Value", CASE WHEN fallbackCulture = "ls"."Culture" THEN 1 ELSE 0 END AS "Order"
				FROM "LocalizationStrings" AS "ls"
				WHERE
					"ls"."Value" IS NOT NULL
					AND "ls"."EntryID" = "le"."ID" 
					AND "ls"."Culture" IN (culture, fallbackCulture)
			) AS "r"
			ORDER BY "r"."Order"
			LIMIT 1
		) AS "ls" ON TRUE
		LIMIT 1;
	END IF;
END; $$
LANGUAGE PLPGSQL;