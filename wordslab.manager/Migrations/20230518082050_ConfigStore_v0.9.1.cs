using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wordslab.manager.Migrations
{
    /// <inheritdoc />
    public partial class ConfigStore_v091 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemovalDate",
                table: "AppDeployment",
                newName: "LastStateTimestamp");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "AppDeployment",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "AppDeployment");

            migrationBuilder.RenameColumn(
                name: "LastStateTimestamp",
                table: "AppDeployment",
                newName: "RemovalDate");
        }
    }
}
