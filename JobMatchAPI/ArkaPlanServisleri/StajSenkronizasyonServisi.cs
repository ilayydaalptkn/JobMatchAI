using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using JobMatchAPI.Data;
using JobMatchAPI.Models;
using System.Text.Json;

namespace JobMatchAPI.ArkaPlanServisleri
{
    public class StajSenkronizasyonServisi : BackgroundService
    {
        private readonly IServiceProvider _servisSaglayici;
        private readonly HttpClient _internetIstemcisi;

        // --- CANLI ADZUNA KÜRESEL API BİLGİLERİ ---
        private readonly string _apiUygulamaId = "b2380762";
        private readonly string _apiAnahtari = "0c8722141efc4c023d3d416fcb84e10e";

        // Lokasyonu en kararlı çalışan "us" (Amerika / Küresel) havuzuna çekiyoruz.
        private string ApiUrlOlustur(string ulkeKodu = "us", int sayfaNo = 1) =>
            $"https://api.adzuna.com/v1/api/jobs/{ulkeKodu}/search/{sayfaNo}?app_id={_apiUygulamaId}&app_key={_apiAnahtari}&what=internship&results_per_page=15";

        public StajSenkronizasyonServisi(IServiceProvider servisSaglayici)
        {
            _servisSaglayici = servisSaglayici;
            _internetIstemcisi = new HttpClient();
            _internetIstemcisi.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        protected override async Task ExecuteAsync(CancellationToken durdurmaSinyali)
        {
            // İlk açılışta sistemin oturması için 5 saniye bekletiyoruz
            await Task.Delay(5000, durdurmaSinyali);

            while (!durdurmaSinyali.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[KÜRESEL MOTOR]: Adzuna API üzerinden küresel staj verileri taranıyor...");
                    await KureselIlanlariToplaVeKaydet();
                }
                catch (Exception hata)
                {
                    // Hata ne olursa olsun yakala ve konsola yaz, uygulamanın çökmesini (Exit Code -1) engelle!
                    Console.WriteLine($"[KÜRESEL MOTOR İSTİSNA YAKALANDI]: {hata.Message}");
                }

                // API kotanı korumak ve test ederken sürekli istek atmamak için süreyi ideal olan 5 dakikaya (300 saniye) çekiyoruz.
                await Task.Delay(TimeSpan.FromSeconds(300), durdurmaSinyali);
            }
        }

        private async Task KureselIlanlariToplaVeKaydet()
        {
            try
            {
                var cevap = await _internetIstemcisi.GetAsync(ApiUrlOlustur("us", 1));

                if (cevap.IsSuccessStatusCode)
                {
                    // --- KRİTİK DÜZELTME BURADA ---
                    // Karakter seti hatasını aşmak için veriyi ham byte dizisi olarak alıp, 
                    // zorla UTF-8 formatında metne (string) çeviriyoruz.
                    var hamByteVerisi = await cevap.Content.ReadAsByteArrayAsync();
                    var jsonMetni = System.Text.Encoding.UTF8.GetString(hamByteVerisi);

                    using (JsonDocument dokuman = JsonDocument.Parse(jsonMetni))
                    {
                        var root = dokuman.RootElement;
                        if (root.TryGetProperty("results", out JsonElement sonuclar) && sonuclar.ValueKind == JsonValueKind.Array)
                        {
                            using (var kapsam = _servisSaglayici.CreateScope())
                            {
                                var veriTabani = kapsam.ServiceProvider.GetRequiredService<VeriTabaniBaglantisi>();
                                int eklenenYeniIlanSayisi = 0;

                                foreach (var job in sonuclar.EnumerateArray())
                                {
                                    string baslik = job.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? "Stajyer Pozisyonu" : "Stajyer Pozisyonu";

                                    string sirket = "Belirtilmemiş Şirket";
                                    if (job.TryGetProperty("company", out JsonElement companyProp) && companyProp.TryGetProperty("display_name", out JsonElement nameProp))
                                    {
                                        sirket = nameProp.GetString() ?? "Belirtilmemiş Şirket";
                                    }

                                    string aciklama = job.TryGetProperty("description", out JsonElement d) ? d.GetString() ?? "İlan açıklaması bulunmuyor." : "İlan açıklaması bulunmuyor";

                                    string sehir = "Küresel / Uzaktan";
                                    if (job.TryGetProperty("location", out JsonElement loc) && loc.TryGetProperty("area", out JsonElement areaArray) && areaArray.GetArrayLength() > 0)
                                    {
                                        sehir = areaArray[0].GetString() ?? "Uzaktan";
                                    }

                                    // Veritabanında aynı ilan var mı kontrolü
                                    var ilanVarMi = veriTabani.Ilanlar.Any(i => i.Baslik == baslik && i.SirketAdi == sirket);

                                    if (!ilanVarMi)
                                    {
                                        var yeniIlan = new Ilan
                                        {
                                            Baslik = baslik,
                                            SirketAdi = sirket,
                                            Aciklama = aciklama.Length > 400 ? aciklama.Substring(0, 400) + "..." : aciklama,
                                            Sehir = sehir,
                                            Maas = "Canlı Adzuna API",
                                            YayinlanmaTarihi = DateTime.UtcNow
                                        };

                                        veriTabani.Ilanlar.Add(yeniIlan);
                                        eklenenYeniIlanSayisi++;
                                    }
                                }

                                if (eklenenYeniIlanSayisi > 0)
                                {
                                    await veriTabani.SaveChangesAsync();
                                    Console.WriteLine($"[KÜRESEL MOTOR BAŞARILI]: Adzuna üzerinden {eklenenYeniIlanSayisi} adet yeni gerçek staj ilanı toplandı ve PostgreSQL'e yazıldı!");
                                }
                                else
                                {
                                    Console.WriteLine("[KÜRESEL MOTOR SAKİN]: Havuzda yeni bir ilan bulunamadı, mevcut verileriniz güncel.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[KÜRESEL MOTOR UYARISI]: API sunucusu yanıt vermedi. Durum Kodu: {cevap.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KÜRESEL MOTOR İÇ HATA]: Veriler işlenirken hata oluştu: {ex.Message}");
            }
        }
    }
}