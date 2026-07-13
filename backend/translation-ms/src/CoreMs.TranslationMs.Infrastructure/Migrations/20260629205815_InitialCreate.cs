using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoreMs.TranslationMs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "translation_ms");

            migrationBuilder.CreateTable(
                name: "translation_bundles",
                schema: "translation_ms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Realm = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Lang = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Data = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_translation_bundles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_translation_bundles_Lang",
                schema: "translation_ms",
                table: "translation_bundles",
                column: "Lang");

            migrationBuilder.CreateIndex(
                name: "IX_translation_bundles_Realm",
                schema: "translation_ms",
                table: "translation_bundles",
                column: "Realm");

            migrationBuilder.CreateIndex(
                name: "IX_translation_bundles_Realm_Lang",
                schema: "translation_ms",
                table: "translation_bundles",
                columns: new[] { "Realm", "Lang" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "translation_bundles",
                schema: "translation_ms");
        }
    }
}
