using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VyacheslavsMegaWebApp.Migrations
{
    /// <inheritdoc />
    public partial class CustomCapes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LauncherSessions_SessionKeyHash",
                table: "LauncherSessions");

            migrationBuilder.AlterColumn<byte[]>(
                name: "SessionKeyHash",
                table: "LauncherSessions",
                type: "longblob",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(3072)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Friendships",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
                .Annotation("Relational:ColumnOrder", 0)
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ClientBuildFiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
                .Annotation("Relational:ColumnOrder", 0)
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModrinthSlug",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasMinecraftLicense",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftGamertag",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MicrosoftLicenseLinkedAt",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftUuid",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnedMojangCapesJson",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomCapes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Slug = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    TextureHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    IsPublic = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCapes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CapeRedeemCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CapeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RedeemedByUserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapeRedeemCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapeRedeemCodes_AspNetUsers_RedeemedByUserId",
                        column: x => x.RedeemedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CapeRedeemCodes_CustomCapes_CapeId",
                        column: x => x.CapeId,
                        principalTable: "CustomCapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserCapeOwnerships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    CapeId = table.Column<int>(type: "int", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AcquiredVia = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCapeOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCapeOwnerships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCapeOwnerships_CustomCapes_CapeId",
                        column: x => x.CapeId,
                        principalTable: "CustomCapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "HasMinecraftLicense", "MicrosoftGamertag", "MicrosoftLicenseLinkedAt", "MicrosoftUuid", "OwnedMojangCapesJson", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bcb2feb1-69f9-4a5a-afc7-c61889b8f081", false, null, null, null, null, "AQAAAAIAAYagAAAAEPVnlOW4GZWq+O81hv6RmiM7KXXHSqd4L36/WsZ8b/0XIojIfMrvp05UTAijFR7MPw==", "afd96aec-53ae-4587-b06f-e54133e5dbf3" });

            migrationBuilder.CreateIndex(
                name: "IX_CapeRedeemCodes_CapeId",
                table: "CapeRedeemCodes",
                column: "CapeId");

            migrationBuilder.CreateIndex(
                name: "IX_CapeRedeemCodes_Code",
                table: "CapeRedeemCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapeRedeemCodes_RedeemedByUserId",
                table: "CapeRedeemCodes",
                column: "RedeemedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomCapes_Slug",
                table: "CustomCapes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCapeOwnerships_CapeId",
                table: "UserCapeOwnerships",
                column: "CapeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCapeOwnerships_UserId_CapeId",
                table: "UserCapeOwnerships",
                columns: new[] { "UserId", "CapeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapeRedeemCodes");

            migrationBuilder.DropTable(
                name: "UserCapeOwnerships");

            migrationBuilder.DropTable(
                name: "CustomCapes");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "ClientBuildFiles");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "ClientBuildFiles");

            migrationBuilder.DropColumn(
                name: "ModrinthSlug",
                table: "ClientBuildFiles");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "ClientBuildFiles");

            migrationBuilder.DropColumn(
                name: "HasMinecraftLicense",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MicrosoftGamertag",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MicrosoftLicenseLinkedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MicrosoftUuid",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OwnedMojangCapesJson",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<byte[]>(
                name: "SessionKeyHash",
                table: "LauncherSessions",
                type: "varbinary(3072)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "longblob");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Friendships",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ClientBuildFiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "958cef03-4205-4adb-bc9f-85bf0291c544", "AQAAAAIAAYagAAAAEMkiRESPpwYF/7p1G/0jkJ4+HRuGXhvj4YuDs4QT2590mQ/pZ9wR4a84aOSEIYSyMA==", "1e7b369b-4631-4e38-a482-207e18fd7e2b" });

            migrationBuilder.CreateIndex(
                name: "IX_LauncherSessions_SessionKeyHash",
                table: "LauncherSessions",
                column: "SessionKeyHash");
        }
    }
}
