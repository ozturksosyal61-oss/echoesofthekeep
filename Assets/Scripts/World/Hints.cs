using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Kademeli ipuçları: bulmaca (1-8) x rol (X/O) x 3 kademe. Çözümler her denemede karıştığı için ipucu cevabı değil yöntemi söyler.
    /// Yerel: her oyuncu kendi ipucunu açar; kademeler arası CooldownSeconds. Açılan her ipucu host'taki sayaca (istatistik) gider.
    /// </summary>
    public static class Hints
    {
        public const int Levels = 3;
        public const float CooldownSeconds = 60f;

        static readonly int[] level = new int[PuzzleManager.MaxPuzzles];
        static float nextAt;

        public static readonly string[] Names = { "", "Sembol kolları", "Kibrit mahzeni", "Sarnıç", "Şifreli mektup", "Katakomb", "Ay aynaları", "Çan ve org", "Büyük salon" };

        // [puzzle][rol][kademe]
        static readonly string[][][] T =
        {
            null,
            new[] { // 1 kollar → fresk
                new[] { "Dört kolun her birinde bir işaret var ama sırası sende değil.", "Sırayı arkadaşın aşağıda bir duvar resminde görüyor; sor.", "Benzer işaretlere dikkat: kare/elmas, artı/çarpı, üçgen/ters üçgen. Yanlışta kollar sıfırlanır, baştan çek." },
                new[] { "Zindanın duvarlarına bak: bir resim işaretleri sırayla gösteriyor.", "Gördüğün sırayı arkadaşına tek tek anlat; kolları o çekecek.", "Benzer işaretleri ayırt ederek tarif et (köşesi mi yukarıda, düz mü duruyor)." },
            },
            new[] { // 2 kibrit mahzeni
                new[] { "Yazıttaki üç Roma rakamı arkadaşının aradığı yeri gösterir.", "Rakamları söyle; o her rakamın yanındaki işareti sana tarif etsin. Kadranları o işaretlere çevir.", "Mumlar onun kalan kibritini gösterir. Emin olmadan kolu çekme: yanlış onay bir kibrit yakar." },
                new[] { "Karanlıkta oyalanma; kibriti ancak okuyacağın yere varınca yak.", "Batı duvarındaki panelde işaretler ve Roma rakamları var. Arkadaşın hangi rakamları istediğini söyleyecek.", "Her rakamın işaretini kısa ve kesin tarif et. Ölürsen her şey yeniden karışır, baştan sor." },
            },
            new[] { // 3 sarnıç
                new[] { "Göstergede suyun şu anki seviyesi ve hedef var; aradaki farkı hesapla.", "Tablo her işaretin suyu ne kadar oynattığını söyler. Bir renk boru ters akar: o vananın etkisini tersine çevir.", "Hangi vanaların çevrileceğini söyle; seviye hedefe tam oturunca mühür kolunu çek. Yanlış mühür suyu yükseltir." },
                new[] { "Her vananın üstündeki işareti ve borusunun rengini arkadaşına söyle.", "Hesabı o yapıyor; sen yalnızca onun söylediği vanaları çevir.", "Su yükseliyor: konuşmayı kısa tut. Başın suya girerse nefesin sınırlı." },
            },
            new[] { // 4 şifreli mektup
                new[] { "Çarkın iç halkası döner; doğru konum arkadaşının mektubundaki mühürde yazan rakamdır.", "Plakada o rakam görünene kadar çarkı çevir. Sonra onun söylediği her işaretin karşısındaki harfi oku.", "Kelime anlamsız çıkıyorsa çark yanlış konumdadır; rakamı yeniden sor." },
                new[] { "Mektup yırtık: parçalar kütüphanenin farklı yerlerine dağılmış. Nişlere, lahitlere, masalara bak.", "Parçaların sırasını mürekkep noktası gösterir. Bazı parçalar boş, onları sayma. Mühürdeki rakamı da söyle.", "Harf kadranı kapıda değil; yan nefte bir lahdin arkasında. Arkadaşının okuduğu kelimeyi oraya gir." },
            },
            new[] { // 5 katakomb
                new[] { "Maket kürsünün üstünde, girişten görünmez. Kırmızı iğneler tuzak.", "Arkadaşın bulunduğu hücredeki rün ve zar işaretini söylesin; maketten yerini bul.", "Her adımda yalnızca bir yön ver ve yerini doğrulat. Fenerin yağı sınırlı; uzun tartışma yağ yakar." },
                new[] { "Her hücrenin köşesinde rün ve zar levhası var; ilk iş onu arkadaşına oku.", "Yolu o görüyor. Yönünü sormadan bir sonraki hücreye geçme: sapan kolların çoğu tuzak.", "Ölürsen labirent ve işaretler değişir; girişten yeniden levha okumaya başla." },
            },
            new[] { // 6 ay aynaları
                new[] { "Gökteki ayın evresine bak ve arkadaşına söyle.", "Aynaları sırayla çevir: ilki ışığı ikinciye, ikincisi kuyudaki aynaya, sonuncusu merceklerden birine.", "Arkadaşın ışığın düştüğü evreyi söyler; doğru evre tutturulana kadar son aynayı çevir. Süre biterse bulut gelir, her şey karışır." },
                new[] { "Zemindeki taşlarda ay evreleri var; büyüyen ve küçülen hilal birbirine benzer, dikkat.", "Işık hangi taşa düşüyorsa onun evresini arkadaşına tarif et.", "Doğru evrede ışık birkaç saniye tutulmalı; üç turu geçmeniz gerekiyor." },
            },
            new[] { // 7 çan ve org
                new[] { "Çanların her birinde bir nota işareti kazılı. Sırayı arkadaşın aşağıdaki kitabede görüyor.", "Çanlar bitince kadranın kapağı açılır; gördüğün notaları arkadaşına oku.", "Son notada büyük çanı onunla aynı anda çal: sesli say, 3-2-1." },
                new[] { "Orgun yanındaki kitabede dört nota yazılı; sırayla arkadaşına oku.", "Sonra o sana kadrandaki notaları okuyacak; ilk üçünü orgda çal.", "Dördüncü notayı onun büyük çanıyla aynı anda bas. Birlikte sesli sayın." },
            },
            new[] { // 8 büyük salon
                new[] { "Salonun senin yarısında bir şövalye heykeli var; kolunu ara.", "Parmaklık ikiniz de kolu çekince açılır.", "Mühürlü kapının önündeki iki taşa birlikte basın." },
                new[] { "Salonun senin yarısında bir şövalye heykeli var; kolunu ara.", "Parmaklık ikiniz de kolu çekince açılır.", "Mühürlü kapının önündeki iki taşa birlikte basın." },
            },
        };

        public static int Current => Progress.Current(PuzzleManager.Instance);
        static int Role => PlayerAvatar.Local != null && PlayerAvatar.Local.RoleIndex == Roles.O ? 1 : 0;

        public static bool Has(int puzzle) => puzzle > 0 && puzzle < T.Length && T[puzzle] != null;
        public static int Level(int puzzle) => Has(puzzle) ? level[puzzle] : 0;
        public static string Text(int puzzle, int k) => Has(puzzle) && k < Levels ? T[puzzle][Role][k] : "";
        public static float Wait => Mathf.Max(0f, nextAt - Time.unscaledTime);

        public static bool CanReveal(int puzzle) => Has(puzzle) && level[puzzle] < Levels && Wait <= 0f && PlayerAvatar.Local != null;

        public static void Reveal(int puzzle)
        {
            if (!CanReveal(puzzle)) return;
            level[puzzle]++;
            nextAt = Time.unscaledTime + CooldownSeconds;
            PlayerAvatar.Local.RequestHint();
        }

        /// <summary>Yeni oturum (menüden oda kur/katıl): ipuçları kapanır.</summary>
        public static void Clear() { System.Array.Clear(level, 0, level.Length); nextAt = 0f; }
    }
}
