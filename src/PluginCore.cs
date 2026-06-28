using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class PluginCore : BaseUnityPlugin
    {
        internal static PluginCore Instance { get; private set; }

        internal ManualLogSource ModLogger { get; private set; }

        internal ConfigEntry<bool> Enabled;
        internal ConfigEntry<float> IntervalSeconds;
        internal ConfigEntry<bool> ClientReportsEnabled;
        internal ConfigEntry<bool> HostLocalScanEnabled;
        internal ConfigEntry<bool> BossCorpseFixEnabled;
        internal ConfigEntry<bool> OnDeadCorpseFixEnabled;
        internal ConfigEntry<bool> KillCorpseFixEnabled;
        internal ConfigEntry<bool> HostCorpseFixEnabled;
        internal ConfigEntry<float> HostCorpseFixGraceSeconds;
        internal ConfigEntry<bool> ClientCorpseFixEnabled;
        internal ConfigEntry<float> ClientCorpseFixGraceSeconds;
        internal ConfigEntry<bool> ClientSilenceDeadBotAudio;
        internal ConfigEntry<bool> VerboseLogging;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            ModLogger = Logger;
            BindConfig();
            _harmony = new Harmony(PluginInfo.GUID);
            // Только audio-патчи на ObservedPlayer — НЕ PatchAll (ломало Kill/OnDead на хосте).
            _harmony.PatchAll(typeof(ObservedPlayerOnDeadAudioPatch));
            _harmony.PatchAll(typeof(ObservedPlayerOnPhraseToldBlockPatch));
            BotDeathCorpsePatches.ApplyPatches(_harmony);
            gameObject.AddComponent<BotDeathReconcileBehaviour>();
            BotDeathReconcileNetwork.Instance.Initialize(Logger);
            Logger.LogInfo($"{PluginInfo.NAME} v{PluginInfo.VERSION} loaded (boss fix + death visual finalize + audio stop)");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            BotDeathReconcileNetwork.Instance.Shutdown();
        }

        private void BindConfig()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Включить reconcile смерти ботов в Fika-coop.");
            IntervalSeconds = Config.Bind("General", "IntervalSeconds", 4f,
                "Интервал сверки (сек). Рекомендуется 3–5.");
            ClientReportsEnabled = Config.Bind("General", "ClientReportsEnabled", true,
                "Клиент шлёт отчёт о мёртвых observed-ботах на хост. Хост: Kill только по этому отчёту.");
            HostLocalScanEnabled = Config.Bind("General", "HostLocalScanEnabled", false,
                "Только мониторинг на хосте (лог would_zombie). Не вызывает Kill/OnDead. Listen-host: держите false.");
            BossCorpseFixEnabled = Config.Bind("CorpseFix", "BossCorpseFixEnabled", true,
                "Боссы/фолловеры: Kill если BotOwner мёртв а HP жив; OnDead/CreateCorpse если HP мёртв без трупа; recheck 4 сек.");
            OnDeadCorpseFixEnabled = Config.Bind("CorpseFix", "OnDeadCorpseFixEnabled", true,
                "П.1: postfix Player.OnDead — CreateCorpse если труп не создался (мгновенно, без повторного Kill).");
            KillCorpseFixEnabled = Config.Bind("CorpseFix", "KillCorpseFixEnabled", true,
                "П.2: postfix ActiveHealthController.Kill — страховка после Kill (если OnDead не дошёл до трупа).");
            HostCorpseFixEnabled = Config.Bind("CorpseFix", "HostCorpseFixEnabled", true,
                "П.3: таймер на хосте — fallback если postfix не помог (grace сек). Не вызывает Kill.");
            HostCorpseFixGraceSeconds = Config.Bind("CorpseFix", "HostCorpseFixGraceSeconds", 1.5f,
                "Секунд ожидания перед timer fallback (не влияет на мгновенный ragdoll после OnDead).");
            ClientCorpseFixEnabled = Config.Bind("CorpseFix", "ClientCorpseFixEnabled", true,
                "П.3: таймер на клиенте — fallback для observed-AI без трупа.");
            ClientCorpseFixGraceSeconds = Config.Bind("CorpseFix", "ClientCorpseFixGraceSeconds", 1.5f,
                "Секунд ожидания перед client corpse fix.");
            ClientSilenceDeadBotAudio = Config.Bind("Audio", "ClientSilenceDeadBotAudio", true,
                "Клиент глушит предсмертные/посмертные звуки observed-ботов (Speaker.Shut + блок фраз).");
            VerboseLogging = Config.Bind("Debug", "VerboseLogging", false,
                "Подробные логи [BOT_RECONCILE].");

            if (IntervalSeconds.Value < 1f)
            {
                IntervalSeconds.Value = 1f;
            }
            else if (IntervalSeconds.Value > 30f)
            {
                IntervalSeconds.Value = 30f;
            }

            if (HostCorpseFixGraceSeconds.Value < 2f)
            {
                HostCorpseFixGraceSeconds.Value = 2f;
            }
            else if (HostCorpseFixGraceSeconds.Value > 30f)
            {
                HostCorpseFixGraceSeconds.Value = 30f;
            }

            if (ClientCorpseFixGraceSeconds.Value < 2f)
            {
                ClientCorpseFixGraceSeconds.Value = 2f;
            }
            else if (ClientCorpseFixGraceSeconds.Value > 30f)
            {
                ClientCorpseFixGraceSeconds.Value = 30f;
            }
        }
    }
}
