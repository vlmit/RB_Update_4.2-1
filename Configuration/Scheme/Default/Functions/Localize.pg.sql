CREATE FUNCTION "Localize"
(
	name TEXT,
	culture VARCHAR(8)
)
RETURNS text
STABLE STRICT PARALLEL SAFE
AS $$
DECLARE
	result varchar;
	fallbackCulture VARCHAR(8);
BEGIN
	IF culture IS NULL THEN
		RETURN NULL;
	END IF;

	IF LEFT(name, 1) <> '$' THEN
		RETURN name;
	END IF;

	SELECT "FallbackCode"
	INTO fallbackCulture
	FROM "Languages"
	WHERE "Code" = culture;

	IF fallbackCulture IS NULL THEN
		SELECT "ls"."Value"
		INTO result
		FROM "LocalizationEntries" AS "le"
		INNER JOIN LATERAL (
			SELECT "ls"."Value"
			FROM "LocalizationStrings" AS "ls"
			WHERE 
				"ls"."EntryID" = "le"."ID"
				AND "ls"."Culture" = culture
			LIMIT 1
		) AS "ls" ON TRUE
		WHERE "le"."Name" = RIGHT(name, length(name) - 1)
			AND "le"."Overridden" = false
		LIMIT 1;
	ELSE
		SELECT "ls"."Value"
		INTO result
		FROM "LocalizationEntries" AS "le"
		INNER JOIN LATERAL (
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
		WHERE "le"."Name" = RIGHT(name, length(name) - 1)
			AND "le"."Overridden" = false
		LIMIT 1;
	END IF;

	RETURN COALESCE(result, name);
END; $$
LANGUAGE PLPGSQL;