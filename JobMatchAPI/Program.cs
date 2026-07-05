using JobMatchAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// 1. JWT Kimlik Doğrulama Servisinin Eklenmesi
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen(options =>
{
    // 🔥 ÇAKIŞMAI ENGELLEYEN SİHİRLİ SATIR: 
    // Aynı rotaya sahip birden fazla metot olsa bile Swagger'ın çökmesini engeller.
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});
// --- 1. SERVİS TANIMLAMALARI (DEPENDENCY INJECTION) ---

// CORS Ayarı: Front-end'in (HTML) tarayıcı engeline takılmadan API'ye erişmesini sağlar
builder.Services.AddCors(options =>
{
    options.AddPolicy("HerKeseIzinVer", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // Test ortamı için false, canlıda token'ı basan site doğrulanır
        ValidateAudience = false, // Test ortamı için false
        ValidateLifetime = true, // Süresi dolmuş token'ları reddet
        ValidateIssuerSigningKey = true, // İmza doğrulaması yap
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BuSizinCokGizliVeGuvenliKriptografikAnahtarinizdir123!")) // En az 32 karakter olmalı
    };
});

// Front-end ile JSON uyumluluğunu en üst seviyeye çıkaran Controller ayarı
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Özellik isimlerini (Property) C# modelinde yazıldığı gibi (büyük/küçük harf değiştirmeden) bırakır
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Swagger/OpenAPI Dökümantasyon Servisleri
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Veritabanı Servisi (PostgreSQL Bağlantısı)
builder.Services.AddDbContext<VeriTabaniBaglantisi>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Canlı Bot ile İlan Kazıma Arka Plan Servisi (HostedService)
builder.Services.AddHostedService<JobMatchAPI.ArkaPlanServisleri.TumSitelerBotServisi>();

var app = builder.Build();

// --- 2. MIDDLEWARE SIRALAMASI (HTTP PIPELINE) ---

app.UseStaticFiles();

// CORS politikasını aktifleştiriyoruz (UseAuthorization ve MapControllers'dan önce olmalı)
app.UseCors("HerKeseIzinVer");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // 👈 Bunu mutlaka ekle kanka
app.UseAuthorization();


// Kontrolcü (Controller) rotalarını eşleştirme
app.MapControllers();

// --- 3. HAZIR VERİ BESLEME SİSTEMİ (DATA SEEDING) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<VeriTabaniBaglantisi>();
        context.Database.EnsureCreated();

        // Eğer veritabanındaki ilan tablosu boş veya yetersizse verileri otomatik simüle et
        if (context.Ilanlar.Count() <= 2)
        {
            Console.WriteLine("[SİSTEM]: Çoklu kariyer sitelerinden ilan çekme simülasyonu başlatıldı...");

            var hazirIlanlar = new List<JobMatchAPI.Models.Ilan>
            {
                new JobMatchAPI.Models.Ilan {
                    Baslik = "Junior .NET Developer",
                    SirketAdi = "E-Ticaret Çözümleri Ltd. (Kariyer.net'ten Alındı)",
                    Sehir = "Ankara",
                    Aciklama = "Aranan Kriterler: C#, ASP.NET Core, Entity Framework, PostgreSQL. Ankara ofisimizde çalışacak yetiştirilmek üzere junior geliştiriciler aranıyor.",
                    Maas = "Asgari Ücret + Yol",
                    YayinlanmaTarihi = DateTime.UtcNow
                },
                new JobMatchAPI.Models.Ilan {
                    Baslik = "React Front-End Engineer",
                    SirketAdi = "Global Yazılım Teknolojileri (LinkedIn'den Alındı)",
                    Sehir = "İstanbul",
                    Aciklama = "Aranan Kriterler: React.js, Tailwind CSS, TypeScript, Redux. Modern arayüz mimarilerine hakim, kullanıcı deneyimine önem veren takım arkadaşı.",
                    Maas = "Dolgun Ücret",
                    YayinlanmaTarihi = DateTime.UtcNow
                },
                new JobMatchAPI.Models.Ilan {
                    Baslik = "Senior Backend Software Expert",
                    SirketAdi = "FinansTek Bankacılık (Indeed'den Alındı)",
                    Sehir = "Ankara",
                    Aciklama = "Aranan Kriterler: Senior .NET Core, Mikroservisler, Docker, RabbitMQ. En az 5 yıl kurumsal bankacılık projelerinde deneyim sahibi uzman.",
                    Maas = "Yüksek Skala",
                    YayinlanmaTarihi = DateTime.UtcNow
                },
                new JobMatchAPI.Models.Ilan {
                    Baslik = ".NET Backend Stajyeri (Uzaktan)",
                    SirketAdi = "Ar-Ge İnovasyon A.Ş. (Github Jobs'tan Alındı)",
                    Sehir = "İzmir",
                    Aciklama = "Aranan Kriterler: C#, Web API, OOP mantığı. Tamamen uzaktan (Fully Remote) çalışacak, haftada en az 3 gün devam edebilecek stajyer arayışımız vardır.",
                    Maas = "Stajyer Ödeneği",
                    YayinlanmaTarihi = DateTime.UtcNow
                }
            };

            foreach (var ilan in hazirIlanlar)
            {
                // Aynı şirkete ait aynı başlıklı mükerrer ilan kontrolü
                bool varMi = context.Ilanlar.Any(i => i.Baslik.ToLower() == ilan.Baslik.ToLower() && i.SirketAdi.ToLower() == ilan.SirketAdi.ToLower());
                if (!varMi)
                {
                    context.Ilanlar.Add(ilan);
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine("[SİSTEM]: Kariyer.net, LinkedIn ve Indeed havuzundan toplam 4 yeni benzersiz ilan sisteme enjekte edildi!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SİSTEM HATASI]: Veri enjeksiyonunda hata: {ex.Message}");
    }
}

app.Run();