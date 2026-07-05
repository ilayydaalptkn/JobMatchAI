using Microsoft.AspNetCore.Mvc;
using JobMatchAPI.Data;
using JobMatchAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Kimlik")]
    public class KimlikController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public KimlikController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        // 1. KAYIT OLMA SİSTEMİ (POST: api/Kimlik/kayit)
        [HttpPost("kayit")]
        public async Task<IActionResult> KayitOl([FromBody] Kullanici yeniKullanici)
        {
            try
            {
                if (yeniKullanici == null || string.IsNullOrEmpty(yeniKullanici.Eposta))
                {
                    return BadRequest("Geçersiz kullanıcı verisi!");
                }

                // E-posta adresi sistemde zaten var mı kontrolü
                var epostaVarMi = await _veriTabani.Kullanicilar.AnyAsync(k => k.Eposta.ToLower() == yeniKullanici.Eposta.ToLower());
                if (epostaVarMi) return BadRequest("Bu e-posta adresi zaten kullanımda!");

                // Rol doğrulama (Güvenlik kontrolü)
                if (yeniKullanici.Rol != "Ogrenci" && yeniKullanici.Rol != "Isveren")
                {
                    yeniKullanici.Rol = "Ogrenci"; // Hatalı bir şey gelirse varsayılan öğrenci atansın
                }

                _veriTabani.Kullanicilar.Add(yeniKullanici);
                await _veriTabani.SaveChangesAsync();

                return Ok(new { mesaj = "Kayıt başarıyla tamamlandı!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Kayıt sırasında teknik bir hata oluştu: {ex.Message}");
            }
        }

        // 2. GİRİŞ YAPMA SİSTEMİ (POST: api/Kimlik/giris)
        // ÇÖZÜM: Çift olan HttpPost kaldırıldı, tek satıra düşürüldü.
        [HttpPost("giris")]
        public async Task<IActionResult> GirisYap([FromBody] GirisModeli model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Eposta) || string.IsNullOrEmpty(model.Sifre))
                {
                    return BadRequest("E-posta ve şifre alanları boş bırakılamaz!");
                }

                string arananEposta = model.Eposta.Trim().ToLower();
                string arananSifre = model.Sifre.Trim();

                // Veritabanındaki pürüzsüz Türkçe kolon eşleşmesi
                var kullanici = await _veriTabani.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Eposta.ToLower() == arananEposta && k.Sifre == arananSifre);

                if (kullanici == null)
                    return BadRequest("E-posta adresi veya şifre hatalı!");

                return Ok(new
                {
                    mesaj = "Giriş başarılı!",
                    id = kullanici.Id,
                    adSoyad = kullanici.AdSoyad,
                    eposta = kullanici.Eposta,
                    sehir = kullanici.Sehir,
                    rol = kullanici.Rol
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Giriş hatası: {ex.Message}");
            }
        }
    }

    // ÇÖZÜM: Karmaşıklaşan yardımcı sınıf tamamen temiz ve %100 Türkçe string alanlara dönüştürüldü.
    public class GirisModeli
    {
        public string Eposta { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}