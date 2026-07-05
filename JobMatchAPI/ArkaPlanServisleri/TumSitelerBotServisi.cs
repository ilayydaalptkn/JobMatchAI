using JobMatchAPI.Data;
using JobMatchAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace JobMatchAPI.ArkaPlanServisleri
{
    public class TumSitelerBotServisi : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HttpClient _httpClient;

        public TumSitelerBotServisi(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[BOT MOTORU]: Çoklu kariyer platformlarından veri toplama süreci başladı...");

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var veriTabani = scope.ServiceProvider.GetRequiredService<VeriTabaniBaglantisi>();
                        int toplamEklenenYeniIlan = 0;

                        // =========================================================================
                        // 1. KAYNAK: ADZUNA API HAVUZU (LinkedIn, Indeed vb. entegrasyonu simüle eder)
                        // =========================================================================
                        try
                        {
                            var response1 = await _httpClient.GetAsync("https://v3.adzuna.com/v1/api/jobs/tr/search/1?app_id=cba67664&app_key=4b0c16997cfd90f230da307e5f3a0976&what=developer", stoppingToken);
                            if (response1.IsSuccessStatusCode)
                            {
                                var jsonString = await response1.Content.ReadAsStringAsync();
                                using var doc = JsonDocument.Parse(jsonString);
                                var ilanlar = doc.RootElement.GetProperty("results");

                                foreach (var element in ilanlar.EnumerateArray())
                                {
                                    string baslik = element.GetProperty("title").GetString() ?? "Yazılım Stajyeri";
                                    string sirket = element.GetProperty("company").GetProperty("display_name").GetString() ?? "Anonim Şirket";
                                    string sehir = "Uzaktan";
                                    if (element.TryGetProperty("location", out var loc) && loc.GetProperty("area").GetArrayLength() > 0)
                                    {
                                        sehir = loc.GetProperty("area")[0].GetString() ?? "Uzaktan";
                                    }
                                    string aciklama = element.GetProperty("description").GetString() ?? "";

                                    // Mükerrer Kontrolü
                                    bool varMi = veriTabani.Ilanlar.Any(i => i.Baslik.ToLower() == baslik.ToLower() && i.SirketAdi.ToLower() == sirket.ToLower());
                                    if (!varMi)
                                    {
                                        veriTabani.Ilanlar.Add(new Ilan
                                        {
                                            Baslik = baslik,
                                            SirketAdi = sirket,
                                            Sehir = sehir,
                                            Aciklama = $"[Kaynak: Kariyer Havuzu 1] {aciklama}",
                                            Maas = "Belirtilmemiş",
                                            YayinlanmaTarihi = DateTime.UtcNow
                                        });
                                        toplamEklenenYeniIlan++;
                                    }
                                }
                            }
                        }
                        catch (Exception ex) { Console.WriteLine($"[KAYNAK 1 HATASI]: {ex.Message}"); }

                        // =========================================================================
                        // 2. KAYNAK: GITHUB JOBS / AÇIK KAYNAK YAZILIM İLANLARI RSS FEED BESLEMESİ
                        // =========================================================================
                        try
                        {
                            // Küresel açık kaynak yazılım ilanları sağlayan test feed'i (Arbejdsformidlingen API mantığı)
                            var response2 = await _httpClient.GetAsync("https://jobicy.com/api/v2/remote-jobs?count=10&geo=turkey", stoppingToken);
                            if (response2.IsSuccessStatusCode)
                            {
                                var jsonString = await response2.Content.ReadAsStringAsync();
                                using var doc = JsonDocument.Parse(jsonString);
                                if (doc.RootElement.TryGetProperty("jobs", out var jobsDizisi))
                                {
                                    foreach (var job in jobsDizisi.EnumerateArray())
                                    {
                                        string baslik = job.GetProperty("jobTitle").GetString() ?? "Yazılım Mühendisi";
                                        string sirket = job.GetProperty("companyName").GetString() ?? "Global Tech";
                                        string sehir = job.GetProperty("jobGeo").GetString() ?? "Uzaktan";
                                        string aciklama = job.GetProperty("url").GetString() ?? "İlan detayları için web sitesini ziyaret edin.";

                                        // Mükerrer Kontrolü
                                        bool varMi = veriTabani.Ilanlar.Any(i => i.Baslik.ToLower() == baslik.ToLower() && i.SirketAdi.ToLower() == sirket.ToLower());
                                        if (!varMi)
                                        {
                                            veriTabani.Ilanlar.Add(new Ilan
                                            {
                                                Baslik = baslik,
                                                SirketAdi = sirket,
                                                Sehir = sehir,
                                                Aciklama = $"[Kaynak: Küresel Yazılım Portalı] Aranan Nitelikler ve Başvuru Linki: {aciklama}",
                                                Maas = "Döviz Bazlı / Global",
                                                YayinlanmaTarihi = DateTime.UtcNow
                                            });
                                            toplamEklenenYeniIlan++;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex) { Console.WriteLine($"[KAYNAK 2 HATASI]: {ex.Message}"); }

                        // =========================================================================
                        // VERİTABANINA TOPLU KAYIT
                        // =========================================================================
                        if (toplamEklenenYeniIlan > 0)
                        {
                            await veriTabani.SaveChangesAsync(stoppingToken);
                            Console.WriteLine($"[BOT MOTORU BAŞARILI]: Tüm siteler tarandı. {toplamEklenenYeniIlan} adet benzersiz ilan veritabanına eklendi!");
                        }
                        else
                        {
                            Console.WriteLine("[BOT MOTORU STABİL]: Yeni ilan bulunamadı, mevcut ilanlar güncel.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BOT GENEL HATA]: {ex.Message}");
                }

                // Sistemleri yormamak adına her 30 dakikada bir siteleri kolaçan etsin
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}