CREATE FUNCTION "DateTruncUtc"
(
	time_interval text,
	date_time timestamptz	
)
RETURNS timestamptz
IMMUTABLE STRICT PARALLEL SAFE
AS $$
DECLARE
	result_date timestamptz;
BEGIN
	SELECT date_trunc(time_interval, date_time AT TIME ZONE 'UTC') AT TIME ZONE 'UTC'
	INTO result_date;
	
	RETURN result_date;
END; $$
LANGUAGE PLPGSQL;
