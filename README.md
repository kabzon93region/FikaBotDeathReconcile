# Fika Bot Death Reconcile



**GitHub:** [kabzon93region](https://github.com/kabzon93region)

**Клиентский мод для SPT 4 + Fika.** Согласование смерти ботов между хостом и клиентами в Fika coop. Предотвращает рассинхрон — когда на одном клиенте бот мёртв, а на другом ещё жив.



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
