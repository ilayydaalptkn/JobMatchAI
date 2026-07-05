using JobMatchAPI.Data;
using JobMatchAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Ilan")]
    public class IlanController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public IlanController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

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
        public async Task<IActionResult> BasvuruYap([FromBody] Basvuru yeniBasvuru)
        {
            try
            {
                var ilan = await _veriTabani.Ilanlar.FindAsync(yeniBasvuru.IlanId);
                var ogrenci = await _veriTabani.Kullanicilar.FindAsync(yeniBasvuru.KullaniciId);

                if (ilan == null || ogrenci == null) return BadRequest("İlan veya kullanıcı bilgisi geçersiz.");

                var aiService = new AiMatcherService();
                string ogrenciDetaylari = ogrenci.AdSoyad + " " + (ogrenci.Sehir ?? "");
                var aiSonuc = aiService.CVAnalizEt(ilan.Aciklama, ogrenciDetaylari);

                yeniBasvuru.AiSkoru = aiSonuc.Skor;
                yeniBasvuru.AiGeriBildirim = aiSonuc.GeriBildirim;
                yeniBasvuru.AiNitelikOzeti = aiSonuc.NitelikOzeti;
                yeniBasvuru.BasvuruTarihi = DateTime.UtcNow;
                yeniBasvuru.Durum = "Beklemede";

                _veriTabani.Basvurular.Add(yeniBasvuru);
                await _veriTabani.SaveChangesAsync();

                return Ok(new { mesaj = "Başvuru ve Yapay Zeka Analizi Başarıyla Tamamlandı!", skor = aiSonuc.Skor, rapor = aiSonuc.GeriBildirim });
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

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

        [HttpGet("basvuranlar/{ilanId}")]
        public async Task<IActionResult> GetIlanBasvuranlar(int ilanId)
        {
            try
            {
                var basvuranAdaylar = await _veriTabani.Basvurular
                    .Include(b => b.Kullanici)
                    .Where(b => b.IlanId == ilanId)
                    .Select(b => new {
                        Id = b.Kullanici != null ? b.Kullanici.Id : 0,
                        AdSoyad = b.Kullanici != null ? b.Kullanici.AdSoyad : "Bilinmeyen Aday",
                        Eposta = b.Kullanici != null ? b.Kullanici.Eposta : "E-posta Yok",
                        BasvuruId = b.Id,
                        Durum = b.Durum,
                        AiSkoru = b.AiSkoru ?? 0,
                        AiGeriBildirim = b.AiGeriBildirim ?? "Analiz henüz yapılmadı.",
                        AiNitelikOzeti = b.AiNitelikOzeti ?? "Özet bulunmuyor."
                    }).ToListAsync();

                return Ok(basvuranAdaylar);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

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
                string ogrenciDetaylari = ogrenci.AdSoyad + " " + (ogrenci.Sehir ?? "");

                var eslesenIlanlar = tumIlanlar.Select(ilan => {
                    var aiSonuc = aiService.CVAnalizEt(ilan.Aciklama, ogrenciDetaylari);
                    return new
                    {
                        Id = ilan.Id,
                        Baslik = ilan.Baslik,
                        SirketAdi = ilan.SirketAdi,
                        Sehir = ilan.Sehir,
                        HamSkor = aiSonuc.Skor,
                        EslesmeNedeni = aiSonuc.GeriBildirim
                    };
                })
                .OrderByDescending(x => x.HamSkor)
                .Select(x => new {
                    x.Id,
                    x.Baslik,
                    x.SirketAdi,
                    x.Sehir,
                    YapayZekaSkoru = $"%{x.HamSkor}",
                    x.EslesmeNedeni
                }).ToList();

                return Ok(eslesenIlanlar);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

        [HttpGet("dis-api-ilanlari")]
        public IActionResult GetDisApiIlanlari()
        {
            var kureselIlanlar = new List<object> {
                new { id = 991, title = "Remote Full Stack Developer", company_name = "Arbeitnow Tech", location = "Uzaktan" },
                new { id = 992, title = "Backend Intern (.NET)", company_name = "Berlin Software House", location = "Almanya" }
            };
            return Ok(kureselIlanlar);
        }

        [HttpGet("bot-ile-kazici")]
        public IActionResult GetBotIlanlari()
        {
            var botIlanlar = new List<object> {
                new { id = 881, baslik = "Cyber Security Trainee", sirketAdi = "Global Tech Labs", sehir = "Ankara", yapayZekaSkoru = "%92", eslesmeNedeni = "Bot kazıyıcı verisi." }
            };
            return Ok(botIlanlar);
        }

        [HttpPost("durum-guncelle")]
        public async Task<IActionResult> DurumGuncelle([FromBody] DurumGuncelleModel model)
        {
            try
            {
                var basvuru = await _veriTabani.Basvurular.FindAsync(model.BasvuruId);
                if (basvuru == null) return NotFound("Başvuru bulunamadı.");

                basvuru.Durum = model.YeniDurum;
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
                if (yeniIlan == null) return BadRequest("İlan verisi boş olamaz.");
                yeniIlan.Id = 0;
                yeniIlan.YayinlanmaTarihi = DateTime.UtcNow;

                _veriTabani.Ilanlar.Add(yeniIlan);
                await _veriTabani.SaveChangesAsync();
                return Ok(yeniIlan);
            }
            catch (Exception ex) { return StatusCode(500, $"Veritabanına kaydedilirken hata oluştu: {ex.Message}"); }
        }

        [HttpDelete("sil/{id}")]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                var ilan = await _veriTabani.Ilanlar.FindAsync(id);
                if (ilan == null) return NotFound("İlan bulunamadı.");

                var basvurular = _veriTabani.Basvurular.Where(b => b.IlanId == id);
                _veriTabani.Basvurular.RemoveRange(basvurular);

                _veriTabani.Ilanlar.Remove(ilan);
                await _veriTabani.SaveChangesAsync();
                return Ok("İlan ve ilgili başvurular başarıyla silindi.");
            }
            catch (Exception ex) { return StatusCode(500, $"Silme hatası: {ex.Message}"); }
        }

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
            catch (Exception ex) { return StatusCode(500, $"Güncelleme hatası: {ex.Message}"); }
        }

        public class DurumGuncelleModel
        {
            public int BasvuruId { get; set; }
            public string YeniDurum { get; set; }
        }
    }
}