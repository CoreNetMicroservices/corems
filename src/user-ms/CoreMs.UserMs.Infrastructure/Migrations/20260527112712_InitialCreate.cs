using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoreMs.UserMs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user_ms");

            migrationBuilder.CreateTable(
                name: "app_user",
                schema: "user_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    image_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    phone_verified = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "action_tokens",
                schema: "user_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_action_tokens_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "user_ms",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_role",
                schema: "user_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_app_user_role_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "user_ms",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authorization_codes",
                schema: "user_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    redirect_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    scope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    code_challenge = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    code_challenge_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    nonce = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    state = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_authorization_codes_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "user_ms",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_token",
                schema: "user_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_login_token_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "user_ms",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_action_tokens_token_hash",
                schema: "user_ms",
                table: "action_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_action_tokens_user_id",
                schema: "user_ms",
                table: "action_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_action_tokens_uuid",
                schema: "user_ms",
                table: "action_tokens",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_user_created_at",
                schema: "user_ms",
                table: "app_user",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_email",
                schema: "user_ms",
                table: "app_user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_user_phone_number",
                schema: "user_ms",
                table: "app_user",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_user_uuid",
                schema: "user_ms",
                table: "app_user",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_app_user_role_user",
                schema: "user_ms",
                table: "app_user_role",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_authorization_codes_code",
                schema: "user_ms",
                table: "authorization_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_authorization_codes_expires_at",
                schema: "user_ms",
                table: "authorization_codes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_authorization_codes_user_id",
                schema: "user_ms",
                table: "authorization_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_login_token_created_at",
                schema: "user_ms",
                table: "login_token",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_login_token_user",
                schema: "user_ms",
                table: "login_token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_token_token",
                schema: "user_ms",
                table: "login_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_login_token_uuid",
                schema: "user_ms",
                table: "login_token",
                column: "uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "action_tokens",
                schema: "user_ms");

            migrationBuilder.DropTable(
                name: "app_user_role",
                schema: "user_ms");

            migrationBuilder.DropTable(
                name: "authorization_codes",
                schema: "user_ms");

            migrationBuilder.DropTable(
                name: "login_token",
                schema: "user_ms");

            migrationBuilder.DropTable(
                name: "app_user",
                schema: "user_ms");
        }
    }
}
