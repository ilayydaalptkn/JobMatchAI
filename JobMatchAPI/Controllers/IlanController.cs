using JobMatchAPI.Data;
using JobMatchAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace JobMatchAPI.Controllers
{

    [ApiController]
    [Route("api/Ilan")] // JavaScript "api/Ilan" yazdığı için çift rota tanımlıyoruz
    public class IlanController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public IlanController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        // 1. Tüm İlanları Listeleme
        [HttpGet("listele")]
        public async Task<IActionResult> Listele()
        {
            try
            {
                if (_veriTabani.Ilanlar == null) return Ok(new List<object>());
                var ilanlar = await _veriTabani.Ilanlar.ToListAsync();
                return Ok(ilanlar);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }
        [HttpPost("basvuru-yap")]
        [Authorize]
        public async Task<IActionResult> BasvuruYap([FromBody] Basvuru yeniBasvuru)
        {
            // 1. İlanı ve Başvuru Yapan Öğrenciyi Veritabanından Buluyoruz
            var ilan = await _veriTabani.Ilanlar.FindAsync(yeniBasvuru.IlanId);
            var ogrenci = await _veriTabani.Kullanicilar.FindAsync(yeniBasvuru.KullaniciId);

            if (ilan == null || ogrenci == null)
            {
                return BadRequest("İlan veya kullanıcı bilgisi geçersiz.");
            }

            // 2. Yapay Zeka Servisini Ayağa Kaldırıyoruz
            var aiService = new AiMatcherService();

            // Öğrencinin CV niyetine geçen nitelik alanını (Örn: Ogrenci.Hakkinda veya Ogrenci.Yetenekler) gönderiyoruz
            // Eğer veritabanında kullanıcı tablosunda böyle bir alan yoksa şimdilik ogrenci.Sehir veya statik bir metin geçebilirsin
            string ogrenciDetaylari = ogrenci.AdSoyad + " " + (ogrenci.Sehir ?? "");

            // 🔥 AI MOTORU HESAPLIYOR
            var aiSonuc = aiService.CVAnalizEt(ilan.Aciklama, ogrenciDetaylari);

            // 3. AI Sonuçlarını Başvuru Nesnesine Gömüyoruz
            yeniBasvuru.AiSkoru = aiSonuc.Skor;
            yeniBasvuru.AiGeriBildirim = aiSonuc.GeriBildirim;
            yeniBasvuru.AiNitelikOzeti = aiSonuc.NitelikOzeti;
            yeniBasvuru.BasvuruTarihi = DateTime.UtcNow;
            yeniBasvuru.Durum = "Beklemede";

            // 4. Veritabanına Kayıt
            _veriTabani.Basvurular.Add(yeniBasvuru);
            await _veriTabani.SaveChangesAsync();

            return Ok(new
            {
                mesaj = "Başvuru ve Yapay Zeka Analizi Başarıyla Tamamlandı!",
                skor = aiSonuc.Skor,
                rapor = aiSonuc.GeriBildirim
            });
        }
        // 2. 🎯 JAVASCRIPT'İN ARADIĞI KRİTİK DETAY METODU (404 Hatasını Çözen Yer)
        [HttpGet("detay/{id}")]
        public async Task<IActionResult> GetIlanDetay(int id)
        {
            try
            {
                if (_veriTabani.Ilanlar == null) return NotFound("Tablo yok.");
                var ilan = await _veriTabani.Ilanlar.FindAsync(id);
                if (ilan == null) return NotFound($"İlan bulunamadı ID: {id}");
                return Ok(ilan);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

        // 3. 💼 İŞVERENİN KENDİ YAYINLADIĞI İLANLARI GETİRİR
        [HttpGet("isveren/{isverenId}")]
        public async Task<IActionResult> GetIsverenIlanlari(int isverenId)
        {
            try
            {
                if (_veriTabani.Ilanlar == null) return Ok(new List<object>());
                var ilanlar = await _veriTabani.Ilanlar.Where(i => i.KullaniciId == isverenId).ToListAsync();
                return Ok(ilanlar);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

        // 🎯 1. İŞVERENİN BAŞVURANLARI VE AI DETAYLARINI GÖRDÜĞÜ API
        [HttpGet("basvuranlar/{ilanId}")]
        public async Task<IActionResult> GetIlanBasvuranlar(int ilanId)
        {
            try
            {
                // Başvuruları çekerken Kullanici tablosunu dahil ediyoruz
                var basvuranAdaylar = await _veriTabani.Basvurular
                    .Include(b => b.Kullanici)
                    .Where(b => b.IlanId == ilanId)
                    .Select(b => new
                    {
                        Id = b.Kullanici != null ? b.Kullanici.Id : 0,
                        AdSoyad = b.Kullanici != null ? b.Kullanici.AdSoyad : "Bilinmeyen Aday",
                        Eposta = b.Kullanici != null ? b.Kullanici.Eposta : "E-posta Yok",
                        BasvuruId = b.Id,
                        Durum = b.Durum,

                        // 🧠 Ekrana yansıyacak Yapay Zeka Sonuçları Buraya Eklendi:
                        AiSkoru = b.AiSkoru ?? 0,
                        AiGeriBildirim = b.AiGeriBildirim ?? "Analiz henüz yapılmadı.",
                        AiNitelikOzeti = b.AiNitelikOzeti ?? "Özet bulunmuyor."
                    })
                    .ToListAsync();

                return Ok(basvuranAdaylar);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Hata: {ex.Message}");
            }
        }

        // 🎯 2. ÖĞRENCİNİN "BANA UYGUN İLANLAR" SEKMESİNDEKİ YAPAY ZEKA MOTORU
        [HttpGet("bana-uygun/{kullaniciId}")]
        public async Task<IActionResult> GetBanaUygunIlanlar(int kullaniciId)
        {
            try
            {
                if (_veriTabani.Ilanlar == null) return Ok(new List<object>());
                var ogrenci = await _veriTabani.Kullanicilar.FindAsync(kullaniciId);
                if (ogrenci == null) return NotFound("Öğrenci bulunamadı.");

                var tumIlanlar = await _veriTabani.Ilanlar.ToListAsync();
                var aiService = new AiMatcherService();

                // Öğrencinin profil metnini hazırlıyoruz (Eğer CV/Hakkında alanı varsa ekleyebilirsin)
                string ogrenciDetaylari = ogrenci.AdSoyad + " " + (ogrenci.Sehir ?? "");

                var eslesenIlanlar = tumIlanlar.Select(ilan => {
                    // 🧠 Her ilan için yapay zeka servisini dinamik olarak tetikliyoruz!
                    var aiSonuc = aiService.CVAnalizEt(ilan.Aciklama, ogrenciDetaylari);

                    return new
                    {
                        Id = ilan.Id,
                        Baslik = ilan.Baslik,
                        SirketAdi = ilan.SirketAdi,
                        Sehir = ilan.Sehir,
                        // JavaScript '%' işareti beklediği için string formatına çeviriyoruz
                        YapayZekaSkoru = $"%{aiSonuc.Skor}",
                        EslesmeNedeni = aiSonuc.GeriBildirim
                    };
                }).OrderByDescending(x => x.YapayZekaSkoru).ToList(); // En yüksek uyumludan en düşüğe sıralar

                return Ok(eslesenIlanlar);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

        // 6. 🌐 DIŞ API İLANLARI ENTEGRASYONU
        [HttpGet("dis-api-ilanlari")]
        public IActionResult GetDisApiIlanlari()
        {
            // JavaScript'teki disApiIlanlariniYukle() metodunun çökmemesi için simüle edilmiş küresel veriler
            var kureselIlanlar = new List<object> {
                new { id = 991, title = "Remote Full Stack Developer", company_name = "Arbeitnow Tech", location = "Uzaktan" },
                new { id = 992, title = "Backend Intern (.NET)", company_name = "Berlin Software House", location = "Almanya" }
            };
            return Ok(kureselIlanlar);
        }

        // 7. 🤖 BOT İLE WEB KAZIMA ENTEGRASYONU
        [HttpGet("bot-ile-kazici")]
        public IActionResult GetBotIlanlari()
        {
            // JavaScript'teki botIlanlariniYukle() metodunun aradığı veriler
            var botIlanlar = new List<object> {
                new { id = 881, baslik = "Cyber Security Trainee", sirketAdi = "Global Tech Labs", sehir = "Ankara", yapayZekaSkoru = "%92", eslesmeNedeni = "Bot kazıyıcı verisi: Siber güvenlik alanına ilgi duyuyorsunuz." }
            };
            return Ok(botIlanlar);
        }

        // 8. 🔄 BAŞVURUYU ONAYLAMA VEYA REDDETME METODU
        [HttpPost("durum-guncelle")]
        public async Task<IActionResult> DurumGuncelle([FromBody] DurumGuncelleModel model)
        {
            try
            {
                var basvuru = await _veriTabani.Basvurular.FindAsync(model.BasvuruId);
                if (basvuru == null) return NotFound("Başvuru bulunamadı.");

                basvuru.Durum = model.YeniDurum; // 'Onaylandi' veya 'Reddedildi'
                await _veriTabani.SaveChangesAsync();

                return Ok(new { mesaj = "Başvuru durumu başarıyla güncellendi." });
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }
        [HttpPost("ekle")]
        public async Task<IActionResult> Ekle([FromBody] JobMatchAPI.Models.Ilan yeniIlan)
        {
            try
            {
                if (yeniIlan == null)
                {
                    return BadRequest("İlan verisi boş olamaz.");
                }

                // Id'yi veritabanının otomatik üretmesi için sıfırlıyoruz
                yeniIlan.Id = 0;
                yeniIlan.YayinlanmaTarihi = DateTime.UtcNow;

                _veriTabani.Ilanlar.Add(yeniIlan);
                await _veriTabani.SaveChangesAsync();

                return Ok(yeniIlan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veritabanına kaydedilirken hata oluştu: {ex.Message}");
            }
        }
        // 1. İLAN SİLME API'SI
        [HttpDelete("sil/{id}")]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                var ilan = await _veriTabani.Ilanlar.FindAsync(id);
                if (ilan == null) return NotFound("İlan bulunamadı.");

                // İlana yapılan başvuruları da temizleyelim ki veritabanı hata vermesin
                var basvurular = _veriTabani.Basvurular.Where(b => b.IlanId == id);
                _veriTabani.Basvurular.RemoveRange(basvurular);

                _veriTabani.Ilanlar.Remove(ilan);
                await _veriTabani.SaveChangesAsync();
                return Ok("İlan ve ilgili başvurular başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Silme hatası: {ex.Message}");
            }
        }

        // 2. İLAN GÜNCELLEME API'SI
        [HttpPut("guncelle/{id}")]
        public async Task<IActionResult> Guncelle(int id, [FromBody] JobMatchAPI.Models.Ilan guncelIlan)
        {
            try
            {
                var ilan = await _veriTabani.Ilanlar.FindAsync(id);
                if (ilan == null) return NotFound("İlan bulunamadı.");

                ilan.Baslik = guncelIlan.Baslik;
                ilan.Aciklama = guncelIlan.Aciklama;
                ilan.Sehir = guncelIlan.Sehir;
                ilan.Maas = guncelIlan.Maas;

                await _veriTabani.SaveChangesAsync();
                return Ok(ilan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Güncelleme hatası: {ex.Message}");
            }
        }


        public class DurumGuncelleModel
        {
            public int BasvuruId { get; set; }
            public string YeniDurum { get; set; }
        }
    }
}

