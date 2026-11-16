using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace __PROJECT__.Infrastructure.Migrations.Main
{
    /// <inheritdoc />
    public partial class RenameEmailTablesToReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "tb_email_provider",
                newName: "rf_email_provider");

            migrationBuilder.RenameTable(
                name: "tb_email_template",
                newName: "rf_email_template");

            migrationBuilder.RenameIndex(
                name: "IX_tb_email_provider_priority",
                table: "rf_email_provider",
                newName: "IX_rf_email_provider_priority");

            migrationBuilder.Sql("""
                EXEC sp_rename N'[dbo].[PK_tb_email_provider]', N'PK_rf_email_provider', N'OBJECT';
                EXEC sp_rename N'[dbo].[PK_tb_email_template]', N'PK_rf_email_template', N'OBJECT';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_rf_email_provider_priority",
                table: "rf_email_provider",
                newName: "IX_tb_email_provider_priority");

            migrationBuilder.Sql("""
                EXEC sp_rename N'[dbo].[PK_rf_email_provider]', N'PK_tb_email_provider', N'OBJECT';
                EXEC sp_rename N'[dbo].[PK_rf_email_template]', N'PK_tb_email_template', N'OBJECT';
                """);

            migrationBuilder.RenameTable(
                name: "rf_email_provider",
                newName: "tb_email_provider");

            migrationBuilder.RenameTable(
                name: "rf_email_template",
                newName: "tb_email_template");
        }
    }
}
