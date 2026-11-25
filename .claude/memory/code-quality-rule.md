---
name: code-quality-rule
description: Mobil kodu Antigravity yazdı — dokunulan her yerde kalite artırılır, eskimiş (obsolete) API/kalıntı bırakılmaz, mimari (MVVM + servis katmanı + DI) korunur
metadata:
  type: feedback
---

Kullanıcı 2026-11-25: "eski hiç bir şey kalmasın kankam. antigravity yazdığı için kod kalitesini her zaman arttır. mimariyi de bozmasın".

- **Eski hiçbir şey kalmaz:** obsolete/deprecated API uyarıları (CS0618, CA1422 vb.), eski adlar ("StaffSync" görünen metinleri, eski hesap/grup), ölü kod, kullanılmayan alan/using bırakılmaz. Derleme 0 uyarı hedeflenir.
- **Kaliteyi her zaman artır:** Antigravity'nin yazdığı bir dosyaya dokunurken o bölgedeki bariz kusurları (sessiz `catch {}`, tekrar eden kod, yanlış sorumluluk) da düzelt.
- **Mimariyi bozma:** MVVM (CommunityToolkit ObservableProperty/RelayCommand), `Services/` arayüz + platform implementasyonu (`Platforms/Android|iOS/Services`), DI kaydı `MauiProgram.cs`, ekranlar arası iletişim `WeakReferenceMessenger` + `Messages/` klasörü. Yeni kod bu kalıplara uyar; ViewModel'den platform API'sine doğrudan gidilmez.

**Why:** Mobil kod Antigravity'den geldi, kalitesi düşük; kullanıcı her dokunuşta temizlenmesini istiyor ama yapının değişmesini istemiyor.
**How to apply:** Her değişiklik sonrası derleme uyarılarını kontrol et, sıfırla. Bkz. [[design-polish-by-claude]], [[mobile-scope]].
