using Microsoft.EntityFrameworkCore.Migrations;

namespace NginxGUI.Data.Migrations
{
    public partial class AddProxyServices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NginxProxyServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NginxFileName = table.Column<string>(type: "TEXT", nullable: true),
                    SystemdServiceName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NginxProxyServices", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NginxProxyServices");
        }
    }
}
