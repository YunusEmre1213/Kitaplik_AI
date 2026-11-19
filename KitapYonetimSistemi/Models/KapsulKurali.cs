namespace KitapYonetimSistemi.Models
{
    // Zaman kapsülü notlarının ne zaman açılacağını belirleyen saf kural.
    public static class KapsulKurali
    {
        public const string Yok = "yok";
        public const string BitinceAc = "bitince";
        public const string AltiAy = "6ay";
        public const string BirYil = "1yil";

        // Form seçimini (bitinceAc, açılış tarihi) çiftine çevirir; geçersiz seçim kapsül sayılmaz.
        public static (bool BitinceAc, DateTime? Acilis) Coz(string? secim, DateTime simdi) => secim switch
        {
            BitinceAc => (true, null),
            AltiAy => (false, simdi.AddMonths(6)),
            BirYil => (false, simdi.AddYears(1)),
            _ => (false, null)
        };

        public static bool KapsulMu(bool bitinceAc, DateTime? acilis) => bitinceAc || acilis.HasValue;

        // Kapsül açıldı mı? Kapsül değilse not her zaman açıktır.
        public static bool Acik(bool bitinceAc, DateTime? acilis, bool bitirdi, DateTime simdi)
        {
            if (!KapsulMu(bitinceAc, acilis)) return true;
            if (bitinceAc && bitirdi) return true;
            return acilis.HasValue && simdi >= acilis.Value;
        }
    }
}
