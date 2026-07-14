using Microsoft.AspNetCore.Mvc;
using JobMatchAPI.Data;
using JobMatchAPI.Models;
using JobMatchAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Kimlik")]
    public class KimlikController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;
        private readonly JwtServisi _jwtServisi;

        public KimlikController(VeriTabaniBaglantisi veriTabani, JwtServisi jwtServisi)
        {
            _veriTabani = veriTabani;
            _jwtServisi = jwtServisi;
        }

        [HttpPost("kayit")]
        public async Task<IActionResult> KayitOl([FromBody] KayitModeli model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var epostaVarMi = await _veriTabani.Kullanicilar
                .AnyAsync(k => k.Eposta.ToLower() == model.Eposta.ToLower());

            if (epostaVarMi)
                return BadRequest("Bu e-posta adresi zaten kullanımda!");

            var rol = model.Rol is "Ogrenci" or "Isveren" ? model.Rol : "Ogrenci";

            var yeniKullanici = new Kullanici
            {
                AdSoyad = model.AdSoyad.Trim(),
                Eposta = model.Eposta.Trim().ToLower(),
                Telefon = model.Telefon.Trim(),
                Sifre = SifreServisi.Hashle(model.Sifre),
                Sehir = model.Sehir.Trim(),
                Rol = rol,
                KayitTarihi = DateTime.UtcNow
            };

            _veriTabani.Kullanicilar.Add(yeniKullanici);
            await _veriTabani.SaveChangesAsync();

            return Ok(new { mesaj = "Kayıt başarıyla tamamlandı!" });
        }

        [HttpPost("giris")]
        public async Task<IActionResult> GirisYap([FromBody] GirisModeli model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var kullanici = await _veriTabani.Kullanicilar
                .FirstOrDefaultAsync(k => k.Eposta.ToLower() == model.Eposta.Trim().ToLower());

            if (kullanici == null || !SifreServisi.Dogrula(model.Sifre.Trim(), kullanici.Sifre))
                return BadRequest("E-posta adresi veya şifre hatalı!");

            if (SifreServisi.HashGerekliMi(kullanici.Sifre))
            {
                kullanici.Sifre = SifreServisi.Hashle(model.Sifre.Trim());
                await _veriTabani.SaveChangesAsync();
            }

            return Ok(new
            {
                mesaj = "Giriş başarılı!",
                token = _jwtServisi.TokenUret(kullanici),
                id = kullanici.Id,
                adSoyad = kullanici.AdSoyad,
                eposta = kullanici.Eposta,
                telefon = kullanici.Telefon,
                sehir = kullanici.Sehir,
                rol = kullanici.Rol
            });
        }
    }

    public class KayitModeli
    {
        [Required, MinLength(2)]
        public string AdSoyad { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Eposta { get; set; } = string.Empty;

        [Required, MinLength(10)]
        public string Telefon { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Sifre { get; set; } = string.Empty;

        [Required]
        public string Sehir { get; set; } = string.Empty;

        public string Rol { get; set; } = "Ogrenci";
    }

    public class GirisModeli
    {
        [Required, EmailAddress]
        public string Eposta { get; set; } = string.Empty;

        [Required]
        public string Sifre { get; set; } = string.Empty;
    }
}
