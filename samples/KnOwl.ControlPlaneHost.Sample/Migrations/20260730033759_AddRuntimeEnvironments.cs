using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddRuntimeEnvironments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnvironmentId",
                table: "RuntimeNodes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RuntimeEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeEnvironments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeNodes_EnvironmentId",
                table: "RuntimeNodes",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeEnvironments_Code",
                table: "RuntimeEnvironments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeEnvironments_IsEnabled",
                table: "RuntimeEnvironments",
                column: "IsEnabled");

            migrationBuilder.AddForeignKey(
                name: "FK_RuntimeNodes_RuntimeEnvironments_EnvironmentId",
                table: "RuntimeNodes",
                column: "EnvironmentId",
                principalTable: "RuntimeEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RuntimeNodes_RuntimeEnvironments_EnvironmentId",
                table: "RuntimeNodes");

            migrationBuilder.DropTable(
                name: "RuntimeEnvironments");

            migrationBuilder.DropIndex(
                name: "IX_RuntimeNodes_EnvironmentId",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "RuntimeNodes");
        }
    }
}
