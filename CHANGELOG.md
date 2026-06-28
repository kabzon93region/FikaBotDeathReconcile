# Changelog

## v1.1.2 (2026-06-28) — Instant ragdoll

- **Fix задержки ~4 сек:** ragdoll/visual finalize сразу в postfix OnDead/Kill, не ждёт `IntervalSeconds` scan
- **FastRecheck:** повторы через 0 / 0.1 / 0.25 / 0.5 / 1 с (каждый кадр в Update)
- **Bugfix:** scheduled recheck больше не прекращается при появлении Corpse без ragdoll
- Timer grace по умолчанию: 6 → **1.5** сек (только fallback)

## v1.1.1 (2026-06-28) — Ragdoll + gunfire loop fix

- **Причина зависшей позы:** surgical `CreateCorpse` без шагов `OnDead` — аниматоры и `FirearmController` оставались активны
- **PrepareForCorpse:** `HandsController.OnPlayerDead`, отключение аниматоров/CC, `Speaker.Shut`
- **FinalizeCorpse:** принудительный старт ragdoll (`method_16`) + `ApplyCorpseImpulse`
- **FixStuckDeathVisual:** если труп уже есть, но модель застыла — доделать визуал
- **Звук стрельбы:** `BotDeathCombatAudioStop` + глушение мёртвых AI на **хосте** (listen-host FikaBot), не только remote client

## v1.1.0 (2026-06-28) — Boss corpse fix

- **Boss-only path:** отдельная логика для `IsBoss()` / `IsFollower()` (обычные боты без изменений)
- **Состояние A:** `IsAlive=true` + `BotOwner.IsDead` (или голова/грудь уничтожена) → `Kill(Bullet)` + recheck 4 сек
- **Состояние B:** `IsAlive=false` + нет `Corpse` → `CreateCorpse` / `OnDead(Undefined)` без проверки `BotOwner.IsDead`
- Postfix `FikaBot.OnDead` + recheck каждые 0.5 сек до 4 сек после смерти босса
- Host/client scan всех боссов в `GameWorld` / `ObservedPlayers`
- Логи: `[BOSS_CORPSE_FIX] stuck ...` (один раз на босса) + action lines
- Конфиг: `BossCorpseFixEnabled`

## v1.0.9 (2026-06-28) — Event-based corpse fix

- **П.1 OnDead postfix:** после `Player.OnDead` у AI — если `Corpse == null`, вызываются `CreateCorpse()` + `ApplyCorpseImpulse()` (ragdoll), **без** повторного Kill
- **П.2 Kill postfix:** после `ActiveHealthController.Kill` у AI — та же хирургическая доделка (страховка, если OnDead не вызвался)
- **П.3 таймер (v1.0.8):** fallback через grace — сначала surgical, потом `OnDead(Undefined)`
- Только **postfix**, без prefix на Kill (попадания не блокируются)
- Конфиг: `OnDeadCorpseFixEnabled`, `KillCorpseFixEnabled`, `HostCorpseFixEnabled` (timer)
- Логи: `[BOT_CORPSE_FIX] ondead_postfix` / `kill_postfix` / `host_corpse_timer`

## v1.0.8 (2026-06-28) — Safe corpse fix (timer only)

- **CorpseFix:** хост/клиент вызывают `OnDead(Undefined)` только если `!IsAlive`, `BotOwner.IsDead`, нет `Corpse`, прошёл grace (6 сек)
- **Не вызывает Kill** — попадания не затрагиваются (критерий противоположен старому «zombie» scan)
- Scan боссов: `CoopHandler` + `BotOwners` + `GameWorld` (AllAlive/Registered/AllPlayersEverExisted)
- Логи: `[BOT_CORPSE_FIX] host_corpse_fix` / `client_corpse_fix`
- Конфиг: секция `CorpseFix` — `HostCorpseFixEnabled`, `ClientCorpseFixEnabled`, grace seconds

## v1.0.7 (2026-06-28) — SAFE MODE

