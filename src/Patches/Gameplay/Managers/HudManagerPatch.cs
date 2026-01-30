using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules;
using HarmonyLib;
using TMPro;
using UnityEngine;


namespace BetterAmongUs.Patches.Gameplay.Managers;

[HarmonyPatch]
internal static class HudManagerPatch
{
    internal static string WelcomeMessage = $"<b><color=#00b530><size=125%><align=\"center\">{string.Format(Translator.GetString("WelcomeMsg.WelcomeToBAU"), Translator.GetString("BetterAmongUs"))}\n{BAUPlugin.GetVersionText()}</size>\n" +
        $"{Translator.GetString("WelcomeMsg.ThanksForDownloading")}</align></color></b>\n<size=120%> </size>\n" +
        string.Format(Translator.GetString("WelcomeMsg.BAUDescription1"), Translator.GetString("bau"), Translator.GetString("BetterOption.AntiCheat"));

    private static bool HasBeenWelcomed = false;

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    [HarmonyPostfix]
    private static void HudManager_Start_Postfix(HudManager __instance)
    {
        if (BetterNotificationManager.BAUNotificationManagerObj == null)
        {
            var ChatNotifications = __instance.Chat.chatNotification;
            if (ChatNotifications != null)
            {
                ChatNotifications.timeOnScreen = 1f;
                ChatNotifications.gameObject.SetActive(true);
                GameObject BAUNotification = UnityEngine.Object.Instantiate(ChatNotifications.gameObject);
                BAUNotification.name = "BAUNotification";
                BAUNotification.GetComponent<ChatNotification>().DestroyMono();
                GameObject.Find($"{BAUNotification.name}/Sizer/PoolablePlayer").DestroyObj();
                GameObject.Find($"{BAUNotification.name}/Sizer/ColorText").DestroyObj();
                BAUNotification.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(-1.57f, 5.3f, -15f);
                GameObject.Find($"{BAUNotification.name}/Sizer/NameText").transform.localPosition = new Vector3(-3.3192f, -0.0105f);
                BetterNotificationManager.NameText = GameObject.Find($"{BAUNotification.name}/Sizer/NameText").GetComponent<TextMeshPro>();
                UnityEngine.Object.DontDestroyOnLoad(BAUNotification);
                BetterNotificationManager.BAUNotificationManagerObj = BAUNotification;
                BAUNotification.SetActive(false);
                ChatNotifications.timeOnScreen = 0f;
                ChatNotifications.gameObject.SetActive(false);
                BetterNotificationManager.TextArea.enableWordWrapping = true;
                BetterNotificationManager.TextArea.m_firstOverflowCharacterIndex = 0;
                BetterNotificationManager.TextArea.overflowMode = TextOverflowModes.Overflow;
            }
        }

        LateTask.Schedule(() =>
        {
            if (!HasBeenWelcomed && GameState.IsInGame && GameState.IsLobby && !GameState.IsFreePlay)
            {
                BetterNotificationManager.Notify($"<b><color=#00751f>{string.Format(Translator.GetString("WelcomeMsg.WelcomeToBAU"), Translator.GetString("BetterAmongUs"))}!</color></b>", 8f);

                Utils.AddChatPrivate(WelcomeMessage, overrideName: " ");
                HasBeenWelcomed = true;
            }
        }, 1f, "HudManagerPatch Start");
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    private static void HudManager_Update_Postfix(HudManager __instance)
    {
        try
        {
            GameObject gameStart = GameObject.Find("GameStartManager");
            if (gameStart != null)
                gameStart.transform.SetLocalY(-2.8f);


            // Set chat
            if (GameState.InGame)
            {
                if (!BAUPlugin.ChatInGameplay.Value)
                {
                    if (!PlayerControl.LocalPlayer.IsAlive())
                    {
                        __instance.Chat.gameObject.SetActive(true);
                    }
                    else if (GameState.IsInGamePlay && !(GameState.IsMeeting || GameState.IsExilling))
                    {
                        __instance.Chat.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (__instance?.Chat?.gameObject.active == false)
                    {
                        __instance.Chat.gameObject.SetActive(true);
                    }
                }
            }
        }
        catch { }
    }
}
