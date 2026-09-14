using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddContractLifecycleState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "EventVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "EventVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeployedAtUtc",
                table: "EventVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeprecatedAtUtc",
                table: "EventVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InReviewAtUtc",
                table: "EventVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "EventVersions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "CommandVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "CommandVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeployedAtUtc",
                table: "CommandVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeprecatedAtUtc",
                table: "CommandVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InReviewAtUtc",
                table: "CommandVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CommandVersions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Commands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventVersions_Status",
                table: "EventVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsActive",
                table: "Events",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CommandVersions_Status",
                table: "CommandVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Commands_IsActive",
                table: "Commands",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventVersions_Status",
                table: "EventVersions");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsActive",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_CommandVersions_Status",
                table: "CommandVersions");

            migrationBuilder.DropIndex(
                name: "IX_Commands_IsActive",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "EventVersions");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "EventVersions");

            migrationBuilder.DropColumn(
                name: "DeployedAtUtc",
                table: "EventVersions");

            migrationBuilder.DropColumn(
                name: "DeprecatedAtUtc",
                table: "EventVersions");

            migrationBuilder.DropColumn(
                name: "InReviewAtUtc",
                table: "EventVersions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EventVersions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "CommandVersions");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "CommandVersions");

            migrationBuilder.DropColumn(
                name: "DeployedAtUtc",
                table: "CommandVersions");

            migrationBuilder.DropColumn(
                name: "DeprecatedAtUtc",
                table: "CommandVersions");

            migrationBuilder.DropColumn(
                name: "InReviewAtUtc",
                table: "CommandVersions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CommandVersions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Commands");
        }
    }
}
