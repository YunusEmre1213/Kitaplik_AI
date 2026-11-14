using KitapYonetimSistemi.Models;
using Xunit;

namespace KitapYonetimSistemi.Tests;

public class YorumGorunurlukTests
{
    [Fact] public void SayfaKapisiYoksaHerkeseAcik() => Assert.True(YorumGorunurluk.Acik(null, false, false, null));
    [Fact] public void IlerlemesiOlmayanKilitliGorur() => Assert.False(YorumGorunurluk.Acik(50, false, false, null));
    [Fact] public void GeridekiOkuyucuKilitliGorur() => Assert.False(YorumGorunurluk.Acik(50, false, false, 49));
    [Fact] public void SayfayaUlasanGorur() => Assert.True(YorumGorunurluk.Acik(50, false, false, 50));
    [Fact] public void BitirenHerSeyiGorur() => Assert.True(YorumGorunurluk.Acik(300, false, true, null));
    [Fact] public void YazarKendiYorumunuGorur() => Assert.True(YorumGorunurluk.Acik(300, true, false, null));
}
