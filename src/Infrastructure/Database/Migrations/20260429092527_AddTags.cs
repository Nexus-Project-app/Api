using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tag_id",
                schema: "public",
                table: "posts");

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_tags",
                schema: "public",
                columns: table => new
                {
                    posts_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tags_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_tags", x => new { x.posts_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_post_tags_posts_posts_id",
                        column: x => x.posts_id,
                        principalSchema: "public",
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_tags_tags_tags_id",
                        column: x => x.tags_id,
                        principalSchema: "public",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_tags_tags_id",
                schema: "public",
                table: "post_tags",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_name",
                schema: "public",
                table: "tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_tags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "public");

            migrationBuilder.AddColumn<Guid>(
                name: "tag_id",
                schema: "public",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
