using JobMatchAPI.Data;
using JobMatchAPI.Helpers;
using JobMatchAPI.Models;
using JobMatchAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Cv")]
    [Authorize]
    public class CvController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public CvController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        [HttpGet("profil")]
        public async Task<IActionResult> ProfilGetir()
        {
            var kullanici = await KullaniciGetir();
            if (kullanici == null) return NotFound("Kullanıcı bulunamadı.");

            return Ok(CvDtoOlustur(kullanici));
        }

        [HttpPut("profil")]
        public async Task<IActionResult> ProfilGuncelle([FromBody] CvGuncelleModeli model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var kullanici = await KullaniciGetir();
            if (kullanici == null) return NotFound("Kullanıcı bulunamadı.");

            if (User.RolAl() != "Ogrenci")
                return Forbid();

            kullanici.Telefon = model.Telefon.Trim();
            kullanici.CvSehir = model.CvSehir.Trim();
            kullanici.CvOkul = model.CvOkul.Trim();
            kullanici.CvHedefIs = model.CvHedefIs.Trim();
            kullanici.CvYetenekler = JsonSerializer.Serialize(model.CvYetenekler ?? new List<string>());
            kullanici.CvTecrubeler = JsonSerializer.Serialize(model.CvTecrubeler ?? new List<string>());

            await _veriTabani.SaveChangesAsync();
            return Ok(new { mesaj = "CV profili güncellendi.", profil = CvDtoOlustur(kullanici) });
        }

        private async Task<Kullanici?> KullaniciGetir() =>
            await _veriTabani.Kullanicilar.FindAsync(User.KullaniciIdAl());

        private static object CvDtoOlustur(Kullanici k) => new
        {
            k.Id,
            k.AdSoyad,
            k.Eposta,
            k.Telefon,
            k.Sehir,
            k.Rol,
            k.CvOkul,
            k.CvHedefIs,
            k.CvSehir,
            CvYetenekler = AiMatcherService.CvJsonAyristir(k.CvYetenekler),
            CvTecrubeler = AiMatcherService.CvJsonAyristir(k.CvTecrubeler)
        };
    }

    public class CvGuncelleModeli
    {
        [Required, MinLength(10)]
        public string Telefon { get; set; } = string.Empty;

        [Required]
        public string CvSehir { get; set; } = string.Empty;

        [Required]
        public string CvOkul { get; set; } = string.Empty;

        [Required]
        public string CvHedefIs { get; set; } = string.Empty;

        public List<string>? CvYetenekler { get; set; }
        public List<string>? CvTecrubeler { get; set; }
    }
}
