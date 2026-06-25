# Changelog

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
