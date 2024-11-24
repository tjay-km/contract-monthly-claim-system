using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CMCS.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentsAndModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Claims",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModuleId",
                table: "Claims",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "IT", true, "Information Technology" },
                    { 2, "CS", true, "Computer Science" },
                    { 3, "SE", true, "Software Engineering" }
                });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "Code", "DepartmentId", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "IT101", 1, true, "Introduction to Programming" },
                    { 2, "IT102", 1, true, "Database Systems" },
                    { 3, "CS201", 2, true, "Data Structures" },
                    { 4, "CS202", 2, true, "Algorithms" },
                    { 5, "SE301", 3, true, "Software Design" },
                    { 6, "SE302", 3, true, "Software Testing" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_DepartmentId",
                table: "Claims",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ModuleId",
                table: "Claims",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Code",
                table: "Modules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_DepartmentId",
                table: "Modules",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Departments_DepartmentId",
                table: "Claims",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Modules_ModuleId",
                table: "Claims",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Departments_DepartmentId",
                table: "Claims");

            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Modules_ModuleId",
                table: "Claims");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Claims_DepartmentId",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_ModuleId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "Claims");
        }
    }
}
