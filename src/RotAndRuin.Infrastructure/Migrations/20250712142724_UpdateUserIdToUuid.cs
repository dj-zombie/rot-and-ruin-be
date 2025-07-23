using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RotAndRuin.Infrastructure.Migrations
{
    public partial class UpdateUserIdToUuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, ensure we have a default user if we need one
            migrationBuilder.Sql(@"
                INSERT INTO ""Users"" (""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsAdmin"", ""CreatedAt"", ""UpdatedAt"")
        SELECT 
            '00000000-0000-0000-0000-000000000000',
            'system',
            'system@example.com',
            'not_used',
            false,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        WHERE NOT EXISTS (
            SELECT 1 FROM ""Users"" WHERE ""Id"" = '00000000-0000-0000-0000-000000000000'
        );
    ");

            // Drop existing foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserId1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId1",
                table: "Orders");

            // Add the new UUID column
            migrationBuilder.AddColumn<Guid>(
                name: "UserIdNew",
                table: "Orders",
                type: "uuid",
                nullable: true);

            // Convert existing data
            migrationBuilder.Sql(@"
                UPDATE ""Orders"" 
                SET ""UserIdNew"" = CASE 
                    WHEN ""UserId"" IS NULL OR ""UserId"" = '' THEN '00000000-0000-0000-0000-000000000000'
                    WHEN ""UserId"" ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN ""UserId""::uuid
                    ELSE '00000000-0000-0000-0000-000000000000'
                END;");

            // Drop old columns
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Orders");

            // Rename the new column
            migrationBuilder.RenameColumn(
                name: "UserIdNew",
                table: "Orders",
                newName: "UserId");

            // Make it non-nullable with default value
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Add new index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Create DataProtectionKeys table
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            // Clean up ProductImages table
            migrationBuilder.DropColumn(
                name: "GridThumbnailUrl",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "HighResolutionUrl",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "ProductImages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            // Restore ProductImage columns
            migrationBuilder.AddColumn<string>(
                name: "GridThumbnailUrl",
                table: "ProductImages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HighResolutionUrl",
                table: "ProductImages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "ProductImages",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Convert UUID back to string
            migrationBuilder.AddColumn<string>(
                name: "UserIdOld",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Orders"" 
                SET ""UserIdOld"" = ""UserId""::text;");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "UserIdOld",
                table: "Orders",
                newName: "UserId");

            // Restore UserId1 and its relationships
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId1",
                table: "Orders",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserId1",
                table: "Orders",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            // Drop DataProtectionKeys table
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            // Clean up the default user if it was created by this migration
            migrationBuilder.Sql(@"
                DELETE FROM ""Users""
                WHERE ""Id"" = '00000000-0000-0000-0000-000000000000'
                AND ""Username"" = 'system';
            ");
        }
    }
}