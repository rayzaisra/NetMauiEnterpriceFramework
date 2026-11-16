USE [__DB_NAME__];
GO

CREATE OR ALTER PROCEDURE dbo.usp_parameter_sytem_approval
    @temporaryId uniqueidentifier,
    @type nvarchar(20),
    @approvalId nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @tableName nvarchar(100), @tempType nvarchar(20), @createdBy nvarchar(100);
    SELECT @tableName = table_name, @tempType = type, @createdBy = created_by
    FROM dbo.tb_parameter_sytem_temporary
    WHERE id = @temporaryId;

    IF @tableName IS NULL
        THROW 51010, N'Pending request not found.', 1;

    IF @tableName NOT IN (N'rf_default_value', N'rf_email_provider', N'rf_email_template',
                          N'rf_files_upload_type', N'rf_group_access_report_set',
                          N'rf_message', N'tb_parameter_system', N'tb_parameter_system_detail',
                          N'tb_report_set', N'tb_screen_menu')
        THROW 51011, N'Table is not registered as a parameter.', 1;

    IF LOWER(@type) IN (N'reject', N'rejected')
    BEGIN
        BEGIN TRANSACTION;
        DELETE FROM dbo.tb_parameter_sytem_temporary_detail WHERE tb_parameter_sytem_temporary_id = @temporaryId;
        DELETE FROM dbo.tb_parameter_sytem_temporary WHERE id = @temporaryId;
        INSERT INTO [__AUDIT_DB_NAME__].[dbo].[tb_log] (entity, log_type, log_msg, created_date, created_by)
        VALUES (N'ParameterApproval', N'REJECT', CONCAT(@tableName, N' rejected'), GETDATE(), @approvalId);
        COMMIT TRANSACTION;
        RETURN;
    END;

    DECLARE @keys TABLE (column_name nvarchar(100));
    INSERT INTO @keys (column_name)
    SELECT field_name FROM dbo.tb_parameter_system_detail
    WHERE table_name = @tableName AND field_key = 1;

    IF NOT EXISTS (SELECT 1 FROM @keys) AND COL_LENGTH(@tableName, N'id') IS NOT NULL
        INSERT INTO @keys (column_name) VALUES (N'id');

    IF NOT EXISTS (SELECT 1 FROM @keys)
        THROW 51012, N'Key metadata is missing for this parameter.', 1;

    DECLARE @details TABLE (column_name nvarchar(100), value nvarchar(max));
    INSERT INTO @details (column_name, value)
    SELECT column_name, value FROM dbo.tb_parameter_sytem_temporary_detail
    WHERE tb_parameter_sytem_temporary_id = @temporaryId AND column_name <> N'__key';

    UPDATE @details
    SET value = CASE WHEN LOWER(value) = N'true' THEN N'1' WHEN LOWER(value) = N'false' THEN N'0' ELSE value END
    WHERE value IS NOT NULL;

    DECLARE @where nvarchar(max) = N'';
    SELECT @where = @where + CASE WHEN LEN(@where) > 0 THEN N' AND ' ELSE N'' END
        + QUOTENAME(d.column_name) + N' = '
        + CASE WHEN d.value IS NULL THEN N'NULL' ELSE N'N''' + REPLACE(d.value, N'''', N'''''') + N'''' END
    FROM @details d
    JOIN @keys k ON k.column_name = d.column_name;

    IF @where = N''
        THROW 51013, N'Key payload is missing.', 1;

    IF @tempType = N'D'
    BEGIN
        DECLARE @deleteSql nvarchar(max);
        IF COL_LENGTH(@tableName, N'is_deleted') IS NOT NULL
        BEGIN
            SET @deleteSql = N'UPDATE ' + QUOTENAME(@tableName) + N' SET [is_deleted] = 1';
            IF COL_LENGTH(@tableName, N'is_active') IS NOT NULL SET @deleteSql += N', [is_active] = 0';
            IF COL_LENGTH(@tableName, N'status_approval') IS NOT NULL SET @deleteSql += N', [status_approval] = 4';
            IF COL_LENGTH(@tableName, N'approved_by') IS NOT NULL SET @deleteSql += N', [approved_by] = N''' + REPLACE(@approvalId, N'''', N'''''') + N'''';
            IF COL_LENGTH(@tableName, N'approved_date') IS NOT NULL SET @deleteSql += N', [approved_date] = GETDATE()';
            IF COL_LENGTH(@tableName, N'deleted_by') IS NOT NULL SET @deleteSql += N', [deleted_by] = N''' + REPLACE(@createdBy, N'''', N'''''') + N'''';
            IF COL_LENGTH(@tableName, N'deleted_date') IS NOT NULL SET @deleteSql += N', [deleted_date] = GETDATE()';
            SET @deleteSql += N' WHERE ' + @where;
        END
        ELSE
        BEGIN
            SET @deleteSql = N'DELETE FROM ' + QUOTENAME(@tableName) + N' WHERE ' + @where;
        END

        BEGIN TRANSACTION;
        EXEC sp_executesql @deleteSql;
        DELETE FROM dbo.tb_parameter_sytem_temporary_detail WHERE tb_parameter_sytem_temporary_id = @temporaryId;
        DELETE FROM dbo.tb_parameter_sytem_temporary WHERE id = @temporaryId;
        INSERT INTO [__AUDIT_DB_NAME__].[dbo].[tb_log] (entity, log_type, log_msg, created_date, created_by)
        VALUES (N'ParameterApproval', N'APPROVE', CONCAT(@tableName, N' deleted'), GETDATE(), @approvalId);
        COMMIT TRANSACTION;
        RETURN;
    END;

    DECLARE @cols nvarchar(max), @vals nvarchar(max), @sets nvarchar(max);
    SELECT @cols = STRING_AGG(QUOTENAME(d.column_name), N', ') WITHIN GROUP (ORDER BY d.column_name),
           @vals = STRING_AGG(CASE WHEN d.value IS NULL THEN N'NULL' ELSE N'N''' + REPLACE(d.value, N'''', N'''''') + N'''' END, N', ')
                     WITHIN GROUP (ORDER BY d.column_name)
    FROM @details d
    WHERE COL_LENGTH(@tableName, d.column_name) IS NOT NULL;

    SELECT @sets = STRING_AGG(QUOTENAME(d.column_name) + N' = ' +
                     CASE WHEN d.value IS NULL THEN N'NULL' ELSE N'N''' + REPLACE(d.value, N'''', N'''''') + N'''' END, N', ')
                 WITHIN GROUP (ORDER BY d.column_name)
    FROM @details d
    WHERE COL_LENGTH(@tableName, d.column_name) IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM @keys k WHERE k.column_name = d.column_name);

    IF COL_LENGTH(@tableName, N'status_approval') IS NOT NULL AND CHARINDEX(N'[status_approval]', ISNULL(@cols, N'')) = 0
        SELECT @cols = CONCAT(@cols, CASE WHEN @cols IS NULL THEN N'' ELSE N', ' END, N'[status_approval]'),
               @vals = CONCAT(@vals, CASE WHEN @vals IS NULL THEN N'' ELSE N', ' END, N'4'),
               @sets = CONCAT(@sets, CASE WHEN @sets IS NULL THEN N'' ELSE N', ' END, N'[status_approval] = 4');

    IF COL_LENGTH(@tableName, N'approved_by') IS NOT NULL AND CHARINDEX(N'[approved_by]', ISNULL(@cols, N'')) = 0
        SELECT @cols = CONCAT(@cols, CASE WHEN @cols IS NULL THEN N'' ELSE N', ' END, N'[approved_by]'),
               @vals = CONCAT(@vals, CASE WHEN @vals IS NULL THEN N'' ELSE N', ' END, N'N''' + REPLACE(@approvalId, N'''', N'''''') + N''''),
               @sets = CONCAT(@sets, CASE WHEN @sets IS NULL THEN N'' ELSE N', ' END, N'[approved_by] = N''' + REPLACE(@approvalId, N'''', N'''''') + N'''');

    IF COL_LENGTH(@tableName, N'approved_date') IS NOT NULL AND CHARINDEX(N'[approved_date]', ISNULL(@cols, N'')) = 0
        SELECT @cols = CONCAT(@cols, CASE WHEN @cols IS NULL THEN N'' ELSE N', ' END, N'[approved_date]'),
               @vals = CONCAT(@vals, CASE WHEN @vals IS NULL THEN N'' ELSE N', ' END, N'GETDATE()'),
               @sets = CONCAT(@sets, CASE WHEN @sets IS NULL THEN N'' ELSE N', ' END, N'[approved_date] = GETDATE()');

    IF @tempType = N'A'
    BEGIN
        IF COL_LENGTH(@tableName, N'created_by') IS NOT NULL AND CHARINDEX(N'[created_by]', ISNULL(@cols, N'')) = 0
            SELECT @cols = CONCAT(@cols, CASE WHEN @cols IS NULL THEN N'' ELSE N', ' END, N'[created_by]'),
                   @vals = CONCAT(@vals, CASE WHEN @vals IS NULL THEN N'' ELSE N', ' END, N'N''' + REPLACE(@createdBy, N'''', N'''''') + N'''');

        IF COL_LENGTH(@tableName, N'created_date') IS NOT NULL AND CHARINDEX(N'[created_date]', ISNULL(@cols, N'')) = 0
            SELECT @cols = CONCAT(@cols, CASE WHEN @cols IS NULL THEN N'' ELSE N', ' END, N'[created_date]'),
                   @vals = CONCAT(@vals, CASE WHEN @vals IS NULL THEN N'' ELSE N', ' END, N'GETDATE()');

        IF COL_LENGTH(@tableName, N'is_active') IS NOT NULL AND CHARINDEX(N'[is_active]', ISNULL(@cols, N'')) = 0
            SELECT @cols = CONCAT(@cols, CASE WHEN @cols IS NULL THEN N'' ELSE N', ' END, N'[is_active]'),
                   @vals = CONCAT(@vals, CASE WHEN @vals IS NULL THEN N'' ELSE N', ' END, N'1');

        IF COL_LENGTH(@tableName, N'is_deleted') IS NOT NULL AND CHARINDEX(N'[is_deleted]', ISNULL(@cols, N'')) = 0
            SELECT @cols = CONCAT(@cols, CASE WHEN @cols IS NULL THEN N'' ELSE N', ' END, N'[is_deleted]'),
                   @vals = CONCAT(@vals, CASE WHEN @vals IS NULL THEN N'' ELSE N', ' END, N'0');

        IF @cols IS NULL
            THROW 51014, N'Parameter payload is empty.', 1;

        DECLARE @insertSql nvarchar(max) =
            N'INSERT INTO ' + QUOTENAME(@tableName) + N' (' + @cols + N') VALUES (' + @vals + N')';
        BEGIN TRANSACTION;
        EXEC sp_executesql @insertSql;
    END
    ELSE
    BEGIN
        IF COL_LENGTH(@tableName, N'modified_by') IS NOT NULL AND CHARINDEX(N'[modified_by]', ISNULL(@sets, N'')) = 0
            SET @sets = CONCAT(@sets, CASE WHEN @sets IS NULL THEN N'' ELSE N', ' END, N'[modified_by] = N''' + REPLACE(@approvalId, N'''', N'''''') + N'''');

        IF COL_LENGTH(@tableName, N'modified_date') IS NOT NULL AND CHARINDEX(N'[modified_date]', ISNULL(@sets, N'')) = 0
            SET @sets = CONCAT(@sets, CASE WHEN @sets IS NULL THEN N'' ELSE N', ' END, N'[modified_date] = GETDATE()');

        IF @sets IS NULL
            THROW 51015, N'Parameter update payload is empty.', 1;

        DECLARE @updateSql nvarchar(max) =
            N'UPDATE ' + QUOTENAME(@tableName) + N' SET ' + @sets + N' WHERE ' + @where;
        BEGIN TRANSACTION;
        EXEC sp_executesql @updateSql;
    END;

    DELETE FROM dbo.tb_parameter_sytem_temporary_detail WHERE tb_parameter_sytem_temporary_id = @temporaryId;
    DELETE FROM dbo.tb_parameter_sytem_temporary WHERE id = @temporaryId;

    INSERT INTO [__AUDIT_DB_NAME__].[dbo].[tb_log] (entity, log_type, log_msg, created_date, created_by)
    VALUES (N'ParameterApproval', N'APPROVE', CONCAT(@tableName, N' approved'), GETDATE(), @approvalId);

    COMMIT TRANSACTION;
END;
GO
