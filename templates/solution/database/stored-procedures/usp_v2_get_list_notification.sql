USE [__DB_NAME__];
GO

CREATE OR ALTER PROCEDURE dbo.usp_v2_get_list_notification
    @UserId nvarchar(100),
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL,
    @SortColumn nvarchar(100) = N'CreatedDate',
    @SortDirection nvarchar(4) = N'desc',
    @TotalCount int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @page_size int = CASE WHEN @PageSize < 1 THEN 10 WHEN @PageSize > 200 THEN 200 ELSE @PageSize END;
    DECLARE @offset int = (CASE WHEN @PageNumber < 1 THEN 1 ELSE @PageNumber END - 1) * @page_size;
    DECLARE @dir nvarchar(4) = CASE WHEN LOWER(@SortDirection) = N'asc' THEN N'ASC' ELSE N'DESC' END;
    IF @SortColumn NOT IN (N'CreatedDate', N'IsSeen') SET @SortColumn = N'CreatedDate';
    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
    DECLARE @search nvarchar(210) = CASE WHEN @SearchTerm IS NULL THEN NULL ELSE N'%' + @SearchTerm + N'%' END;

    SELECT @TotalCount = COUNT(*)
    FROM dbo.tb_notification n
    LEFT JOIN dbo.rf_notification_type t ON t.id = n.notification_type_id
    WHERE n.receiver_user_id = @UserId
      AND ISNULL(n.is_deleted, 0) = 0
      AND (@SearchTerm IS NULL OR t.short_text LIKE N'%' + @SearchTerm + N'%' OR t.long_text LIKE N'%' + @SearchTerm + N'%');

    DECLARE @sql nvarchar(max) = N'
        SELECT n.id AS Id, n.receiver_user_id AS ReceiverUserId, n.reff_id AS ReffId,
               n.is_seen AS IsSeen, n.created_date AS CreatedDate, n.created_by AS CreatedBy,
               t.short_text AS ShortText, t.long_text AS LongText, t.href AS Href
        FROM dbo.tb_notification n
        LEFT JOIN dbo.rf_notification_type t ON t.id = n.notification_type_id
        WHERE n.receiver_user_id = @userId
          AND ISNULL(n.is_deleted, 0) = 0
          AND (@search IS NULL OR t.short_text LIKE @search OR t.long_text LIKE @search)
        ORDER BY ' + QUOTENAME(@SortColumn) + N' ' + @dir + N'
        OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;';

    EXEC sp_executesql @sql,
        N'@offset int, @page_size int, @search nvarchar(210), @userId nvarchar(100)',
        @offset, @page_size,
        @search = @search,
        @userId = @UserId;
END;
GO
