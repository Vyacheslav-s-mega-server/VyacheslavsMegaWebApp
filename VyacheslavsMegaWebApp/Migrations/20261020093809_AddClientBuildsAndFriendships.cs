using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VyacheslavsMegaWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddClientBuildsAndFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string tableName = "Contacts";
            string constraintName = "FK_Contacts_AspNetUsers_UserId";

            migrationBuilder.Sql(
            $@"
                SET @sql = IF(
                    EXISTS (
                        SELECT 1 
                        FROM information_schema.table_constraints 
                        WHERE constraint_schema = DATABASE() 
                          AND table_name = '{tableName}' 
                          AND constraint_name = '{constraintName}' 
                          AND constraint_type = 'FOREIGN KEY'
                    ), 
                    'ALTER TABLE `{tableName}` DROP FOREIGN KEY `{constraintName}`;', 
                    'SELECT ''Foreign key does not exist'';'
                );

                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "YellowHint",
                table: "MainPageData",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "MainPageData",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "ServerVersion",
                table: "MainPageData",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "ServerAddress",
                table: "MainPageData",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "MainPageData",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MainPageData",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "AboutTag",
                table: "MainPageData",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AboutText",
                table: "MainPageData",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AboutTitle",
                table: "MainPageData",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feature1Desc",
                table: "MainPageData",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feature1Title",
                table: "MainPageData",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feature2Desc",
                table: "MainPageData",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feature2Title",
                table: "MainPageData",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feature3Desc",
                table: "MainPageData",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feature3Title",
                table: "MainPageData",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "MainPageData",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleAccent",
                table: "MainPageData",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "SessionKeyHash",
                table: "LauncherSessions",
                type: "varbinary(3072)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "longblob");

            migrationBuilder.AddColumn<string>(
                name: "ClientToken",
                table: "LauncherSessions",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServerId",
                table: "LauncherSessions",
                type: "longtext",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Contacts",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.CreateTable(
                name: "ClientBuildFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    BuildSlug = table.Column<string>(type: "longtext", nullable: false),
                    RelativePath = table.Column<string>(type: "longtext", nullable: false),
                    FileName = table.Column<string>(type: "longtext", nullable: false),
                    Sha256 = table.Column<string>(type: "longtext", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DownloadUrl = table.Column<string>(type: "longtext", nullable: true),
                    Category = table.Column<string>(type: "longtext", nullable: true),
                    DisplayName = table.Column<string>(type: "longtext", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientBuildFiles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RequesterId = table.Column<string>(type: "varchar(255)", nullable: false),
                    AddresseeId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Friendships_AspNetUsers_AddresseeId",
                        column: x => x.AddresseeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friendships_AspNetUsers_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "958cef03-4205-4adb-bc9f-85bf0291c544", "AQAAAAIAAYagAAAAEMkiRESPpwYF/7p1G/0jkJ4+HRuGXhvj4YuDs4QT2590mQ/pZ9wR4a84aOSEIYSyMA==", "1e7b369b-4631-4e38-a482-207e18fd7e2b" });

            migrationBuilder.UpdateData(
                table: "MainPageData",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AboutTag", "AboutText", "AboutTitle", "Feature1Desc", "Feature1Title", "Feature2Desc", "Feature2Title", "Feature3Desc", "Feature3Title", "Subtitle", "TitleAccent" },
                values: new object[] { "О ПРОЕКТЕ VEC", null, "Сервер, сделанный для людей", null, "Собственный лаунчер VEC", null, "Честная игра без донат-мусора", null, "Стабильность и защита", null, "ИДЕАЛЬНЫЙ МИР" });

            migrationBuilder.CreateIndex(
                name: "IX_LauncherSessions_SessionKeyHash",
                table: "LauncherSessions",
                column: "SessionKeyHash");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_AddresseeId",
                table: "Friendships",
                column: "AddresseeId");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_RequesterId",
                table: "Friendships",
                column: "RequesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_AspNetUsers_UserId",
                table: "Contacts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_AspNetUsers_UserId",
                table: "Contacts");

            migrationBuilder.DropTable(
                name: "ClientBuildFiles");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_LauncherSessions_SessionKeyHash",
                table: "LauncherSessions");

            migrationBuilder.DropColumn(
                name: "AboutTag",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "AboutText",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "AboutTitle",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "Feature1Desc",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "Feature1Title",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "Feature2Desc",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "Feature2Title",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "Feature3Desc",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "Feature3Title",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "TitleAccent",
                table: "MainPageData");

            migrationBuilder.DropColumn(
                name: "ClientToken",
                table: "LauncherSessions");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "LauncherSessions");

            migrationBuilder.AlterColumn<string>(
                name: "YellowHint",
                table: "MainPageData",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "MainPageData",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ServerVersion",
                table: "MainPageData",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ServerAddress",
                table: "MainPageData",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "MainPageData",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MainPageData",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "SessionKeyHash",
                table: "LauncherSessions",
                type: "longblob",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(3072)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Contacts",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0410d2d9-fb40-45fb-9c70-6626a184e790", "AQAAAAIAAYagAAAAEPbA2qg+ghHujuDnsow7DE0nmGtPlXjyG9sux5zpKHHu+8GKOWlbnYjsMJmCAsEUlQ==", "62eba975-9eb1-45b0-949a-9637a9e205b7" });

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_AspNetUsers_UserId",
                table: "Contacts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
