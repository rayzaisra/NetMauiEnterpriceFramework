using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace __PROJECT__.Infrastructure.Migrations.Audit
{
    /// <inheritdoc />
    public partial class AddActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_log_activity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    http_method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    route_template = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    status_code = table.Column<int>(type: "int", nullable: false),
                    success = table.Column<bool>(type: "bit", nullable: false),
                    duration_ms = table.Column<int>(type: "int", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    client_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_log_activity", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_log_activity_correlation_id",
                table: "tb_log_activity",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_log_activity_created_date",
                table: "tb_log_activity",
                column: "created_date");

            migrationBuilder.CreateIndex(
                name: "IX_tb_log_activity_status_code_created_date",
                table: "tb_log_activity",
                columns: new[] { "status_code", "created_date" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_log_activity_user_id_created_date",
                table: "tb_log_activity",
                columns: new[] { "user_id", "created_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_log_activity");
        }
    }
}
