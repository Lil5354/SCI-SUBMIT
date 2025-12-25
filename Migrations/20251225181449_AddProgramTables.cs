using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SciSubmit.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgramSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConferenceId = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Venue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PresentationsLink = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PapersLink = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProgramLink = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramSchedules_Conferences_ConferenceId",
                        column: x => x.ConferenceId,
                        principalTable: "Conferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramScheduleId = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Contents = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramItems_ProgramSchedules_ProgramScheduleId",
                        column: x => x.ProgramScheduleId,
                        principalTable: "ProgramSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramItems_ProgramScheduleId",
                table: "ProgramItems",
                column: "ProgramScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSchedules_ConferenceId",
                table: "ProgramSchedules",
                column: "ConferenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramItems");

            migrationBuilder.DropTable(
                name: "ProgramSchedules");
        }
    }
}
