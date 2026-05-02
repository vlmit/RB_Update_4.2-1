CREATE FUNCTION [GetString]
(
	@name NVARCHAR(256),
	@culture NVARCHAR(8)
)
RETURNS NVARCHAR(MAX)
WITH RETURNS NULL ON NULL INPUT
AS
BEGIN
	DECLARE @result NVARCHAR(MAX)
	DECLARE @fallbackCulture NVARCHAR(8)

	SELECT @fallbackCulture = [FallbackCode] 
	FROM [Languages] WITH (NOLOCK)
	WHERE [Code] = @culture

	IF @fallbackCulture IS NULL 
		SELECT TOP 1 @result = [ls].[Value]
		FROM [LocalizationEntries] AS [le] WITH(NOLOCK)
		CROSS APPLY (
			SELECT TOP 1 [ls].[Value]
			FROM [LocalizationStrings] AS [ls] WITH(NOLOCK)
			WHERE 
				[ls].[EntryID] = [le].[ID]
				AND [ls].[Culture] = @culture
		) AS [ls]
		WHERE 
			[le].[Name] = @name
			AND [le].[Overridden] = 0
	ELSE
		SELECT TOP 1 @result = [ls].[Value]
		FROM [LocalizationEntries] AS [le] WITH(NOLOCK)
		CROSS APPLY (
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
		WHERE 
			[le].[Name] = @name
			AND [le].[Overridden] = 0

		RETURN ISNULL(@result, N'$' + @name)
END;