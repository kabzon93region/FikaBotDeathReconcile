# Publish to GitHub — Fika Bot Death Reconcile

**Статус:** `ready`  
**GitHub:** Release + zip  
**Версия:** `1.1.3`  
**Deployment:** `(headless_all)`

## 1. Подготовка (уже сделано этим скриптом)

Папка: `github-repos/FikaBotDeathReconcile/`

## 2. Создать репозиторий и запушить

```powershell
cd github-repos/FikaBotDeathReconcile
git init
git add .
git commit -m "Source backup Fika Bot Death Reconcile v1.1.3"
git branch -M main
git remote add origin https://github.com/kabzon93region/FikaBotDeathReconcile.git
git push -u origin main
```

Или автоматически:

```powershell
python CURSORAIMODING/tools/publish/publish_github_release.py FikaBotDeathReconcile --create-repo
```

## 3. GitHub Release

Прикрепить zip (только игровые файлы, без INSTALL.md):

`\\Servant\data\Games\EscapeFromTarkov4\CURSORAIMODING\releases\FikaBotDeathReconcile_(headless_all)_v1.1.3_2026-06-29.zip`

```powershell
gh release create v1.1.3 "\\Servant\data\Games\EscapeFromTarkov4\CURSORAIMODING\releases\FikaBotDeathReconcile_(headless_all)_v1.1.3_2026-06-29.zip" ^
  --title "Fika Bot Death Reconcile v1.1.3" ^
  --notes-file CHANGELOG.md
```

## Описание репозитория (suggested)

Согласование смерти ботов host↔client в Fika coop.

SPT 4.0 + Fika 2.3 headless stack. Deployment: `(headless_all)`.
