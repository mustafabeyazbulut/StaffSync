---
name: design-decisions-2026-11
description: Mobil + rehber dağıtımı için kullanıcının verdiği kararlar (tuşlu telefon dışa aktarımı, hata bildirimi, font) ve spec dosyasının yeri
metadata:
  type: project
---

Spec: `docs/superpowers/specs/2026-11-24-staffsync-mobil-ve-rehber-dagitimi-design.md`
Backend planı: `docs/superpowers/plans/2026-11-24-backend-rehber-ve-dagitim.md` (17 görev, Task 0'dan başlanır). Mobil planı henüz yazılmadı — backend bittikten sonra yazılacak.

**Durum (2026-11-25):** Backend planı bitti ve commit edildi. Mobil plan (`docs/superpowers/plans/2026-11-24-mobil-uygulama.md`, Task 1–10) kullanıcı tarafından **Antigravity**'ye yaptırıldı; derleniyor ama inceleme ciddi hatalar buldu (yanlış rehber ucu, refresh cevabı uyumsuz, Android hesabı/SyncAdapter yok, iOS not alanı yetkisi). Düzeltmeler `docs/superpowers/plans/2026-11-25-mobil-duzeltmeler.md` (Task 1–8) + ek 7 madde Antigravity'ye yaptırıldı ve incelemeden geçti (2026-11-25, son commit eee94f4; Android derlemesi temiz). Açık kalanlar: `usesCleartextTraffic` tüm uygulamaya açık (canlıda HTTPS'e kısıtlanmalı), gerçek cihaz/emülatör testi, iOS hiç cihazda denenmedi.

Kullanıcının 2026-11-24'te verdiği kararlar:
- **Eski cihazlar = tuşlu cep telefonları.** Bugün ekip rehber dosyasını elle hazırlayıp bir "publish" klasörüne koyuyor, oradan cihazlara elle aktarıyor. Çözüm: backend her gün + kişi değişikliğinden sonra otomatik `rehber.vcf` (vCard 2.1, Türkçe için QUOTED-PRINTABLE) ve `rehber.csv` üretip o klasöre yazar; ayrıca indirme ucu. Masa/IP telefonları ve santral kapsam dışı.
- **Hatalı bilgi bildirimi:** veritabanına kaydedilir + İK/BT'ye e-posta (şirket SMTP'si). Admin bildirimi görür ama kişiyi kendisi düzeltir.
- **Font:** telefonun sistem fontu (SF Pro / Roboto); Arial Nova gömülmez.
- **Koyu tema paleti** mockup'taki haliyle onaylandı ("uygundur").
- **Backend mimarisi korunur:** Generic Repository + Unit of Work kalır ("düzelt kanka"). Kaldırmayı önerdim, kullanıcı tutup düzeltmeyi seçti — tekrar kaldırmayı önerme. Repo hataları (sayfalama `OrderBy(x=>x)`, `CountAsync` filtresiz, `Find` takip ayarı) düzeltilir; karmaşık sorgular için `IReadRepository.AsQueryable()` eklenir.

**Why:** Uygulama akıllı telefonlarda elle aktarımı bitirir; tuşlu telefonlar için de elle dosya hazırlama ortadan kalkar.
**How to apply:** Planlama/uygulamada bu kararları yeniden sorma. Bkz. [[mobile-scope]], [[contact-sync-rules]], [[package-license-pins]], [[aunde-brand]].