- **Критично:** убран `PatchAll(assembly)` — больше нет Harmony на `Player.OnDead` / `ActiveHealthController.Kill`
- **Критично:** хост **никогда** не вызывает `Kill`/`OnDead` по локальному scan (listen-host больше не ломает попадания)
- Host scan (`HostLocalScanEnabled`) — только мониторинг в лог (`would_zombie`), по умолчанию **выключен**
- Единственное действие на хосте: `Kill(Bullet)` по **client_report** (2+ ПК в coop)
- Удалены trace/boss/deferred/diagnostic патчи из сборки
- Harmony только: `ObservedPlayer.OnDead` + блок `OnPhraseTold` (audio на клиенте)

## v1.0.6 (2026-06-28) — HOTFIX (не использовать)

- **REGRESSION FIX:** v1.0.3–1.0.5 вызывали `Kill`/`OnDead` на живых или ещё умирающих ботах → пули проходили сквозь, ботов нельзя было убить
- Убраны агрессивные эвристики (`IsAlive`+`BotOwner.IsDead`, `IsBrokenBossCorpse`, vitals, GameWorld scan для reconcile)
- Убран Harmony-патч на `ActiveHealthController.Kill` и отложенный `deferred_kill`
- Host scan: только `FikaBot`; zombie = `!IsAlive` + нет Corpse + `!BotOwner.IsDead`
- Grace **3 сек** после `!IsAlive` перед `OnDead` (не прерываем нормальную смерть)
- `Kill(Bullet)` на хосте — **только** по `client_report`

## v1.0.5 (2026-06-28)

- Fix: AI-детекция — боссы не выпадали из scan после смерти (`AIData.IsAI` сбрасывается); учитываются `FikaBot`, `BotOwner`, роль boss/follower
- Debug: `DiagnosticLogging` — `[TRACE]` Kill/OnDead/BotHealthSync для боссов, `[DIAG]` снимок всех боссов каждые 15 сек
- Debug: `boss_not_zombie` с причиной, `ExplainZombieState` для каждого события
- Scan-логи при `VerboseLogging` или `DiagnosticLogging`

## v1.0.4 (2026-06-28)

- Боссы/фолловеры: scan через `GameWorld.AllAlivePlayersList`, `RegisteredPlayers`, `AllPlayersEverExisted` (не только `CoopHandler.Players`)
- Boss zombie: `!IsAlive` без трупа, `IsAlive` + уничтожена голова/грудь, «живой» Player + мёртвый Corpse
- Harmony: `Player.OnDead` / `FikaBot.OnDead` postfix — отложенный reconcile если у босса нет Corpse
- Verbose: `host scan ai=N zombies=M bossZombies=K`

## v1.0.3 (2026-06-28)

- Listen-host: zombie-детекция при `BotOwner.IsDead` + `HealthController.IsAlive` (зависшая модель без трупа)
- Отложенная проверка после `ActiveHealthController.Kill` (1.5 с и 4 с) — `deferred_kill`
- Host scan: дополнительный обход `BotsController.BotOwners` (боссы вне `CoopHandler.Players`)
- Fallback `HealthController` → `ActiveHealthController` при reconcile
- Verbose: строка `host scan bots=N zombies=M`

## v1.0.2 (2026-06-22)

- Клиент: полное глушение трупов ботов — `Speaker.Shut()`, `Clip = null`, остановка `_speechSource`
- Harmony: `ObservedPlayer.OnDead` (postfix) и блок `OnPhraseTold` для мёртвых observed-AI
- Периодическое перевыключение звука каждый кадр (не раз в 4 с)
- Конфиг: `Audio / ClientSilenceDeadBotAudio` (по умолчанию true)

## v1.0.1 (2026-06-22)

- Клиент: глушение звуков мёртвых observed-ботов (`ToggleMuteSpeechSource` + `AudioSource`)
- Логи: `[BOT_RECONCILE] silenced dead observed bot`

## v1.0.0 (2026-06-14)

- Первый релиз: reconcile zombie-ботов в Fika-coop
- Клиент → хост: отчёт о мёртвых `ObservedPlayer` (AI)
- Хост: `Kill(Bullet)` при client-dead / host-alive
- Хост: локальный scan `!IsAlive` без трупа → `OnDead`
- Конфиг: интервал 4 с, cooldown 5 с на NetId
- Логи: `[BOT_RECONCILE]`
