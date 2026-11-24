---
name: mobile-scope
description: Mobil uygulama sadece rehber görüntüleyici + telefona senkron; kişi ekleme/düzenleme/silme mobilde yok
metadata:
  type: project
---

Mobil (MAUI) uygulama kapsamı: Giriş → Rehber izni (ilk açılış) → Rehber (arama, departman filtresi, A-Z) → Kişi Detayı (ara/mesaj/e-posta/WhatsApp, telefona kaydet) → Profil (senkron aç/kapat, son senkron, şimdi senkronize et, tema, çıkış). Tasarım yönü: "Modern iOS tarzı", ana renk #164194, açık/koyu tema. Mockup: https://claude.ai/artifact/PNCkj2XAMGQ2HJ3Y52VhKm

**Kişi ekleme/düzenleme/silme mobilde YOK** — admin ekranı, "+" butonu, "Düzenle" linki eklenmez. Backend'deki admin CRUD uçları mobilden çağrılmaz.

**Bunun yerine "Hatalı bilgi bildir" var:** Kişi Detayı'nın altında link → hangi alan yanlış (çoklu seçim çipleri) + "doğrusu ne olmalı" açıklaması → gönder → teşekkür ekranı. Backend'de bunun için yeni uç gerekecek (henüz yok).

**Rehber: sayfalama + filtre zorunlu.** Liste sonsuz kaydırmayla sayfa sayfa yüklenir (`GetContactsWithPagination`). Arama kutusunun yanında filtre butonu (etkin filtre sayısı rozeti) → alttan açılan sayfa: Sıralama (Ad/Departman/Unvan), Departman (çoklu), Şirket.

**Why:** Kullanıcı 2026-11-24'te admin düzenleme ekranını "gerek yok, kaldır" dedi; ardından "düzenleme olmasın ama yanlış bilgi bildirimi yapabilsinler" dedi.
**How to apply:** Mobilde sadece okuma (ContactList / sayfalama) ve senkron. Bkz. [[project-overview]].
