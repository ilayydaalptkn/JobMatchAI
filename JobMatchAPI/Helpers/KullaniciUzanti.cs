using System.Security.Claims;

namespace JobMatchAPI.Helpers
{
    public static class KullaniciUzanti
    {
        public static int KullaniciIdAl(this ClaimsPrincipal kullanici)
        {
            var id = kullanici.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var parsed) ? parsed : 0;
        }

        public static string RolAl(this ClaimsPrincipal kullanici) =>
            kullanici.FindFirstValue(ClaimTypes.Role) ?? "Ogrenci";
    }
}
