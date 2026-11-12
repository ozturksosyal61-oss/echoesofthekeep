namespace CoopGame
{
    /// <summary>
    /// Okunabilir mektuplar. Hikâye: Kont Valerian, saatçi Aurel'i kuleye (X), arşivci Mira'yı mahzenlere (O) ayırdı.
    /// Kardeşler yalnız taşın taşıdığı sesle konuştu; şatonun kilitlerini birlikte kurdular. Mektuplar bulmacayı ima eder, çözmez.
    /// Kimlik: 0 lobi, bulmaca p için X = 2p, O = 2p + 1, 16 final (taht).
    /// </summary>
    public static class LoreTexts
    {
        public struct Entry { public string title, body, sign; }

        static Entry E(string t, string b, string s) => new Entry { title = t, body = b, sign = s };

        public static Entry Get(int id) => id >= 0 && id < All.Length && All[id].title != null ? All[id] : E("Solmuş sayfa", "Mürekkep akmış; okunacak bir şey kalmamış.", "");

        static readonly Entry[] All =
        {
            // 0 — Lobi Holü
            E("Kâhyanın son notu",
              "Kim olursanız olun, bu şato tek kişiye kapısını açmaz.\n\n" +
              "Kont Valerian iki kardeşi birbirinden ayırdığından beri taşlar yalnız sesi taşır. Biriniz çanların duyulduğu yere, yukarı çıkacak; " +
              "öbürünüz suyun damladığı yere, aşağı inecek. Birbirinizi görmeyeceksiniz.\n\n" +
              "Gördüğünüzü anlatın. Duyduğunuza güvenin. Ve plakaların üzerinde biraz bekleyin; şato kimin nereye ait olduğuna kendisi karar verir.",
              "— Kâhya Edrik"),
            default, default,
            // 2 — X, Çan Kulesi (kollar)
            E("Kule defterinden",
              "Mira,\n\nDört kolu kendi ellerimle döktüm, her birinin başına bir işaret kazıdım. Ama hangisinin önce çekileceğini bana söylemediler. " +
              "Kont, sırayı senin duvarına, hücrelerin yanındaki boyalı taşa işletti; 'kardeşler birbirine muhtaç kalsın' dedi.\n\n" +
              "Yanlış kolu çektiğimde dişliler geri sarıyor, her şey başa dönüyor. Duvarda ne görüyorsan, gördüğün sırayla bağır. Ben dinliyorum.",
              "— Aurel"),
            // 3 — O, Zindan (fresk)
            E("Hücre duvarına sıkıştırılmış",
              "Aurel,\n\nBurada azizler yok; duvardaki boya işaretlerden ibaret. Kare mi elmas mı, artı mı çarpı mı... ışık azken birbirine benziyorlar, dikkatli bak dedim kendime.\n\n" +
              "Kasa kapısı senin kollarına bağlı. Ben okuyacağım, sen çekeceksin. Bir işareti atlarsam kapı yine susacak.",
              "— Mira"),
            // 4 — X, Saat Odası (yazıt + kadranlar + mumlar)
            E("Durmuş saatin arkasında",
              "Mira,\n\nSaat odasında üç mum yanıyor; her biri senin cebindeki bir kibrit. Biri aşağıda sönünce burada da sönüyor, ben de anlıyorum.\n\n" +
              "Duvardaki yazıtta rakamlar var, ama rakam tek başına hiçbir şey açmaz. Aşağıda, taş yataklarda yatanların üstünde işaretler olduğunu biliyorum. " +
              "Sen rakamın yanındaki işareti bul, ben kadranı ona çevireyim. Kol, ancak üçü doğruysa çekilmeli.",
              "— Aurel"),
            // 5 — O, Mahzen (karanlık + kibrit)
            E("Kibrit kutusunun içinde",
              "Kapı arkamdan kapandı ve karanlık hemen geldi.\n\nÜç kibritim var; her biri birkaç nefeslik. Lahitlerin üstünde bir işaret ve bir rakam kazılı. " +
              "Aurel bana rakamları söylüyor, ben ona işaretleri.\n\nKaranlıkta fazla kalırsam bir şey yaklaşıyor gibi oluyor. Kibriti boşa yakma; önce ne arayacağını bil, sonra çak.",
              "— Mira"),
            // 6 — X, Hazne (gösterge + tablo)
            E("Ölçü defterinin kenarı",
              "Göstergedeki ibre suyun nerede olduğunu, kırmızı çentik nerede durması gerektiğini söyler.\n\n" +
              "Ölçü tablosunu meraklı gözlerden sakladım; perdeli rafların arkasına bak. Her işaretin suya kattığını ya da aldığını orada yazdım. " +
              "Bir şeyi unutma: borulardan biri ters akar ve tablo bunu da fısıldar.\n\nSu çentiğe tam oturmadan mühürleme. Mira aşağıda nefesini tutuyor olacak.",
              "— Aurel"),
            // 7 — O, Sarnıç (vanalar)
            E("Islak sayfa",
              "Sarnıçta su her nefeste biraz daha yükseliyor.\n\nVanaları Kont'un adamları bulamasın diye sütunların gölgesine gizledim; duvardakiler artık işe yaramaz. " +
              "Her vananın bir işareti ve bir boru rengi var. Hangisini kaç kez açacağımı Aurel bilir, ben yalnız gördüğümü söylerim.\n\n" +
              "Su başımı aşarsa nefesimi tutarım. Uzun değil.",
              "— Mira"),
            // 8 — X, Arşiv (şifre çarkı)
            E("Mühür ocağının yanında",
              "Kont'un mektuplarını çarkla şifreledim: dış halkada işaretler, içte harfler.\n\n" +
              "Anahtar, mektubun mühründe yazar. Ama mektup bende değil; Mira onu yırtıp aşağıya dağıttı. Rakamı ondan iste, iç halkayı o kadar döndür. " +
              "Sonra onun okuduğu her işaretin karşısına düşen harfi söyle.\n\nKelime anlamsız çıkıyorsa anahtar yanlıştır, işaretler değil.",
              "— Aurel"),
            // 9 — O, Kütüphane (yırtık mektup + kilit)
            E("Okuma masasının çekmecesinden",
              "Kont'un mektubunu yırttım ve parçaları kütüphaneye dağıttım; bazıları sahte, onları bilerek bıraktım.\n\n" +
              "Gerçek parçalarda mürekkep damlası var; damlaları say, sıra oradadır. Mühürdeki rakamı Aurel'e ilet.\n\n" +
              "Kelime sana döndüğünde kilidi kapıda arama. Ölülerin arasında, taşın arkasında bekliyor.",
              "— Mira"),
            // 10 — X, Harita Odası (maket)
            E("Kürsüdeki harita",
              "Katakombun maketini yaptım; her odacığa bir rün ve zar noktası kazıdım. Kırmızı iğneler, oraya basanın geri dönmediğini gösterir.\n\n" +
              "Mira fenerle aşağıda yürürken bana gördüğü rünü söyleyecek. Ben de maketin başında ona hangi yöne döneceğini. " +
              "Yağ sınırlı; tereddüt ettikçe fener kısalır.",
              "— Aurel"),
            // 11 — O, Katakomb (kâşifin günlüğü)
            E("Kâşifin günlüğü",
              "Üçüncü gün. Yollar her girişte değişiyor gibi.\n\nKöşelerde rün ve zar levhaları var, ama ne anlama geldiklerini çözemedim. " +
              "Tebeşirle oklar çizdim; sonra fark ettim ki oklar beni hep aynı yere getiriyor. Oklara güvenmeyin.\n\n" +
              "Yukarıda biri olsaydı, levhaları ona okurdum.",
              "— Adı silinmiş bir kâşif"),
            // 12 — X, Çatı (aynalar)
            E("Rüzgârda kalmış not",
              "Çatıda üç ayna var. Ayın ışığını birinden öbürüne, oradan kuyunun ağzına indir.\n\n" +
              "Ay her gece başka yerden, başka yüzle doğar. Yüzünü iyi bak ve Mira'ya anlat: büyüyen mi, küçülen mi, ne kadarı karanlık?\n\n" +
              "Bulutlar gelmeden bitir. Bulut gelirse aynalar savrulur.",
              "— Aurel"),
            // 13 — O, Kuyu Odası (ay taşları)
            E("Ay rahiplerinin kürsüsünde",
              "Zeminde ayın sekiz yüzü var. Işık hangi taşa düşerse onu Aurel'e söyle; hangisinin doğru olduğunu yalnız gökyüzüne bakan bilir.\n\n" +
              "Doğru taşta ışığın biraz beklemesi gerekir. Acele edersen taşlar kayar.\n\nBüyüyen ile küçülen birbirine aynada bakan iki kardeş gibidir; kolayca karışır.",
              "— Mira"),
            // 14 — X, Çan Dairesi (çanlar + kadran)
            E("Çan ipinin dibinde",
              "Çanlara notalar kazıdım. Hangi sırayla çalınacakları aşağıda, kemiklerin arasındaki kitabede yazar; Mira okusun.\n\n" +
              "Sonra kadranın kapağı açılacak; orada gördüğünü ona oku. En sonunda büyük çan ile org aynı anda konuşmalı. " +
              "Birlikte sayın, birlikte vurun.",
              "— Aurel"),
            // 15 — O, Kemik Org Kriptası
            E("Org sırasının altında",
              "Kemik org Kont'un en büyük gururuydu.\n\nOrgun üstündeki sayfa yalan söyler; gerçek kitabe kemiklerin arasında gizlidir. " +
              "Notaları Aurel'e oku, o çanları çalsın. Sonra o bana okuyacak, ben çalacağım.\n\nSon notayı yalnız çalarsam boşa gider: üç, iki, bir.",
              "— Mira"),
            // 16 — Final, taht
            E("Tahtta bırakılmış mektup",
              "Buraya ikiniz birlikte geldiyseniz biz başaramadık demektir.\n\n" +
              "Bu kapıyı Kont'un uyuttuğu şey için mühürledik. Kolları birlikte çekin, mühür taşlarına birlikte basın; şato ancak iki sesi birden duyar.\n\n" +
              "Ve kapının ardından ne duyarsanız duyun, cevap vermeyin.",
              "— Aurel ve Mira"),
        };

    }
}
