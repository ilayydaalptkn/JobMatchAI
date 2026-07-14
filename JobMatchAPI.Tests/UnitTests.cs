using JobMatchAPI.Services;
using Xunit;

namespace JobMatchAPI.Tests
{
    public class SifreServisiTests
    {
        [Fact]
        public void Hashle_ve_Dogrula_Basarili()
        {
            var hash = SifreServisi.Hashle("Test123!");
            Assert.True(SifreServisi.Dogrula("Test123!", hash));
            Assert.False(SifreServisi.Dogrula("YanlisSifre", hash));
        }

        [Fact]
        public void Eski_DuzMetin_Sifre_Gecis_Desteklenir()
        {
            Assert.True(SifreServisi.Dogrula("eskiSifre", "eskiSifre"));
        }
    }

    public class AiMatcherServiceTests
    {
        private readonly AiMatcherService _service = new();

        [Fact]
        public void CVAnalizEt_Yuksek_Skor_Doner()
        {
            var sonuc = _service.CVAnalizEt(
                "Aranan: C#, .NET, PostgreSQL, React backend developer",
                "bilgisayar mühendisliği c# .net postgresql react backend developer ankara");

            Assert.True(sonuc.Skor >= 50);
            Assert.False(string.IsNullOrWhiteSpace(sonuc.GeriBildirim));
        }

        [Fact]
        public void CVAnalizEt_Bos_Veri_Sifir_Skor()
        {
            var sonuc = _service.CVAnalizEt("", "test");
            Assert.Equal(0, sonuc.Skor);
        }
    }
}
