---
name: solo-developer-no-pr
description: Kullanıcı projede tek başına çalışıyor — PR açma/sorma yok, ekip varsayımı yapılmaz
metadata:
  type: feedback
---

Kullanıcı 2026-11-26: "ne pr'ı ekip mi var. tekim ben".

- Pull request açılmaz, "PR açalım mı" diye sorulmaz; ekip/reviewer varsayımıyla iş önerilmez.
- Dal birleştirme gerekirse doğrudan git (merge) ile yapılır — o da kullanıcı isterse.

**Why:** Projede tek geliştirici kullanıcı; PR süreci gereksiz yük.
**How to apply:** Yarım işler listesine PR maddesi yazma; iş bitince commit/push yeterli. Bkz. [[design-polish-by-claude]].
