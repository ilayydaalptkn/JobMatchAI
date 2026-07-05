using System;
using System.Collections.Generic;
using System.Linq;

public class AiMatcherService
{
    public (int Skor, string GeriBildirim, string NitelikOzeti) CVAnalizEt(string ilanAciklama, string ogrenciNitelikleri)
    {
        if (string.IsNullOrEmpty(ilanAciklama) || string.IsNullOrEmpty(ogrenciNitelikleri))
            return (0, "Analiz için yeterli veri bulunamadı.", "Eksik bilgi.");

        ilanAciklama = ilanAciklama.ToLower();
        ogrenciNitelikleri = ogrenciNitelikleri.ToLower();

        // 🎯 1. Kritik Teknolojiler & Yetenekler Havuzu (Ağırlıklı Puanlama)
        var yetenekHavuzu = new Dictionary<string, int>
        {
            { "c#", 20 }, { ".net", 20 }, { "asp.net", 15 },
            { "javascript", 15 }, { "typescript", 15 }, { "react", 20 },
            { "angular", 20 }, { "vue", 15 }, { "sql", 15 },
            { "mssql", 15 }, { "postgresql", 15 }, { "python", 15 },
            { "java", 20 }, { "spring", 20 }, { "css", 10 },
            { "tailwind", 10 }, { "html", 5 }, { "git", 10 },
            { "docker", 15 }, { "api", 10 }, { "rest", 10 }
        };

        int toplamIlanPuanı = 0;
        int kazanilanPuan = 0;
        List<string> eslesenYetenekler = new List<string>();
        List<string> eksikYetenekler = new List<string>();

        foreach (var yetenek in yetenekHavuzu)
        {
            // Eğer ilan bu yeteneği arıyorsa
            if (ilanAciklama.Contains(yetenek.Key))
            {
                toplamIlanPuanı += yetenek.Value;

                // Öğrencide bu yetenek var mı?
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

        // 📊 2. Skor Hesaplama Kriteri (Gelişmiş Algoritma)
        int temelSkor = toplamIlanPuanı > 0 ? (int)((double)kazanilanPuan / toplamIlanPuanı * 100) : 50;

        // Bonus puanlar (Yumuşak yetenekler ve adaptasyon analizi)
        if (ogrenciNitelikleri.Contains("staj") || ogrenciNitelikleri.Contains("proje")) temelSkor += 5;
        if (ogrenciNitelikleri.Contains("hızlı öğrenme") || ogrenciNitelikleri.Contains("takım")) temelSkor += 3;

        // Skoru 0 ile 100 arasında sınırla
        int nihaiSkor = Math.Clamp(temelSkor, 5, 100);

        // 📝 3. Detaylı Geri Bildirim Üretimi
        string geriBildirim = "";
        string nitelikOzeti = "";

        if (nihaiSkor >= 80)
        {
            geriBildirim = $"🚀 Mükemmel Eşleşme! CV'niz bu ilanda aranan temel kriterleri fazlasıyla karşılıyor. Özellikle {string.Join(", ", eslesenYetenekler)} konularındaki yetkinliğiniz harika.";
            nitelikOzeti = $"➕ Güçlü Yönler: Teknik uyum çok yüksek. Proje odaklı yaklaşım.\n❌ Zayıf Yönler: Belirgin bir eksiklik gözlenmedi.";
        }
        else if (nihaiSkor >= 50)
        {
            geriBildirim = $"⚖️ Dengeli Uyum. İlan için güzel bir potansiyeliniz var ancak kendinizi biraz daha geliştirmeniz gerekebilir. Eşleşen teknolojiler: {string.Join(", ", eslesenYetenekler)}.";
            string eksikKisim = eksikYetenekler.Count > 0 ? $"{string.Join(", ", eksikYetenekler)} konularına yoğunlaşabilirsiniz." : "Projelerinizi detaylandırabilirsiniz.";
            nitelikOzeti = $"➕ Güçlü Yönler: Temel programlama mantığı ve uyum.\n❌ Geliştirilmesi Gerekenler: {eksikKisim}";
        }
        else
        {
            geriBildirim = $"⚠️ Düşük Uyum. Bu ilan için teknik yetkinlikleriniz şu an biraz mesafeli duruyor. İlginizi çekiyorsa {string.Join(", ", eksikYetenekler)} teknolojilerine göz atmanızı öneririz.";
            nitelikOzeti = $"➕ Güçlü Yönler: Öğrenme isteği ve başvuru cesareti.\n❌ Kritik Eksikler: Aranan temel teknolojilerin ({string.Join(", ", eksikYetenekler)}) CV'de bulunamaması.";
        }

        return (nihaiSkor, geriBildirim, nitelikOzeti);
    }
}