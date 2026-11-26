---
name: company-line-shortcode
description: Bu bir şirket hatları uygulaması — kısa kod (Contact.Home) asıl aranan numara; kullanıcı Profil'de "Şirket hattı kullanıyorum" der, aramalar ona göre kısa kod ya da cep
metadata:
  type: project
---

Kullanıcı 2026-11-25: "bu şirket hatları uygulaması, kısa kod varsa önemli olan kısa kodun aranması". Ardından: "profilde ayar koyalım; şirket hattı derse kısa kod, değilse numarayı arasın, adamın bildirimiyle ilerlesin".

- Kısa kod = `Contact.Home` (backend'de yorum "KısaKod"). Test verisinde 120 kişinin hepsinde var.
- `ISettingsService.UsesCompanyLine` (varsayılan **açık** — kullanıcı 2026-11-26 "varsayılan açık olsun" dedi; hattı olmayan Profil'den kapatır, o zaman cep aranır).
- Arama kararı tek yerde: `ICallService` (GetCallNumber / GetCallLabel). Ekranlar kendisi karar vermez.
- Rehber listesinde ara düğmesi ne arayacağını gösteren hap: "📞 1001" ya da "📞 Ara".
- Telefon rehberine yazımda: şirket hattıysa kısa kod "Kısa kod" etiketli **super primary** numara + cep; değilse sadece cep. Tercih değişince `ContactSyncFormat` biçimi değişir → tam senkron.

**Why:** Şirket hattında kısa kod ücretsiz/şirket içi; ama hattı olmayan biri kısa kodu arayamaz.
**How to apply:** Yeni arama/numara özelliğinde `ICallService`'i kullan; kısa kodu ayardan bağımsız kullanma (ayar varsayılan açık ama kapatılabilir). Bkz. [[contact-sync-rules]], [[mobile-scope]].
