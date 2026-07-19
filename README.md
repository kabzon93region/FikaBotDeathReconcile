# Fika Bot Death Reconcile

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![EFT](https://img.shields.io/badge/EFT-16%2E9-orange)](https://www.escapefromtarkov.com/)
[![SPT](https://img.shields.io/badge/SPT-4.0.13-blue)](https://sp-tarkov.com/)
[![Fika](https://img.shields.io/badge/Fika-2%2E3%2Ex-purple)](https://github.com/project-fika/Fika-Plugin)
[![BepInEx](https://img.shields.io/badge/BepInEx-5%2E4%2Ex-yellow)](https://github.com/BepInEx/BepInEx)
![Deployment](https://img.shields.io/badge/deployment-headless_all-lightgrey)

Клиентский мод для SPT 4 + Fika. Согласование смерти ботов между хостом и клиентами в Fika coop. Предотвращает рассинхрон — когда на одном клиенте бот мёртв, а на другом ещё жив.

| | |
|---|---|
| **Разработчик** | [kabzon93region](https://github.com/kabzon93region) |
| **Версия** | 1.1.2 |
| **GitHub** | [FikaBotDeathReconcile](https://github.com/kabzon93region/FikaBotDeathReconcile) |
| **Deployment** | `(headless_all)` |
| **Тип** | client |

## Возможности

- Синхронизация статуса смерти ботов host↔client
- Предотвращение ghost-ботов (живы на одном клиенте, мертвы на другом)
- Минимальная нагрузка — только события смерти

## Установка

1. Скопировать `FikaBotDeathReconcile.dll` в `BepInEx/plugins/` (на headless-хосте и клиентах)

## Требования

- **Fika** headless-coop
- **SPT**: 4.0.x
- **BepInEx**: 5.4.x

## Совместимость

- `headless_all` — и на хосте, и на клиентах

## Поддержать проект

Разовый донат картой РФ, СБП, ЮMoney, VK Pay:
**[DonationAlerts → kabzon93region](https://www.donationalerts.com/r/kabzon93region)**

---

*Мод разработан при поддержке [Cursor AI](https://cursor.sh/) и Xiomi MiMo.*
