using BepInEx;
using BetterAmongUs.Data;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Chat;
using HarmonyLib;
using System.Diagnostics;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Client;

[HarmonyPatch]
internal static class OptionsMenuBehaviourPatch
{
    internal static TabGroup? BetterOptionsTab { get; private set; }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(OptionsMenuBehaviour __instance)
    {
        BetterOptionsTab = CreateTabPage(__instance, Translator.GetString("BetterOption"));
        SetupAllClientOptions(__instance);
        UpdateTabPositions(__instance);
    }

    private static void SetupAllClientOptions(OptionsMenuBehaviour __instance)
    {
        if (__instance.DisableMouseMovement == null) return;

        ClientOptionItem.ClientOptions.Clear();

        // Toggle options with config binding
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.AntiCheat"), BAUPlugin.AntiCheat, __instance);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.SendBetterRpc"), BAUPlugin.SendBetterRpc, __instance, SendBetterRpcAction);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.BetterNotifications"), BAUPlugin.BetterNotifications, __instance, ClearNotifications);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.ForceOwnLanguage"), BAUPlugin.ForceOwnLanguage, __instance);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.ChatDarkMode"), BAUPlugin.ChatDarkMode, __instance, ChatPatch.SetChatTheme);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.ChatInGame"), BAUPlugin.ChatInGameplay, __instance);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.LobbyInfo"), BAUPlugin.LobbyPlayerInfo, __instance);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.LobbyTheme"), BAUPlugin.DisableLobbyTheme, __instance, ToggleLobbyTheme);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.UnlockFPS"), BAUPlugin.UnlockFPS, __instance, UpdateFrameRate);
        ClientOptionItem.CreateToggle(Translator.GetString("BetterOption.ShowFPS"), BAUPlugin.ShowFPS, __instance);

        // Button options (no toggle)
        ClientOptionItem.CreateButton(Translator.GetString("BetterOption.SaveData"), __instance, OpenSaveData, () =>
        {
            bool cannotOpen = GameState.IsInGame && !GameState.IsLobby;
            if (cannotOpen)
            {
                BetterNotificationManager.Notify($"Cannot open save data while in gameplay!", 2.5f);
            }
            return !cannotOpen;
        });

        ClientOptionItem.CreateButton(Translator.GetString("BetterOption.ToVanilla"), __instance, SwitchToVanilla, () =>
        {
            bool cannotSwitch = GameState.IsInGame;
            if (cannotSwitch)
            {
                BetterNotificationManager.Notify($"Unable to switch to vanilla while in game!", 2.5f);
            }
            return !cannotSwitch;
        });
    }

    private static void SwitchToVanilla()
    {
        ConsoleManager.DetachConsole();
        BetterNotificationManager.BAUNotificationManagerObj?.DestroyObj();
        Harmony.UnpatchAll();
        ModManager.Instance.ModStamp.gameObject.SetActive(false);
        SceneChanger.ChangeScene("MainMenu");
    }

    private static void SendBetterRpcAction()
    {
        if (!GameState.IsInGame) return;

        foreach (var player in BAUPlugin.AllPlayerControls)
        {
            if (player.IsLocalPlayer()) continue;
            player.BetterData().HandshakeHandler.ResendSecretToPlayer();
        }
    }

    private static void ClearNotifications()
    {
        BetterNotificationManager.NotifyQueue.Clear();
        BetterNotificationManager.showTime = 0f;
        BetterNotificationManager.Notifying = false;
    }

    private static void ToggleLobbyTheme()
    {
        if (GameState.IsLobby && !BAUPlugin.DisableLobbyTheme.Value)
        {
            SoundManager.instance.CrossFadeSound("MapTheme", LobbyBehaviour.Instance.MapTheme, 0.5f, 1.5f);
        }
    }

    private static void UpdateFrameRate()
    {
        Application.targetFrameRate = BAUPlugin.UnlockFPS.Value ? 165 : 60;
    }

    private static void OpenSaveData()
    {
        if (!File.Exists(BetterDataManager.dataPath)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = BetterDataManager.dataPath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private static TabGroup CreateTabPage(OptionsMenuBehaviour __instance, string name)
    {
        var tabPrefab = __instance.Tabs[^1];
        var tab = UnityEngine.Object.Instantiate(tabPrefab, tabPrefab.transform.parent);

        tab.name = $"{name}Button";
        tab.DestroyTextTranslators();
        tab.GetComponentInChildren<TextMeshPro>(true)?.SetText(name);
        tab.gameObject.SetActive(true);

        var content = new GameObject($"{name}Tab");
        content.SetActive(false);
        content.transform.SetParent(tab.Content.transform.parent);
        content.transform.localScale = Vector3.one;
        tab.Content = content;

        var tabs = new List<TabGroup>(__instance.Tabs) { tab };
        __instance.Tabs = tabs.ToArray();

        var index = __instance.Tabs.Length - 1;
        var button = tab.GetComponent<PassiveButton>();
        button.OnClick = new();
        button.OnClick.AddListener((Action)(() =>
        {
            tab.Rollover.SetEnabledColors();
            __instance.OpenTabGroup(index);
        }));

        return tab;
    }

    private static void UpdateTabPositions(OptionsMenuBehaviour __instance)
    {
        Vector3 basePos = new(0f, !GameState.InGame ? 0 : 2.5f, -1f);
        const float buttonSpacing = 0.6f;
        const float buttonWidth = 1.0f;

        int activeCount = 0;
        foreach (var tabButton in __instance.Tabs)
        {
            if (tabButton.gameObject.activeInHierarchy) activeCount++;
        }

        if (activeCount == 0) return;

        float totalWidth = (activeCount - 1) * buttonSpacing + activeCount * buttonWidth;
        float startX = basePos.x - (totalWidth / 2f) + (buttonWidth / 2f);

        int activeIndex = 0;
        foreach (var tabButton in __instance.Tabs)
        {
            if (!tabButton.gameObject.activeInHierarchy) continue;

            float xPos = startX + activeIndex * (buttonWidth + buttonSpacing);
            tabButton.transform.localPosition = new Vector3(xPos, basePos.y, basePos.z);
            activeIndex++;
        }
    }
}