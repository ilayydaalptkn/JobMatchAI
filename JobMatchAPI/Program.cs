using JobMatchAPI.Data;
using JobMatchAPI.Models;
using JobMatchAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Veritabanı bağlantı dizesi bulunamadı.");

var jwtServisi = new JwtServisi(builder.Configuration);
builder.Services.AddSingleton(jwtServisi);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://ilayydaalptkn.github.io", "http://localhost:5500", "http://127.0.0.1:5500" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token girin. Örnek: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtServisi.AnahtarAl()))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<VeriTabaniBaglantisi>(options =>
{
    if (!builder.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(connectionString))
        options.UseNpgsql(connectionString);
});

if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<JobMatchAPI.ArkaPlanServisleri.TumSitelerBotServisi>();

var app = builder.Build();

app.UseStaticFiles();
app.UseCors("ProductionCors");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<VeriTabaniBaglantisi>();
            context.Database.Migrate();
            await VeriTohumla(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SİSTEM HATASI]: Başlangıç hatası: {ex.Message}");
        }
    }
}

app.Run();

static async Task VeriTohumla(VeriTabaniBaglantisi context)
{
    var demoHesaplar = new[]
    {
        new { Eposta = "demo@jobmatch.ai", AdSoyad = "Demo Öğrenci", Rol = "Ogrenci", Telefon = "05551234567", Sehir = "Ankara",
            CvOkul = "ODTÜ - Bilgisayar Mühendisliği", CvHedefIs = "Backend Developer", CvSehir = "Ankara",
            CvYetenekler = "[\"C#\",\".NET\",\"PostgreSQL\",\"React\"]", CvTecrubeler = "[\"Üniversite Projesi - JobMatch AI\"]" },
        new { Eposta = "isveren@jobmatch.ai", AdSoyad = "Demo İşveren A.Ş.", Rol = "Isveren", Telefon = "05559876543", Sehir = "İstanbul",
            CvOkul = "", CvHedefIs = "", CvSehir = "", CvYetenekler = "[]", CvTecrubeler = "[]" }
    };

    foreach (var demo in demoHesaplar)
    {
        if (!await context.Kullanicilar.AnyAsync(k => k.Eposta == demo.Eposta))
        {
            context.Kullanicilar.Add(new Kullanici
            {
                AdSoyad = demo.AdSoyad,
                Eposta = demo.Eposta,
                Telefon = demo.Telefon,
                Sifre = SifreServisi.Hashle("Demo123!"),
                Sehir = demo.Sehir,
                Rol = demo.Rol,
                CvOkul = demo.CvOkul,
                CvHedefIs = demo.CvHedefIs,
                CvSehir = demo.CvSehir,
                CvYetenekler = demo.CvYetenekler,
                CvTecrubeler = demo.CvTecrubeler
            });
        }
    }
    await context.SaveChangesAsync();

    if (context.Ilanlar.Count() <= 2)
    {
        var isveren = await context.Kullanicilar.FirstOrDefaultAsync(k => k.Rol == "Isveren");
        var hazirIlanlar = new List<Ilan>
        {
            new() {
                Baslik = "Junior .NET Developer",
                SirketAdi = isveren?.AdSoyad ?? "E-Ticaret Çözümleri Ltd.",
                Sehir = "Ankara",
                Aciklama = "Aranan Kriterler: C#, ASP.NET Core, Entity Framework, PostgreSQL.",
                Maas = "Asgari Ücret + Yol",
                KullaniciId = isveren?.Id,
                YayinlanmaTarihi = DateTime.UtcNow
            },
            new() {
                Baslik = "React Front-End Engineer",
                SirketAdi = isveren?.AdSoyad ?? "Global Yazılım Teknolojileri",
                Sehir = "İstanbul",
                Aciklama = "Aranan Kriterler: React.js, Tailwind CSS, TypeScript, Redux.",
                Maas = "Dolgun Ücret",
                KullaniciId = isveren?.Id,
                YayinlanmaTarihi = DateTime.UtcNow
            }
        };

        foreach (var ilan in hazirIlanlar)
        {
            bool varMi = context.Ilanlar.Any(i =>
                i.Baslik.ToLower() == ilan.Baslik.ToLower() &&
                i.SirketAdi.ToLower() == ilan.SirketAdi.ToLower());
            if (!varMi) context.Ilanlar.Add(ilan);
        }

        await context.SaveChangesAsync();
    }
}

public partial class Program { }
