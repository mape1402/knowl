using KnOwl.Contracts.Distribution;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiRuntimeDistributionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "ContractReleases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAtUtc",
                table: "ContractReleases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RuntimeNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EnvironmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistributionMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EndpointBaseUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EndpointApiPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AuthenticationMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecretReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApiKeyReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractReleaseTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuntimeNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolloutGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActivationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RuntimeVersionApplied = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractReleaseTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractReleaseTargets_ContractArtifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "ContractArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractReleaseTargets_ContractReleaseItems_ReleaseItemId",
                        column: x => x.ReleaseItemId,
                        principalTable: "ContractReleaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractReleaseTargets_ContractReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "ContractReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractReleaseTargets_RuntimeNodes_RuntimeNodeId",
                        column: x => x.RuntimeNodeId,
                        principalTable: "RuntimeNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleaseTargets_ArtifactId",
                table: "ContractReleaseTargets",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleaseTargets_ReleaseId_ReleaseItemId_RuntimeNodeId",
                table: "ContractReleaseTargets",
                columns: new[] { "ReleaseId", "ReleaseItemId", "RuntimeNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleaseTargets_ReleaseItemId",
                table: "ContractReleaseTargets",
                column: "ReleaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleaseTargets_RuntimeNodeId_Status",
                table: "ContractReleaseTargets",
                columns: new[] { "RuntimeNodeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeNodes_Code",
                table: "RuntimeNodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeNodes_IsEnabled_Status",
                table: "RuntimeNodes",
                columns: new[] { "IsEnabled", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractReleaseTargets");

            migrationBuilder.DropTable(
                name: "RuntimeNodes");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "ContractReleases");

            migrationBuilder.DropColumn(
                name: "FailedAtUtc",
                table: "ContractReleases");
        }
    }
}

