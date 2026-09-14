using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddRuntimeContractArtifacts : Migration
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

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "Topic", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_SourceReleaseId",
                table: "RuntimeContractArtifacts",
                column: "SourceReleaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuntimeContractArtifacts");
        }
    }
}
