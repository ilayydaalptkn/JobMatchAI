using JobMatchAPI.Data;
using JobMatchAPI.Helpers;
using JobMatchAPI.Models;
using JobMatchAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Ilan")]
    public class IlanController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;
        private readonly AiMatcherService _aiService = new();

        public IlanController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        [HttpGet("listele")]
        [AllowAnonymous]
        public async Task<IActionResult> Listele([FromQuery] int? kullaniciId)
        {
            var ilanlar = await _veriTabani.Ilanlar.OrderByDescending(i => i.YayinlanmaTarihi).ToListAsync();

            if (kullaniciId.HasValue)
            {
                var ogrenci = await _veriTabani.Kullanicilar.FindAsync(kullaniciId.Value);
                if (ogrenci != null)
                {
                    var cvMetni = AiMatcherService.CvMetniOlustur(ogrenci);
                    ilanlar = ilanlar.Select(ilan =>
                    {
                        var sonuc = _aiService.CVAnalizEt(ilan.Aciklama + " " + ilan.Baslik, cvMetni);
                        ilan.YapayZekaSkoru = sonuc.Skor;
                        ilan.EslesmeNedeni = sonuc.GeriBildirim;
                        return ilan;
                    }).ToList();
                }
            }

            return Ok(ilanlar);
        }

        [HttpPost("basvuru-yap")]
        [Authorize(Roles = "Ogrenci")]
        public async Task<IActionResult> BasvuruYap([FromBody] BasvuruYapModeli model)
        {
            var ogrenciId = User.KullaniciIdAl();
            var ilan = await _veriTabani.Ilanlar.FindAsync(model.IlanId);
            var ogrenci = await _veriTabani.Kullanicilar.FindAsync(ogrenciId);

            if (ilan == null || ogrenci == null)
                return BadRequest("İlan veya kullanıcı bilgisi geçersiz.");

            if (string.IsNullOrWhiteSpace(ogrenci.CvHedefIs) || string.IsNullOrWhiteSpace(ogrenci.CvOkul))
                return BadRequest("Başvuru yapmadan önce CV profilinizi tamamlayın.");

            var mevcutBasvuru = await _veriTabani.Basvurular
                .AnyAsync(b => b.IlanId == model.IlanId && b.KullaniciId == ogrenciId);

            if (mevcutBasvuru)
                return BadRequest("Bu ilana zaten başvurdunuz.");

            var cvMetni = AiMatcherService.CvMetniOlustur(ogrenci);
            var aiSonuc = _aiService.CVAnalizEt(ilan.Aciklama + " " + ilan.Baslik, cvMetni);

            var yeniBasvuru = new Basvuru
            {
                KullaniciId = ogrenciId,
                IlanId = model.IlanId,
                AiSkoru = aiSonuc.Skor,
                AiGeriBildirim = aiSonuc.GeriBildirim,
                AiNitelikOzeti = aiSonuc.NitelikOzeti,
                BasvuruTarihi = DateTime.UtcNow,
                Durum = "Beklemede"
            };

            _veriTabani.Basvurular.Add(yeniBasvuru);
            await _veriTabani.SaveChangesAsync();

            return Ok(new { mesaj = "Başvuru ve AI analizi tamamlandı!", skor = aiSonuc.Skor, rapor = aiSonuc.GeriBildirim });
        }

        [HttpGet("detay/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetIlanDetay(int id)
        {
            var ilan = await _veriTabani.Ilanlar.FindAsync(id);
            if (ilan == null) return NotFound($"İlan bulunamadı ID: {id}");
            return Ok(ilan);
        }

        [HttpGet("isveren/{isverenId}")]
        [Authorize(Roles = "Isveren")]
        public async Task<IActionResult> GetIsverenIlanlari(int isverenId)
        {
            if (User.KullaniciIdAl() != isverenId)
                return Forbid();

            var ilanlar = await _veriTabani.Ilanlar
                .Where(i => i.KullaniciId == isverenId)
                .OrderByDescending(i => i.YayinlanmaTarihi)
                .ToListAsync();

            return Ok(ilanlar);
        }

        [HttpGet("basvuranlar/{ilanId}")]
        [Authorize(Roles = "Isveren")]
        public async Task<IActionResult> GetIlanBasvuranlar(int ilanId)
        {
            var ilan = await _veriTabani.Ilanlar.FindAsync(ilanId);
            if (ilan == null) return NotFound("İlan bulunamadı.");

            if (ilan.KullaniciId != User.KullaniciIdAl())
                return Forbid();

            var basvuranAdaylar = await _veriTabani.Basvurular
                .Include(b => b.Kullanici)
                .Where(b => b.IlanId == ilanId)
                .OrderByDescending(b => b.AiSkoru)
                .Select(b => new
                {
                    Id = b.Kullanici != null ? b.Kullanici.Id : 0,
                    AdSoyad = b.Kullanici != null ? b.Kullanici.AdSoyad : "Bilinmeyen Aday",
                    Eposta = b.Kullanici != null ? b.Kullanici.Eposta : "",
                    Telefon = b.Kullanici != null ? b.Kullanici.Telefon : "",
                    CvOkul = b.Kullanici != null ? b.Kullanici.CvOkul : "",
                    CvHedefIs = b.Kullanici != null ? b.Kullanici.CvHedefIs : "",
                    CvSehir = b.Kullanici != null ? b.Kullanici.CvSehir : "",
                    CvYetenekler = b.Kullanici != null ? b.Kullanici.CvYetenekler : "[]",
                    CvTecrubeler = b.Kullanici != null ? b.Kullanici.CvTecrubeler : "[]",
                    BasvuruId = b.Id,
                    Durum = b.Durum,
                    AiSkoru = b.AiSkoru ?? 0,
                    AiGeriBildirim = b.AiGeriBildirim ?? "",
                    AiNitelikOzeti = b.AiNitelikOzeti ?? ""
                }).ToListAsync();

            return Ok(basvuranAdaylar);
        }

        [HttpGet("bana-uygun/{kullaniciId}")]
        [Authorize(Roles = "Ogrenci")]
        public async Task<IActionResult> GetBanaUygunIlanlar(int kullaniciId)
        {
            if (User.KullaniciIdAl() != kullaniciId)
                return Forbid();

            var ogrenci = await _veriTabani.Kullanicilar.FindAsync(kullaniciId);
            if (ogrenci == null) return NotFound("Öğrenci bulunamadı.");

            var cvMetni = AiMatcherService.CvMetniOlustur(ogrenci);
            var tumIlanlar = await _veriTabani.Ilanlar.ToListAsync();

            var eslesenIlanlar = tumIlanlar.Select(ilan =>
            {
                var aiSonuc = _aiService.CVAnalizEt(ilan.Aciklama + " " + ilan.Baslik, cvMetni);
                return new
                {
                    ilan.Id,
                    ilan.Baslik,
                    ilan.SirketAdi,
                    ilan.Sehir,
                    ilan.Aciklama,
                    ilan.Maas,
                    HamSkor = aiSonuc.Skor,
                    EslesmeNedeni = aiSonuc.GeriBildirim
                };
            })
            .OrderByDescending(x => x.HamSkor)
            .Select(x => new
            {
                x.Id,
                x.Baslik,
                x.SirketAdi,
                x.Sehir,
                x.Aciklama,
                x.Maas,
                YapayZekaSkoru = $"%{x.HamSkor}",
                x.EslesmeNedeni
            }).ToList();

            return Ok(eslesenIlanlar);
        }

        [HttpPost("durum-guncelle")]
        [Authorize(Roles = "Isveren")]
        public async Task<IActionResult> DurumGuncelle([FromBody] DurumGuncelleModel model)
        {
            var basvuru = await _veriTabani.Basvurular
                .Include(b => b.Kullanici)
                .FirstOrDefaultAsync(b => b.Id == model.BasvuruId);

            if (basvuru == null) return NotFound("Başvuru bulunamadı.");

            var ilan = await _veriTabani.Ilanlar.FindAsync(basvuru.IlanId);
            if (ilan?.KullaniciId != User.KullaniciIdAl())
                return Forbid();

            if (model.YeniDurum is not ("Onaylandi" or "Reddedildi" or "Beklemede"))
                return BadRequest("Geçersiz durum değeri.");

            basvuru.Durum = model.YeniDurum;
            await _veriTabani.SaveChangesAsync();

            return Ok(new { mesaj = "Başvuru durumu güncellendi.", durum = basvuru.Durum });
        }

        [HttpPost("ekle")]
        [Authorize(Roles = "Isveren")]
        public async Task<IActionResult> Ekle([FromBody] IlanEkleModeli model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isveren = await _veriTabani.Kullanicilar.FindAsync(User.KullaniciIdAl());
            if (isveren == null) return Unauthorized();

            var yeniIlan = new Ilan
            {
                Baslik = model.Baslik.Trim(),
                Aciklama = model.Aciklama.Trim(),
                Sehir = model.Sehir.Trim(),
                Maas = string.IsNullOrWhiteSpace(model.Maas) ? "Belirtilmemiş" : model.Maas.Trim(),
                SirketAdi = isveren.AdSoyad,
                KullaniciId = isveren.Id,
                YayinlanmaTarihi = DateTime.UtcNow
            };

            _veriTabani.Ilanlar.Add(yeniIlan);
            await _veriTabani.SaveChangesAsync();
            return Ok(yeniIlan);
        }

        [HttpDelete("sil/{id}")]
        [Authorize(Roles = "Isveren")]
        public async Task<IActionResult> Sil(int id)
        {
            var ilan = await _veriTabani.Ilanlar.FindAsync(id);
            if (ilan == null) return NotFound("İlan bulunamadı.");

            if (ilan.KullaniciId != User.KullaniciIdAl())
                return Forbid();

            var basvurular = _veriTabani.Basvurular.Where(b => b.IlanId == id);
            _veriTabani.Basvurular.RemoveRange(basvurular);
            _veriTabani.Ilanlar.Remove(ilan);
            await _veriTabani.SaveChangesAsync();

            return Ok(new { mesaj = "İlan ve ilgili başvurular silindi." });
        }

        [HttpPut("guncelle/{id}")]
        [Authorize(Roles = "Isveren")]
        public async Task<IActionResult> Guncelle(int id, [FromBody] IlanGuncelleModeli model)
        {
            var ilan = await _veriTabani.Ilanlar.FindAsync(id);
            if (ilan == null) return NotFound("İlan bulunamadı.");

            if (ilan.KullaniciId != User.KullaniciIdAl())
                return Forbid();

            ilan.Baslik = model.Baslik.Trim();
            ilan.Aciklama = model.Aciklama.Trim();
            ilan.Sehir = model.Sehir.Trim();
            ilan.Maas = model.Maas.Trim();

            await _veriTabani.SaveChangesAsync();
            return Ok(ilan);
        }

        public class BasvuruYapModeli
        {
            [Required]
            public int IlanId { get; set; }
        }

        public class DurumGuncelleModel
        {
            [Required]
            public int BasvuruId { get; set; }

            [Required]
            public string YeniDurum { get; set; } = string.Empty;
        }

        public class IlanEkleModeli
        {
            [Required, MinLength(3)]
            public string Baslik { get; set; } = string.Empty;

            [Required]
            public string Aciklama { get; set; } = string.Empty;

            [Required]
            public string Sehir { get; set; } = string.Empty;

            public string Maas { get; set; } = string.Empty;
        }

        public class IlanGuncelleModeli
        {
            [Required, MinLength(3)]
            public string Baslik { get; set; } = string.Empty;

            [Required]
            public string Aciklama { get; set; } = string.Empty;

            [Required]
            public string Sehir { get; set; } = string.Empty;

            [Required]
            public string Maas { get; set; } = string.Empty;
        }
    }
}
