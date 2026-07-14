using System.Net.Http.Json;
using JobMatchAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobMatchAPI.Tests
{
    public class ApiIntegrationTests : IClassFixture<TestWebAppFactory>
    {
        private readonly HttpClient _client;

        public ApiIntegrationTests(TestWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Kayit_ve_Giris_Akisi_Calisir()
        {
            var email = $"test{Guid.NewGuid():N}@jobmatch.test";

            var kayit = await _client.PostAsJsonAsync("/api/Kimlik/kayit", new
            {
                AdSoyad = "Test Kullanici",
                Eposta = email,
                Telefon = "05551234567",
                Sifre = "Test123!",
                Sehir = "Ankara",
                Rol = "Ogrenci"
            });

            Assert.True(kayit.IsSuccessStatusCode);

            var giris = await _client.PostAsJsonAsync("/api/Kimlik/giris", new
            {
                Eposta = email,
                Sifre = "Test123!"
            });

            Assert.True(giris.IsSuccessStatusCode);
            var body = await giris.Content.ReadFromJsonAsync<GirisCevabi>();
            Assert.NotNull(body?.token);
            Assert.Equal("Ogrenci", body.rol);
        }

        [Fact]
        public async Task Ilan_Listele_Anonim_Erisilebilir()
        {
            var cevap = await _client.GetAsync("/api/Ilan/listele");
            Assert.True(cevap.IsSuccessStatusCode);
        }

        private class GirisCevabi
        {
            public string token { get; set; } = "";
            public string rol { get; set; } = "";
        }
    }

    public class TestWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<VeriTabaniBaglantisi>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<VeriTabaniBaglantisi>(options =>
                    options.UseSqlite("DataSource=file:memdb1?mode=memory&cache=shared"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<VeriTabaniBaglantisi>();
                db.Database.EnsureCreated();
            });
        }
    }
}
