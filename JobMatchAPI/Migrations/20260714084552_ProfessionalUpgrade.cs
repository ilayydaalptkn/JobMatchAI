using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobMatchAPI.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvHedefIs",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CvOkul",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CvSehir",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CvTecrubeler",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CvYetenekler",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "KullaniciId",
                table: "Ilanlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ilanlar_KullaniciId",
                table: "Ilanlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Basvurular_IlanId",
                table: "Basvurular",
                column: "IlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Basvurular_Ilanlar_IlanId",
                table: "Basvurular",
                column: "IlanId",
                principalTable: "Ilanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ilanlar_Kullanicilar_KullaniciId",
                table: "Ilanlar",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Basvurular_Ilanlar_IlanId",
                table: "Basvurular");

            migrationBuilder.DropForeignKey(
                name: "FK_Ilanlar_Kullanicilar_KullaniciId",
                table: "Ilanlar");

            migrationBuilder.DropIndex(
                name: "IX_Ilanlar_KullaniciId",
                table: "Ilanlar");

            migrationBuilder.DropIndex(
                name: "IX_Basvurular_IlanId",
                table: "Basvurular");

            migrationBuilder.DropColumn(
                name: "CvHedefIs",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "CvOkul",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "CvSehir",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "CvTecrubeler",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "CvYetenekler",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "KullaniciId",
                table: "Ilanlar");
        }
    }
}
