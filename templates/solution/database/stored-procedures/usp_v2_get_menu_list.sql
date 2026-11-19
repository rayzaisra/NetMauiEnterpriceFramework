USE [__DB_NAME__];
GO

CREATE OR ALTER PROCEDURE dbo.usp_v2_get_menu_list
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL,
    @SortColumn nvarchar(100) = N'Id',
    @SortDirection nvarchar(4) = N'asc',
    @TotalCount int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @page_size int = CASE WHEN @PageSize < 1 THEN 10 WHEN @PageSize > 200 THEN 200 ELSE @PageSize END;
    DECLARE @offset int = (CASE WHEN @PageNumber < 1 THEN 1 ELSE @PageNumber END - 1) * @page_size;
    DECLARE @dir nvarchar(4) = CASE WHEN LOWER(@SortDirection) = N'desc' THEN N'DESC' ELSE N'ASC' END;
    IF @SortColumn NOT IN (N'Id', N'MenuUnique', N'MenuName', N'ParentLevelId', N'OrderSeq', N'IsActive') SET @SortColumn = N'Id';
    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
    DECLARE @search nvarchar(210) = CASE WHEN @SearchTerm IS NULL THEN NULL ELSE N'%' + @SearchTerm + N'%' END;

    SELECT @TotalCount = COUNT(*)
    FROM dbo.um_menu m
    WHERE m.is_deleted = 0
      AND (@SearchTerm IS NULL OR m.menu_unique LIKE N'%' + @SearchTerm + N'%' OR m.menu_name LIKE N'%' + @SearchTerm + N'%' OR m.menu_url LIKE N'%' + @SearchTerm + N'%');

    DECLARE @sql nvarchar(max) = N'
        SELECT m.id AS Id, m.menu_unique AS MenuUnique, m.menu_name AS MenuName, m.menu_url AS MenuUrl,
               m.parent_level_id AS ParentLevelId, m.menu_unique_parent AS MenuUniqueParent,
               p.menu_name AS ParentName, m.menu_icon AS MenuIcon, i.menu_icon_name AS MenuIconName,
               m.order_seq AS OrderSeq, m.component_type AS ComponentType, m.is_active AS IsActive, m.status_approval AS StatusApproval
        FROM dbo.um_menu m
        LEFT JOIN dbo.um_menu p ON p.menu_unique = m.menu_unique_parent
        LEFT JOIN dbo.enum_menu_icon i ON i.menu_icon_code = m.menu_icon
        WHERE m.is_deleted = 0
          AND (@search IS NULL OR m.menu_unique LIKE @search OR m.menu_name LIKE @search OR m.menu_url LIKE @search)
        ORDER BY ' + QUOTENAME(@SortColumn) + N' ' + @dir + N'
        OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;';

    EXEC sp_executesql @sql,
        N'@offset int, @page_size int, @search nvarchar(210)',
        @offset, @page_size, @search = @search;
END;
GO
