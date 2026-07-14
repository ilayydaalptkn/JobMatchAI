namespace JobMatchAPI.Services
{
    public class AiMatcherService
    {
        public static string CvMetniOlustur(Models.Kullanici kullanici)
        {
            var yetenekler = CvJsonAyristir(kullanici.CvYetenekler);
            var tecrubeler = CvJsonAyristir(kullanici.CvTecrubeler);

            return string.Join(" ", new[]
            {
                kullanici.AdSoyad,
                kullanici.Sehir,
                kullanici.CvSehir,
                kullanici.CvOkul,
                kullanici.CvHedefIs,
                string.Join(" ", yetenekler),
                string.Join(" ", tecrubeler)
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        public static List<string> CvJsonAyristir(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return json.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
        }

        public (int Skor, string GeriBildirim, string NitelikOzeti) CVAnalizEt(string ilanAciklama, string ogrenciNitelikleri)
        {
            if (string.IsNullOrEmpty(ilanAciklama) || string.IsNullOrEmpty(ogrenciNitelikleri))
                return (0, "Analiz için yeterli veri bulunamadı.", "Eksik bilgi.");

            ilanAciklama = ilanAciklama.ToLower();
            ogrenciNitelikleri = ogrenciNitelikleri.ToLower();

            var yetenekHavuzu = new Dictionary<string, int>
            {
                { "c#", 20 }, { ".net", 20 }, { "asp.net", 15 },
                { "javascript", 15 }, { "typescript", 15 }, { "react", 20 },
                { "angular", 20 }, { "vue", 15 }, { "sql", 15 },
                { "postgresql", 15 }, { "python", 15 }, { "java", 20 },
                { "spring", 20 }, { "css", 10 }, { "tailwind", 10 },
                { "html", 5 }, { "git", 10 }, { "docker", 15 },
                { "api", 10 }, { "rest", 10 }, { "excel", 10 },
                { "garson", 15 }, { "servis", 10 }, { "ofis", 10 }
            };

            int toplamIlanPuani = 0;
            int kazanilanPuan = 0;
            var eslesenYetenekler = new List<string>();
            var eksikYetenekler = new List<string>();

            foreach (var yetenek in yetenekHavuzu)
            {
                if (ilanAciklama.Contains(yetenek.Key))
                {
                    toplamIlanPuani += yetenek.Value;
                    if (ogrenciNitelikleri.Contains(yetenek.Key))
                    {
                        kazanilanPuan += yetenek.Value;
                        eslesenYetenekler.Add(yetenek.Key.ToUpper());
                    }
                    else
                    {
                        eksikYetenekler.Add(yetenek.Key.ToUpper());
                    }
                }
            }

            int temelSkor = toplamIlanPuani > 0 ? (int)((double)kazanilanPuan / toplamIlanPuani * 100) : 50;

            if (ogrenciNitelikleri.Contains("staj") || ogrenciNitelikleri.Contains("proje")) temelSkor += 5;
            if (ogrenciNitelikleri.Contains("hızlı öğrenme") || ogrenciNitelikleri.Contains("takım")) temelSkor += 3;

            int nihaiSkor = Math.Clamp(temelSkor, 5, 100);

            string geriBildirim;
            string nitelikOzeti;

            if (nihaiSkor >= 80)
            {
                geriBildirim = $"Mükemmel eşleşme! CV'niz bu ilanda aranan kriterleri karşılıyor. Güçlü alanlar: {string.Join(", ", eslesenYetenekler)}.";
                nitelikOzeti = "Güçlü Yönler: Teknik uyum yüksek.\nGeliştirilmesi Gerekenler: Belirgin eksik yok.";
            }
            else if (nihaiSkor >= 50)
            {
                geriBildirim = $"Dengeli uyum. Eşleşen teknolojiler: {string.Join(", ", eslesenYetenekler)}.";
                var eksikKisim = eksikYetenekler.Count > 0 ? $"{string.Join(", ", eksikYetenekler)} konularına odaklanabilirsiniz." : "Projelerinizi detaylandırabilirsiniz.";
                nitelikOzeti = $"Güçlü Yönler: Temel uyum mevcut.\nGeliştirilmesi Gerekenler: {eksikKisim}";
            }
            else
            {
                geriBildirim = $"Düşük uyum. Önerilen gelişim alanları: {string.Join(", ", eksikYetenekler)}.";
                nitelikOzeti = $"Güçlü Yönler: Başvuru motivasyonu.\nKritik Eksikler: {string.Join(", ", eksikYetenekler)}.";
            }

            return (nihaiSkor, geriBildirim, nitelikOzeti);
        }
    }
}
