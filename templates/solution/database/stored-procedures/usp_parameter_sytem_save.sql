USE [__DB_NAME__];
GO

CREATE OR ALTER PROCEDURE dbo.usp_parameter_sytem_save
    @tableName nvarchar(100),
    @createdBy nvarchar(100),
    @type nvarchar(20),
    @detailsJson nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @tableName NOT IN (N'rf_default_value', N'rf_email_provider', N'rf_email_template',
                          N'rf_files_upload_type', N'rf_group_access_report_set',
                          N'rf_message', N'tb_parameter_system', N'tb_parameter_system_detail',
                          N'tb_report_set', N'tb_screen_menu')
        THROW 51000, N'Table is not registered as a parameter.', 1;

    IF @type NOT IN (N'A', N'M', N'D')
        THROW 51001, N'Invalid approval type.', 1;

    DECLARE @id uniqueidentifier = NEWID();

    BEGIN TRANSACTION;

    INSERT INTO dbo.tb_parameter_sytem_temporary (id, table_name, type, created_date, created_by)
    VALUES (@id, @tableName, @type, GETDATE(), @createdBy);

    INSERT INTO dbo.tb_parameter_sytem_temporary_detail
        (tb_parameter_sytem_temporary_id, column_name, value, value_before)
    SELECT @id, column_name, value, value_before
    FROM OPENJSON(@detailsJson)
    WITH (
        column_name nvarchar(100) '$.column_name',
        value nvarchar(max) '$.value',
        value_before nvarchar(max) '$.value_before'
    );

    COMMIT TRANSACTION;

    SELECT @id AS TemporaryId;
END;
GO
