using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class RefactorContractsVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Topic_Version",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Commands_Topic_Version",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "PayloadSchemaJson",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PayloadSchemaJson",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Commands");

            migrationBuilder.CreateTable(
                name: "CommandVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    PayloadSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandVersions_Commands_CommandDefinitionId",
                        column: x => x.CommandDefinitionId,
                        principalTable: "Commands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    PayloadSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventVersions_Events_EventDefinitionId",
                        column: x => x.EventDefinitionId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Topic",
                table: "Events",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_Commands_Topic",
                table: "Commands",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_CommandVersions_CommandDefinitionId_VersionNumber",
                table: "CommandVersions",
                columns: new[] { "CommandDefinitionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandVersions_CreatedAtUtc",
                table: "CommandVersions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EventVersions_CreatedAtUtc",
                table: "EventVersions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EventVersions_EventDefinitionId_VersionNumber",
                table: "EventVersions",
                columns: new[] { "EventDefinitionId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandVersions");

            migrationBuilder.DropTable(
                name: "EventVersions");

            migrationBuilder.DropIndex(
                name: "IX_Events_Topic",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Commands_Topic",
                table: "Commands");

            migrationBuilder.AddColumn<string>(
                name: "PayloadSchemaJson",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Events",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayloadSchemaJson",
                table: "Commands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Commands",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Topic_Version",
                table: "Events",
                columns: new[] { "Topic", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Commands_Topic_Version",
                table: "Commands",
                columns: new[] { "Topic", "Version" });
        }
    }
}
