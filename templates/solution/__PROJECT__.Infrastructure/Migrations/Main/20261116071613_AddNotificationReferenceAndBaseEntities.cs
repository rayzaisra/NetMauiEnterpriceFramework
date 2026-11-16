using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace __PROJECT__.Infrastructure.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddNotificationReferenceAndBaseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[FK_tb_notification_tb_notification_type_notification_type_id]', N'F') IS NOT NULL
                    ALTER TABLE [dbo].[tb_notification] DROP CONSTRAINT [FK_tb_notification_tb_notification_type_notification_type_id];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tb_notification_notification_type_id' AND object_id = OBJECT_ID(N'[dbo].[tb_notification]'))
                    DROP INDEX [IX_tb_notification_notification_type_id] ON [dbo].[tb_notification];

                EXEC sp_rename N'[dbo].[tb_notification_type]', N'rf_notification_type', N'OBJECT';

                DECLARE @pk sysname;
                SELECT @pk = kc.name
                FROM sys.key_constraints kc
                INNER JOIN sys.tables t ON t.object_id = kc.parent_object_id
                WHERE t.name = N'rf_notification_type' AND kc.type = N'PK';

                IF @pk IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[rf_notification_type] DROP CONSTRAINT [' + @pk + N']');

                ALTER TABLE [dbo].[rf_notification_type] ADD [id_new] varchar(50) NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [dbo].[rf_notification_type]
                SET [id_new] = UPPER(CONVERT(varchar(50), [id]));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[rf_notification_type] ALTER COLUMN [id_new] varchar(50) NOT NULL;
                ALTER TABLE [dbo].[rf_notification_type] DROP COLUMN [id];
                EXEC sp_rename N'[dbo].[rf_notification_type].[id_new]', N'id', N'COLUMN';
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[rf_notification_type] ADD CONSTRAINT [PK_rf_notification_type] PRIMARY KEY CLUSTERED ([id]);

                ALTER TABLE [dbo].[rf_notification_type] ADD
                    [is_active] bit NULL,
                    [is_deleted] bit NULL,
                    [modified_by] nvarchar(100) NULL,
                    [modified_date] datetime2 NULL,
                    [deleted_by] nvarchar(100) NULL,
                    [deleted_date] datetime2 NULL,
                    [status_approval] int NULL,
                    [is_locked] bit NULL,
                    [is_default] bit NULL,
                    [approved_by] nvarchar(100) NULL,
                    [approved_date] datetime2 NULL,
                    [approval_type_id] nvarchar(2) NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [dbo].[rf_notification_type]
                SET [is_active] = 1,
                    [is_deleted] = 0,
                    [status_approval] = 4,
                    [is_locked] = 0,
                    [is_default] = 0,
                    [approved_by] = ISNULL([created_by], N'system'),
                    [approved_date] = ISNULL([created_date], GETDATE()),
                    [approval_type_id] = N'A'
                WHERE [is_active] IS NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[tb_notification] ADD [notification_type_id_new] varchar(50) NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [dbo].[tb_notification]
                SET [notification_type_id_new] = CASE
                    WHEN [notification_type_id] IS NULL THEN NULL
                    ELSE UPPER(CONVERT(varchar(50), [notification_type_id]))
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[tb_notification] DROP COLUMN [notification_type_id];
                EXEC sp_rename N'[dbo].[tb_notification].[notification_type_id_new]', N'notification_type_id', N'COLUMN';

                ALTER TABLE [dbo].[tb_notification] ADD
                    [is_active] bit NULL,
                    [is_deleted] bit NULL,
                    [modified_by] nvarchar(100) NULL,
                    [modified_date] datetime2 NULL,
                    [deleted_by] nvarchar(100) NULL,
                    [deleted_date] datetime2 NULL,
                    [status_approval] int NULL,
                    [is_locked] bit NULL,
                    [is_default] bit NULL,
                    [approved_by] nvarchar(100) NULL,
                    [approved_date] datetime2 NULL,
                    [approval_type_id] nvarchar(2) NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [dbo].[tb_notification]
                SET [is_active] = 1,
                    [is_deleted] = 0,
                    [status_approval] = 4,
                    [is_locked] = 0,
                    [is_default] = 0,
                    [approved_by] = ISNULL([created_by], N'system'),
                    [approved_date] = ISNULL([created_date], GETDATE()),
                    [approval_type_id] = N'A'
                WHERE [is_active] IS NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[tb_notification]
                    ADD CONSTRAINT [FK_tb_notification_rf_notification_type_notification_type_id]
                    FOREIGN KEY ([notification_type_id]) REFERENCES [dbo].[rf_notification_type]([id])
                    ON DELETE SET NULL;

                CREATE INDEX [IX_tb_notification_receiver_user_id_created_date]
                    ON [dbo].[tb_notification] ([receiver_user_id], [created_date]);
                CREATE INDEX [IX_tb_notification_receiver_user_id_is_seen_created_date]
                    ON [dbo].[tb_notification] ([receiver_user_id], [is_seen], [created_date]);
                CREATE INDEX [IX_tb_notification_notification_type_id]
                    ON [dbo].[tb_notification] ([notification_type_id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[FK_tb_notification_rf_notification_type_notification_type_id]', N'F') IS NOT NULL
                    ALTER TABLE [dbo].[tb_notification] DROP CONSTRAINT [FK_tb_notification_rf_notification_type_notification_type_id];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tb_notification_receiver_user_id_is_seen_created_date' AND object_id = OBJECT_ID(N'[dbo].[tb_notification]'))
                    DROP INDEX [IX_tb_notification_receiver_user_id_is_seen_created_date] ON [dbo].[tb_notification];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tb_notification_receiver_user_id_created_date' AND object_id = OBJECT_ID(N'[dbo].[tb_notification]'))
                    DROP INDEX [IX_tb_notification_receiver_user_id_created_date] ON [dbo].[tb_notification];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tb_notification_notification_type_id' AND object_id = OBJECT_ID(N'[dbo].[tb_notification]'))
                    DROP INDEX [IX_tb_notification_notification_type_id] ON [dbo].[tb_notification];

                ALTER TABLE [dbo].[tb_notification] ADD [notification_type_id_old] uniqueidentifier NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [dbo].[tb_notification]
                SET [notification_type_id_old] = TRY_CONVERT(uniqueidentifier, [notification_type_id]);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[tb_notification] DROP COLUMN [notification_type_id];
                EXEC sp_rename N'[dbo].[tb_notification].[notification_type_id_old]', N'notification_type_id', N'COLUMN';

                DECLARE @pkNotificationType sysname;
                SELECT @pkNotificationType = kc.name
                FROM sys.key_constraints kc
                INNER JOIN sys.tables t ON t.object_id = kc.parent_object_id
                WHERE t.name = N'rf_notification_type' AND kc.type = N'PK';

                IF @pkNotificationType IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[rf_notification_type] DROP CONSTRAINT [' + @pkNotificationType + N']');

                ALTER TABLE [dbo].[rf_notification_type] ADD [id_old] uniqueidentifier NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [dbo].[rf_notification_type]
                SET [id_old] = TRY_CONVERT(uniqueidentifier, [id]);
                DELETE FROM [dbo].[rf_notification_type] WHERE [id_old] IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [dbo].[tb_notification]
                SET [notification_type_id] = NULL
                WHERE [notification_type_id] IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [dbo].[rf_notification_type] t WHERE t.[id_old] = [tb_notification].[notification_type_id]);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[rf_notification_type] DROP COLUMN [id];
                EXEC sp_rename N'[dbo].[rf_notification_type].[id_old]', N'id', N'COLUMN';
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[rf_notification_type] ALTER COLUMN [id] uniqueidentifier NOT NULL;
                ALTER TABLE [dbo].[rf_notification_type] ADD CONSTRAINT [PK_tb_notification_type] PRIMARY KEY CLUSTERED ([id]);

                ALTER TABLE [dbo].[rf_notification_type] DROP COLUMN
                    [is_active], [is_deleted], [modified_by], [modified_date], [deleted_by], [deleted_date],
                    [status_approval], [is_locked], [is_default], [approved_by], [approved_date], [approval_type_id];

                ALTER TABLE [dbo].[tb_notification] DROP COLUMN
                    [is_active], [is_deleted], [modified_by], [modified_date], [deleted_by], [deleted_date],
                    [status_approval], [is_locked], [is_default], [approved_by], [approved_date], [approval_type_id];

                EXEC sp_rename N'[dbo].[rf_notification_type]', N'tb_notification_type', N'OBJECT';

                ALTER TABLE [dbo].[tb_notification]
                    ADD CONSTRAINT [FK_tb_notification_tb_notification_type_notification_type_id]
                    FOREIGN KEY ([notification_type_id]) REFERENCES [dbo].[tb_notification_type]([id])
                    ON DELETE SET NULL;

                CREATE INDEX [IX_tb_notification_notification_type_id]
                    ON [dbo].[tb_notification] ([notification_type_id]);
                """);
        }
    }
}
