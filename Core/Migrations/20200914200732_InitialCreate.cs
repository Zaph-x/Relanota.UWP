using Microsoft.EntityFrameworkCore.Migrations;

namespace Core.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Content = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "NoteTag",
                columns: table => new
                {
                    NoteKey = table.Column<int>(nullable: false),
                    TagKey = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteTag", x => new { x.NoteKey, x.TagKey });
                    table.ForeignKey(
                        name: "FK_NoteTag_Notes_NoteKey",
                        column: x => x.NoteKey,
                        principalTable: "Notes",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoteTag_Tags_TagKey",
                        column: x => x.TagKey,
                        principalTable: "Tags",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteTag_TagKey",
                table: "NoteTag",
                column: "TagKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteTag");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
