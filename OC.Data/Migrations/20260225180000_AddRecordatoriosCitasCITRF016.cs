using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OC.Data.Migrations
{
    /// <inheritdoc />
    [Migration("20260225180000_AddRecordatoriosCitasCITRF016")]
    public partial class AddRecordatoriosCitasCITRF016 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificacionesActivas",
                table: "Citas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CanalNotificacion",
                table: "Citas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Email");

            migrationBuilder.CreateTable(
                name: "EnviosNotificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CitaId = table.Column<int>(type: "int", nullable: false),
                    TipoNotificacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaHoraEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MensajeResumen = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Exito = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnviosNotificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnviosNotificacion_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnviosNotificacion_CitaId",
                table: "EnviosNotificacion",
                column: "CitaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnviosNotificacion");

            migrationBuilder.DropColumn(
                name: "NotificacionesActivas",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "CanalNotificacion",
                table: "Citas");
        }
    }
}
