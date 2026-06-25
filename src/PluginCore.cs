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
        internal ConfigEntry<bool> ClientSilenceDeadBotAudio;
        internal ConfigEntry<bool> VerboseLogging;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            ModLogger = Logger;
            BindConfig();
            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll(typeof(ObservedPlayerOnDeadAudioPatch).Assembly);
            gameObject.AddComponent<BotDeathReconcileBehaviour>();
            BotDeathReconcileNetwork.Instance.Initialize(Logger);
            Logger.LogInfo($"{PluginInfo.NAME} v{PluginInfo.VERSION} loaded");
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
                "Клиент шлёт отчёт о мёртвых observed-ботах на хост.");
            HostLocalScanEnabled = Config.Bind("General", "HostLocalScanEnabled", true,
                "Хост ищет zombie-ботов локально (!IsAlive без трупа).");
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
        }
    }
}
