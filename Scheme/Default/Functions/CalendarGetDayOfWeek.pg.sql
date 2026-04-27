CREATE FUNCTION "CalendarGetDayOfWeek"
(
	date_time timestamptz
)
RETURNS int
IMMUTABLE STRICT PARALLEL SAFE
AS $$
	SELECT extract(isodow FROM date_time)::int;
$$
LANGUAGE SQL;