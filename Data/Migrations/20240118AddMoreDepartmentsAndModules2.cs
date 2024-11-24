using Microsoft.EntityFrameworkCore.Migrations;

namespace CMCS.Data.Migrations
{
    public partial class AddMoreDepartmentsAndModules2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new departments
            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Name", "Code", "IsActive" },
                values: new object[,]
                {
                    { 4, "Mathematics", "MATH", true },
                    { 5, "Physics", "PHYS", true },
                    { 6, "Chemistry", "CHEM", true },
                    { 7, "Biology", "BIO", true },
                    { 8, "Engineering", "ENG", true },
                    { 9, "Business Management", "BUS", true },
                    { 10, "Economics", "ECON", true },
                    { 11, "Psychology", "PSYC", true }
                });

            // Add new modules
            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "Code", "Name", "DepartmentId", "IsActive" },
                values: new object[,]
                {
                    // Mathematics Modules
                    { 7, "MTH101", "Calculus I", 4, true },
                    { 8, "MTH201", "Linear Algebra", 4, true },
                    { 9, "MTH301", "Abstract Algebra", 4, true },
                    { 10, "MTH401", "Real Analysis", 4, true },

                    // Physics Modules
                    { 11, "PHY101", "Mechanics", 5, true },
                    { 12, "PHY201", "Electromagnetism", 5, true },
                    { 13, "PHY301", "Quantum Physics", 5, true },
                    { 14, "PHY401", "Thermodynamics", 5, true },

                    // Chemistry Modules
                    { 15, "CHM101", "General Chemistry", 6, true },
                    { 16, "CHM201", "Organic Chemistry", 6, true },
                    { 17, "CHM301", "Physical Chemistry", 6, true },
                    { 18, "CHM401", "Analytical Chemistry", 6, true },

                    // Biology Modules
                    { 19, "BIO101", "Cell Biology", 7, true },
                    { 20, "BIO201", "Genetics", 7, true },
                    { 21, "BIO301", "Ecology", 7, true },
                    { 22, "BIO401", "Molecular Biology", 7, true },

                    // Engineering Modules
                    { 23, "ENG101", "Engineering Mathematics", 8, true },
                    { 24, "ENG201", "Mechanics of Materials", 8, true },
                    { 25, "ENG301", "Control Systems", 8, true },
                    { 26, "ENG401", "Engineering Design", 8, true },

                    // Business Management Modules
                    { 27, "BUS101", "Introduction to Business", 9, true },
                    { 28, "BUS201", "Marketing Management", 9, true },
                    { 29, "BUS301", "Operations Management", 9, true },
                    { 30, "BUS401", "Strategic Management", 9, true },

                    // Economics Modules
                    { 31, "ECN101", "Microeconomics", 10, true },
                    { 32, "ECN201", "Macroeconomics", 10, true },
                    { 33, "ECN301", "International Economics", 10, true },
                    { 34, "ECN401", "Development Economics", 10, true },

                    // Psychology Modules
                    { 35, "PSY101", "Introduction to Psychology", 11, true },
                    { 36, "PSY201", "Developmental Psychology", 11, true },
                    { 37, "PSY301", "Cognitive Psychology", 11, true },
                    { 38, "PSY401", "Clinical Psychology", 11, true }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the added modules
            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValues: new object[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38 });

            // Remove the added departments
            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValues: new object[] { 4, 5, 6, 7, 8, 9, 10, 11 });
        }
    }
}
