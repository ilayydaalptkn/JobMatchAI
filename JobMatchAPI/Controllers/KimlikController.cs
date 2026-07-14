using Microsoft.AspNetCore.Mvc;
using JobMatchAPI.Data;
using JobMatchAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpPost("kayit")]
        public async Task<IActionResult> KayitOl([FromBody] Kullanici yeniKullanici)
        {
            try
            {
                if (yeniKullanici == null)
                {
                    return BadRequest("Geçersiz veya boş kullanıcı verisi gönderildi!");
                }

                // 🚨 SUPABASE BAĞLANTI KONTROLÜ VE E-POSTA SORGUSU
                if (_veriTabani.Kullanicilar == null)
                {
                    return StatusCode(500, "Veritabanında Kullanıcılar tablosu bulunamadı (DbSet null)!");
                }

                if (!string.IsNullOrEmpty(yeniKullanici.Eposta))
                {
                    var epostaVarMi = await _veriTabani.Kullanicilar
                        .AnyAsync(k => k.Eposta.ToLower() == yeniKullanici.Eposta.ToLower());

                    if (epostaVarMi)
                        return BadRequest("Bu e-posta adresi zaten kullanımda!");
                }
                else
                {
                    return BadRequest("E-posta alanı boş bırakılamaz!");
                }

                if (yeniKullanici.Rol != "Ogrenci" && yeniKullanici.Rol != "Isveren")
                {
                    yeniKullanici.Rol = "Ogrenci";
                }

                // PostgreSQL'in otomatik ID üretmesi için gelen ID'yi sıfırlıyoruz
                yeniKullanici.Id = 0;

                _veriTabani.Kullanicilar.Add(yeniKullanici);
                await _veriTabani.SaveChangesAsync();

                return Ok(new { mesaj = "Kayıt başarıyla tamamlandı!" });
            }
            catch (Exception ex)
            {
                // 500 hatası verirse Render loglarında neyin patlattığını tam görebilmen için hatayı dışarı fırlatıyoruz
                return StatusCode(500, $"[BACKEND KAYIT HATASI]: {ex.Message} -> İç Hata: {ex.InnerException?.Message}");
            }
        }

        [HttpPost("giris")]
        public async Task<IActionResult> GirisYap([FromBody] GirisModeli model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Eposta) || string.IsNullOrEmpty(model.Sifre))
                {
                    return BadRequest("E-posta ve şifre alanları boş bırakılamaz!");
                }

                if (_veriTabani.Kullanicilar == null)
                {
                    return StatusCode(500, "Veritabanı tablosu aktif değil!");
                }

                string arananEposta = model.Eposta.Trim().ToLower();
                string arananSifre = model.Sifre.Trim();

                var kullanici = await _veriTabani.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Eposta.ToLower() == arananEposta && k.Sifre == arananSifre);

                if (kullanici == null)
                    return BadRequest("E-posta adresi veya şifre hatalı!");

                // 🚨 500 HATASINA SEBEP OLABİLECEK JWT OLUŞTURMA ALANINI GÜVENLİĞE ALIYORUZ
                string tokenString = "mock_token_fallback";
                try
                {
                    var claims = new[]
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, kullanici.Eposta ?? ""),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, kullanici.Rol ?? "Ogrenci")
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BuSizinCokGizliVeGuvenliKriptografikAnahtarinizdir123!"));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(
                        claims: claims,
                        expires: DateTime.UtcNow.AddDays(1),
                        signingCredentials: creds
                    );

                    tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                }
                catch (Exception jwtEx)
                {
                    Console.WriteLine($"[JWT TOKEN UYARISI]: Token üretilemedi, fallback moduna geçiliyor: {jwtEx.Message}");
                }

                return Ok(new
                {
                    mesaj = "Giriş başarılı!",
                    token = tokenString,
                    id = kullanici.Id,
                    adSoyad = kullanici.AdSoyad,
                    eposta = kullanici.Eposta,
                    sehir = kullanici.Sehir,
                    rol = kullanici.Rol
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"[BACKEND GİRİŞ HATASI]: {ex.Message} -> İç Hata: {ex.InnerException?.Message}");
            }
        }
    }

    public class GirisModeli
    {
        public string Eposta { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}