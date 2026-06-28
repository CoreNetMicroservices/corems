using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoreMs.CommunicationMs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "communication_ms");

            migrationBuilder.CreateTable(
                name: "message",
                schema: "communication_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_by_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sent_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email",
                schema: "communication_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    email_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sender = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sender_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bcc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    recipient = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_message_id",
                        column: x => x.id,
                        principalSchema: "communication_ms",
                        principalTable: "message",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sms",
                schema: "communication_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    sid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms", x => x.id);
                    table.ForeignKey(
                        name: "FK_sms_message_id",
                        column: x => x.id,
                        principalSchema: "communication_ms",
                        principalTable: "message",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_attachment",
                schema: "communication_ms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    email_message_id = table.Column<long>(type: "bigint", nullable: false),
                    document_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    checksum = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_attachment", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_attachment_email_email_message_id",
                        column: x => x.email_message_id,
                        principalSchema: "communication_ms",
                        principalTable: "email",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_email_attachment_email",
                schema: "communication_ms",
                table: "email_attachment",
                column: "email_message_id");

            migrationBuilder.CreateIndex(
                name: "idx_message_created_at",
                schema: "communication_ms",
                table: "message",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_message_status",
                schema: "communication_ms",
                table: "message",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_message_type",
                schema: "communication_ms",
                table: "message",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "idx_message_user",
                schema: "communication_ms",
                table: "message",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_message_uuid",
                schema: "communication_ms",
                table: "message",
                column: "uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_attachment",
                schema: "communication_ms");

            migrationBuilder.DropTable(
                name: "sms",
                schema: "communication_ms");

            migrationBuilder.DropTable(
                name: "email",
                schema: "communication_ms");

            migrationBuilder.DropTable(
                name: "message",
                schema: "communication_ms");
        }
    }
}
