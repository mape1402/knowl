using KnOwl.Contracts.Distribution;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.RuntimeHost.Migrations.RuntimeStorage
{
    /// <inheritdoc />
    public partial class InitialRuntimeStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuntimeContractArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReleaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtifactType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DeployedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeContractArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeDesignNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EndpointBaseUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RemoteRuntimeNodeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistributionMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AccessTokenTtlSeconds = table.Column<int>(type: "int", nullable: false),
                    TokenRefreshSkewSeconds = table.Column<int>(type: "int", nullable: false),
                    TokenValidationCacheTtlSeconds = table.Column<int>(type: "int", nullable: false),
                    InboundClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InboundKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InboundSecretHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InboundAllowedScopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InboundCredentialStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Missing"),
                    InboundCredentialCreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InboundCredentialRotatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InboundCredentialRevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InboundLastTokenIssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InboundLastTokenFailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InboundLastFailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OutboundClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OutboundKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProtectedOutboundSecret = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OutboundRequestedScopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OutboundCredentialStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Missing"),
                    OutboundCredentialImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutboundLastTokenReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeDesignNodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_DeployedAtUtc",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "Topic", "DeployedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "Topic", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_SourceReleaseId",
                table: "RuntimeContractArtifacts",
                column: "SourceReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeDesignNodes_InboundClientId",
                table: "RuntimeDesignNodes",
                column: "InboundClientId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeDesignNodes_Key",
                table: "RuntimeDesignNodes",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuntimeContractArtifacts");

            migrationBuilder.DropTable(
                name: "RuntimeDesignNodes");
        }
    }
}

