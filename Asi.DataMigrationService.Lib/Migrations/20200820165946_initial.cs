using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Asi.DataMigrationService.Lib.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportMaps",
                columns: table => new
                {
                    ImportMapId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(maxLength: 50, nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: true),
                    MapInfo = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_ImportMaps", x => x.ImportMapId));

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    TenantId = table.Column<string>(maxLength: 50, nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Tenant", x => x.TenantId));

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    ProjectId = table.Column<string>(maxLength: 50, nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    ProjectInfo = table.Column<string>(nullable: true),
                    CreatedOn = table.Column<DateTime>(nullable: false),
                    UpdatedOn = table.Column<DateTime>(nullable: false),
                    IsLocked = table.Column<bool>(nullable: false),
                    TenantId = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Project_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDataSource",
                columns: table => new
                {
                    ProjectDataSourceId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    DataSourceType = table.Column<string>(maxLength: 50, nullable: false),
                    Data = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDataSource", x => x.ProjectDataSourceId);
                    table.ForeignKey(
                        name: "FK_ProjectDataSource_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectJob",
                columns: table => new
                {
                    ProjectJobId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<string>(nullable: true),
                    State = table.Column<int>(nullable: false),
                    SubmittedOnUtc = table.Column<DateTime>(nullable: false),
                    SubmittedBy = table.Column<string>(maxLength: 100, nullable: true),
                    StartedOnUtc = table.Column<DateTime>(nullable: true),
                    CompletedOnUtc = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectJob", x => x.ProjectJobId);
                    table.ForeignKey(
                        name: "FK_ProjectJob_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectImport",
                columns: table => new
                {
                    ProjectImportId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectDataSourceId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 150, nullable: false),
                    PropertyNames = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectImport", x => x.ProjectImportId);
                    table.ForeignKey(
                        name: "FK_ProjectImport_ProjectDataSource_ProjectDataSourceId",
                        column: x => x.ProjectDataSourceId,
                        principalTable: "ProjectDataSource",
                        principalColumn: "ProjectDataSourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectJobMessage",
                columns: table => new
                {
                    ProjectJobMessageId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectJobId = table.Column<int>(nullable: false),
                    MessageType = table.Column<int>(nullable: false),
                    Processor = table.Column<string>(maxLength: 50, nullable: true),
                    Source = table.Column<string>(maxLength: 100, nullable: true),
                    RowNumber = table.Column<int>(nullable: false),
                    Message = table.Column<string>(maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectJobMessage", x => x.ProjectJobMessageId);
                    table.ForeignKey(
                        name: "FK_ProjectJobMessage_ProjectJob_ProjectJobId",
                        column: x => x.ProjectJobId,
                        principalTable: "ProjectJob",
                        principalColumn: "ProjectJobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectImportData",
                columns: table => new
                {
                    ProjectImportDataId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectImportId = table.Column<int>(nullable: false),
                    RowNumber = table.Column<int>(nullable: false),
                    Data = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectImportData", x => x.ProjectImportDataId);
                    table.ForeignKey(
                        name: "FK_ProjectImportData_ProjectImport_ProjectImportId",
                        column: x => x.ProjectImportId,
                        principalTable: "ProjectImport",
                        principalColumn: "ProjectImportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportMaps_TenantId_Name",
                table: "ImportMaps",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Project_TenantId",
                table: "Project",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDataSource_ProjectId_DataSourceType_Name",
                table: "ProjectDataSource",
                columns: new[] { "ProjectId", "DataSourceType", "Name" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImport_ProjectDataSourceId_Name",
                table: "ProjectImport",
                columns: new[] { "ProjectDataSourceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImportData_ProjectImportId_RowNumber",
                table: "ProjectImportData",
                columns: new[] { "ProjectImportId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJob_ProjectId",
                table: "ProjectJob",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJobMessage_ProjectJobId",
                table: "ProjectJobMessage",
                column: "ProjectJobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportMaps");

            migrationBuilder.DropTable(
                name: "ProjectImportData");

            migrationBuilder.DropTable(
                name: "ProjectJobMessage");

            migrationBuilder.DropTable(
                name: "ProjectImport");

            migrationBuilder.DropTable(
                name: "ProjectJob");

            migrationBuilder.DropTable(
                name: "ProjectDataSource");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "Tenant");
        }
    }
}
