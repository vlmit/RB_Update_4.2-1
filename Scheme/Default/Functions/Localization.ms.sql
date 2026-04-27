CREATE FUNCTION [Localization]
(
	@name NVARCHAR(256),
	@culture NVARCHAR(8)
)
RETURNS @result TABLE
(
	[Value] NVARCHAR(MAX)
)
AS
BEGIN
	DECLARE @fallbackCulture NVARCHAR(8)

	SELECT @fallbackCulture = [FallbackCode]
	FROM [Languages] WITH(NOLOCK)
	WHERE [Code] = @culture

	IF @fallbackCulture IS NULL
		INSERT INTO @result
		SELECT TOP 1 ISNULL([ls].[Value], @name) AS [Value]
		FROM (SELECT 1 AS [Value]) AS [t]
		OUTER APPLY (
			SELECT TOP 1 [le].[ID]
			FROM [LocalizationEntries] AS [le] WITH(NOLOCK)
			WHERE 
				LEFT(@name, 1) = '$'
				AND [le].[Name] = RIGHT(@name, IIF(LEN(@name) > 0, LEN(@name) - 1, 0))
				AND [le].[Overridden] = 0
		) AS [le]
		OUTER APPLY (
			SELECT TOP 1 [ls].[Value]
			FROM [LocalizationStrings] AS [ls] WITH(NOLOCK)
			WHERE 
				[ls].[EntryID] = [le].[ID]
				AND [ls].[Culture] = @culture
		) AS [ls]
	ELSE
		INSERT INTO @result
		SELECT TOP 1 ISNULL([ls].[Value], @name) AS [Value]
		FROM (SELECT 1 AS [Value]) AS [t]
		OUTER APPLY (
			SELECT TOP 1 [le].[ID]
			FROM [LocalizationEntries] AS [le] WITH(NOLOCK)
			WHERE 
				LEFT(@name, 1) = '$'
				AND [le].[Name] = RIGHT(@name, IIF(LEN(@name) > 0, LEN(@name) - 1, 0))
				AND [le].[Overridden] = 0
		) AS [le]
		OUTER APPLY (
			SELECT TOP 1 [r].[Value]
			FROM 
			(
				SELECT [ls].[Value], IIF(@fallbackCulture = [ls].[Culture], 1, 0) AS [Order]
				FROM [LocalizationStrings] AS [ls] WITH(NOLOCK)
				WHERE
					[ls].[Value] IS NOT NULL
					AND [ls].[EntryID] = [le].[ID] 
					AND [ls].[Culture] IN (@culture, @fallbackCulture)
			) AS [r]
			ORDER BY [r].[Order]
		) AS [ls]
	RETURN
END;