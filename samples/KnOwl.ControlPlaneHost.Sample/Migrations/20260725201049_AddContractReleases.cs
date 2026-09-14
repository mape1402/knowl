using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddContractReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Draft"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InReviewAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeployedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractReleases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractReleaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractReleaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractReleaseItems_ContractArtifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "ContractArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractReleaseItems_ContractReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "ContractReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleaseItems_ArtifactId",
                table: "ContractReleaseItems",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleaseItems_ReleaseId_ArtifactId",
                table: "ContractReleaseItems",
                columns: new[] { "ReleaseId", "ArtifactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleases_Status",
                table: "ContractReleases",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractReleaseItems");

            migrationBuilder.DropTable(
                name: "ContractReleases");
        }
    }
}
