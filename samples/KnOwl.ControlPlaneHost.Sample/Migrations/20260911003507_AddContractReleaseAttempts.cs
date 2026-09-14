using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddContractReleaseAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractReleaseAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InitiatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractReleaseAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractReleaseAttempts_ContractReleaseTargets_ReleaseTargetId",
                        column: x => x.ReleaseTargetId,
                        principalTable: "ContractReleaseTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractReleaseAttempts_ReleaseTargetId_StartedAtUtc",
                table: "ContractReleaseAttempts",
                columns: new[] { "ReleaseTargetId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractReleaseAttempts");
        }
    }
}
