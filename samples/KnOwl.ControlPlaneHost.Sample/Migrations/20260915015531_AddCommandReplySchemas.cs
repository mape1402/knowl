using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandReplySchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReplyPayloadSchemaJson",
                table: "CommandVersions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE ContractArtifacts SET ArtifactType = 'CommandRequest' WHERE ArtifactType = 'Command';");
            migrationBuilder.Sql("UPDATE RuntimeContractArtifacts SET ArtifactType = 'CommandRequest' WHERE ArtifactType = 'Command';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ContractArtifacts SET ArtifactType = 'Command' WHERE ArtifactType = 'CommandRequest';");
            migrationBuilder.Sql("UPDATE RuntimeContractArtifacts SET ArtifactType = 'Command' WHERE ArtifactType = 'CommandRequest';");

            migrationBuilder.DropColumn(
                name: "ReplyPayloadSchemaJson",
                table: "CommandVersions");
        }
    }
}
