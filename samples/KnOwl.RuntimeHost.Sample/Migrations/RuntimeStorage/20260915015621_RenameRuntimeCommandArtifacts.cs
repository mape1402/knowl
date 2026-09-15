using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.RuntimeHost.Sample.Migrations.RuntimeStorage
{
    /// <inheritdoc />
    public partial class RenameRuntimeCommandArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE RuntimeContractArtifacts SET ArtifactType = 'CommandRequest' WHERE ArtifactType = 'Command';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE RuntimeContractArtifacts SET ArtifactType = 'Command' WHERE ArtifactType = 'CommandRequest';");
        }
    }
}
