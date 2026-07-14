namespace JobMatchAPI.Services
{
    public static class SifreServisi
    {
        public static string Hashle(string sifre) => BCrypt.Net.BCrypt.HashPassword(sifre);

        public static bool Dogrula(string sifre, string hashVeyaDuzMetin)
        {
            if (string.IsNullOrEmpty(hashVeyaDuzMetin)) return false;

            if (hashVeyaDuzMetin.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(sifre, hashVeyaDuzMetin);

            return hashVeyaDuzMetin == sifre;
        }

        public static bool HashGerekliMi(string? kayitliDeger) =>
            !string.IsNullOrEmpty(kayitliDeger) && !kayitliDeger.StartsWith("$2");
    }
}
