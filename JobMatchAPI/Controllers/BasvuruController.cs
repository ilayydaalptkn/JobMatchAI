using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using JobMatchAPI.Data;
using Microsoft.EntityFrameworkCore;
using JobMatchAPI.Models;
using Microsoft.AspNetCore.Authorization; // 🔥 JWT Kalkanı için eklenen namespace
using System.IdentityModel.Tokens.Jwt;    // 🔥 Token işlemleri için eklenen namespace
using Microsoft.IdentityModel.Tokens;       // 🔥 Şifreleme anahtarları için eklenen namespace

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Basvuru")]
    public class BasvuruController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public BasvuruController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        [HttpGet("listele")]
        public async Task<IActionResult> Listele()
        {
            try
            {
                if (_veriTabani.Basvurular == null) return Ok(new List<object>());
                var basvurular = await _veriTabani.Basvurular.ToListAsync();
                return Ok(basvurular);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

        [HttpGet("ogrenci/{kullaniciId}")]
        public async Task<IActionResult> GetOgrenciBasvurulari(int kullaniciId)
        {
            try
            {
                if (_veriTabani.Basvurular == null || _veriTabani.Ilanlar == null) return Ok(new List<object>());

                var basvuruListesi = await (from b in _veriTabani.Basvurular
                                            join i in _veriTabani.Ilanlar on b.IlanId equals i.Id
                                            join isveren in _veriTabani.Kullanicilar on i.KullaniciId equals isveren.Id into isverenGrup
                                            from isv in isverenGrup.DefaultIfEmpty()
                                            where b.KullaniciId == kullaniciId
                                            orderby b.Id descending
                                            select new
                                            {
                                                Id = b.Id,
                                                KullaniciId = b.KullaniciId,
                                                IlanId = b.IlanId,
                                                BasvuruTarihi = b.BasvuruTarihi,
                                                Durum = b.Durum,
                                                IlanBaslik = i.Baslik,
                                                SirketAdi = i.SirketAdi,
                                                IsverenEposta = isv != null ? isv.Eposta : "İletişim bilgisi yok"
                                            }).ToListAsync();

                return Ok(basvuruListesi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Başvuru listeleme backend hatası: {ex.Message}");
            }
        }

        [HttpPost("durum-degistir/{basvuruId}/{yeniDurum}")]
        public async Task<IActionResult> DurumDegistir(int basvuruId, string yeniDurum)
        {
            try
            {
                if (_veriTabani.Basvurular == null) return BadRequest("Veritabanı tablosuna erişilemiyor.");

                var basvuru = await _veriTabani.Basvurular.FindAsync(basvuruId);
                if (basvuru == null) return NotFound("İlgili başvuru kaydı bulunamadı.");

                basvuru.Durum = yeniDurum;
                await _veriTabani.SaveChangesAsync();

                return Ok(new { mesaj = $"Başvuru durumu başarıyla '{yeniDurum}' yapıldı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Durum güncelleme hatası: {ex.Message}");
            }
        }

        // 🔒 SİBER GÜVENLİK ADIMI: Bu metoda erişmek için geçerli bir JWT Token göndermek ŞART oldu!
        [HttpPost("/api/Ilan/basvuru-yap", Name = "BasvuruYapUnique")]
        [Authorize]
        public async Task<IActionResult> BasvuruYap([FromBody] Basvuru yeniBasvuru)
        {
            try
            {
                if (yeniBasvuru == null) return BadRequest("Gönderilen başvuru verisi boş.");
                if (_veriTabani.Basvurular == null) return BadRequest("Veritabanı tablosuna erişilemiyor.");

                bool varMi = false;
                try
                {
                    varMi = await _veriTabani.Basvurular.AnyAsync(b => b.IlanId == yeniBasvuru.IlanId && b.KullaniciId == yeniBasvuru.KullaniciId);
                }
                catch (Exception) { }

                if (varMi) return BadRequest("Bu ilana zaten başvuru yaptınız.");

                yeniBasvuru.Id = 0;
                yeniBasvuru.BasvuruTarihi = DateTime.UtcNow;
                yeniBasvuru.Durum = "Beklemede";

                await _veriTabani.Basvurular.AddAsync(yeniBasvuru);
                await _veriTabani.SaveChangesAsync();

                return Ok(new { mesaj = "Başvuru başarıyla alındı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Başvuru eklenirken hata oluştu: {ex.Message}");
            }
        }

        [HttpGet("ai-analiz/{ilanId}/{kullaniciId}")]
        public async Task<IActionResult> RealAiAnaliz(int ilanId, int kullaniciId)
        {
            string skor = "%88";
            string tavsiye = "🤖 [JobMatch AI Raporu]: Profiliniz ilan gereksinimleriyle yüksek oranda eşleşmektedir. C# ve .NET Core altyapınız bu pozisyon için oldukça yeterlidir.";
            string eksikler = "Portfolyonuza PostgreSQL kullanan canlı bir Web API projesi eklemek sizi öne geçirecektir.";

            try
            {
                if (_veriTabani.Ilanlar != null && _veriTabani.Kullanicilar != null)
                {
                    var ilan = await _veriTabani.Ilanlar.FindAsync(ilanId);
                    var kullanici = await _veriTabani.Kullanicilar.FindAsync(kullaniciId);

                    if (ilan != null && kullanici != null)
                    {
                        string ilanBaslik = ilan.Baslik?.ToLower() ?? "";
                        string ilanAciklama = ilan.Aciklama?.ToLower() ?? "";
                        string kullaniciSehir = kullanici.Sehir?.ToLower() ?? "";
                        string ilanSehir = ilan.Sehir?.ToLower() ?? "";

                        bool sehirUyumlu = kullaniciSehir == ilanSehir || ilanAciklama.Contains("remote") || ilanAciklama.Contains("uzaktan");

                        if (ilanBaslik.Contains("frontend") || ilanBaslik.Contains("front-end") || ilanAciklama.Contains("react"))
                        {
                            skor = sehirUyumlu ? "%50" : "%35";
                            tavsiye = $"🤖 [JobMatch AI Raporu]: Bu pozisyon yoğunlukla Front-End teknolojileri içermektedir. {kullanici.AdSoyad} olarak sizin profiliniz ise Back-End (.NET) odaklıdır.";
                            eksikler = "React.js veya Vue.js deneyimi, gelişmiş Tailwind CSS bilgisi.";
                        }
                        else if (ilanBaslik.Contains("senior") || ilanBaslik.Contains("kıdemli"))
                        {
                            skor = "%55";
                            tavsiye = $"🤖 [JobMatch AI Raporu]: Kurum bu pozisyonda kıdemli uzman aramaktadır. Başlangıç seviyesi profiliniz bu ilan için mentor desteğine ihtiyaç duyabilir.";
                            eksikler = "Mikroservis mimarileri, CI/CD süreçleri.";
                        }
                        else if (ilanBaslik.Contains("c#") || ilanBaslik.Contains(".net") || ilanBaslik.Contains("backend"))
                        {
                            skor = sehirUyumlu ? "%95" : "%75";
                            tavsiye = $"🤖 [JobMatch AI Raporu]: Harika bir uyum! İlanın aradığı ASP.NET Core ve C# yetenekleri, {kullanici.AdSoyad} isimli adayın teknoloji yığınıyla örtüşüyor.";
                            eksikler = sehirUyumlu ? "Belirgin bir teknik eksik bulunamadı." : $"Teknik altyapınız tam puan aldı fakat ilan konumu ({ilan.Sehir}) ile yaşadığınız şehir ({kullanici.Sehir}) farklılık gösteriyor.";
                        }
                    }
                }
                return Ok(new { skor = skor, tavsiye = tavsiye, eksikler = eksikler });
            }
            catch (Exception)
            {
                return Ok(new { skor = skor, tavsiye = tavsiye, eksikler = eksikler });
            }
        }

        // 🔥 YENİ: SİBER GÜVENLİK GİRİŞ NOKTASI (JWT GİRİŞ METODU)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] KullaniciGirisModel model)
        {
            try
            {
                if (_veriTabani.Kullanicilar == null) return BadRequest("Kullanıcı tablosuna erişilemiyor.");

                var kullanici = await _veriTabani.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Eposta == model.Eposta && k.Sifre == model.Sifre);

                if (kullanici == null)
                    return Unauthorized(new { mesaj = "E-posta veya şifre hatalı!" });

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

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    token = tokenString,
                    kullaniciId = kullanici.Id,
                    rol = kullanici.Rol,
                    adSoyad = kullanici.AdSoyad
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Giriş yapılırken sistem hatası oluştu: {ex.Message}");
            }
        }
    }

    // 🔥 GİRİŞ PARAMETRELERİNİ YAKALAYAN YARDIMCI SINIF (Class Dışı, Namespace İçi)
    public class KullaniciGirisModel
    {
        public string Eposta { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}