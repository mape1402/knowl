using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddRuntimeNodeConnectionSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessTokenTtlSeconds",
                table: "RuntimeNodes",
                type: "int",
                nullable: false,
                defaultValue: 86400);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InboundAllowedScopes",
                table: "RuntimeNodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InboundClientId",
                table: "RuntimeNodes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "InboundCredentialCreatedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InboundCredentialRevokedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InboundCredentialRotatedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InboundCredentialStatus",
                table: "RuntimeNodes",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Missing");

            migrationBuilder.AddColumn<string>(
                name: "InboundKeyId",
                table: "RuntimeNodes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InboundLastFailureReason",
                table: "RuntimeNodes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "InboundLastTokenFailedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InboundLastTokenIssuedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InboundSecretHash",
                table: "RuntimeNodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RuntimeNodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OutboundClientId",
                table: "RuntimeNodes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OutboundCredentialImportedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutboundCredentialStatus",
                table: "RuntimeNodes",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Missing");

            migrationBuilder.AddColumn<string>(
                name: "OutboundKeyId",
                table: "RuntimeNodes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OutboundLastTokenReceivedAtUtc",
                table: "RuntimeNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutboundRequestedScopes",
                table: "RuntimeNodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProtectedOutboundSecret",
                table: "RuntimeNodes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TokenRefreshSkewSeconds",
                table: "RuntimeNodes",
                type: "int",
                nullable: false,
                defaultValue: 300);

            migrationBuilder.AddColumn<int>(
                name: "TokenValidationCacheTtlSeconds",
                table: "RuntimeNodes",
                type: "int",
                nullable: false,
                defaultValue: 300);

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeNodes_InboundClientId",
                table: "RuntimeNodes",
                column: "InboundClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuntimeNodes_InboundClientId",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "AccessTokenTtlSeconds",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundAllowedScopes",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundClientId",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundCredentialCreatedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundCredentialRevokedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundCredentialRotatedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundCredentialStatus",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundKeyId",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundLastFailureReason",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundLastTokenFailedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundLastTokenIssuedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "InboundSecretHash",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "OutboundClientId",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "OutboundCredentialImportedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "OutboundCredentialStatus",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "OutboundKeyId",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "OutboundLastTokenReceivedAtUtc",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "OutboundRequestedScopes",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "ProtectedOutboundSecret",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "TokenRefreshSkewSeconds",
                table: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "TokenValidationCacheTtlSeconds",
                table: "RuntimeNodes");
        }
    }
}
