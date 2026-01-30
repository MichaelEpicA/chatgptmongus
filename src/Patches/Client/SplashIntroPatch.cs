using BetterAmongUs.Helpers;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Client;

[HarmonyPatch]
internal static class SplashIntroPatch
{
    internal static bool Skip = false;
    internal static bool BetterIntro = false;
    internal static bool IsReallyDoneLoading = false;
    private static GameObject? BetterLogo;

    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Start))]
    [HarmonyPrefix]
    private static void SplashManager_Start_Prefix(SplashManager __instance)
    {
        Skip = false;
        BetterIntro = false;
        IsReallyDoneLoading = false;
        __instance.logoAnimFinish.transform.Find("BlackOverlay").transform.SetLocalY(100f);
    }

    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
    [HarmonyPrefix]
    private static bool SplashManager_Update_Prefix(SplashManager __instance)
    {
        if (Skip)
        {
            CheckIfDone(__instance);
            return false;
        }

        if (Time.time - __instance.startTime > 1.8f && BetterIntro)
        {
            UnityEngine.Object.Destroy(__instance.logoAnimFinish.GetComponent<AudioSource>());
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (CheckIfDone(__instance, true))
            {
                Skip = true;
                return false;
            }
        }

        if (__instance.doneLoadingRefdata && !__instance.startedSceneLoad && Time.time - __instance.startTime > __instance.minimumSecondsBeforeSceneChange)
        {
            if (!BetterIntro)
            {
                __instance.startTime = Time.time;
                __instance.logoAnimFinish.gameObject.SetActive(false);
                __instance.logoAnimFinish.gameObject.SetActive(true);
                GameObject InnerLogo = __instance.logoAnimFinish.transform.Find("LogoRoot/ISLogo").gameObject;
                BetterLogo = UnityEngine.Object.Instantiate(InnerLogo, InnerLogo.transform.parent);
                InnerLogo.DestroyObj();
                BetterLogo.name = "BetterLogo";
                BetterLogo.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("BetterAmongUs.Resources.Images.BetterAmongUs-Logo.png", 150f);
                __instance.logoAnimFinish.transform.Find("BlackOverlay").transform.SetLocalY(0f);

                BetterIntro = true;
                return false;
            }

            CheckIfDone(__instance);
        }

        return false;
    }

    private static bool CheckIfDone(SplashManager __instance, bool isSkip = false)
    {
        if (Time.time - __instance.startTime > 2f && BetterIntro || isSkip && BetterIntro)
        {
            IsReallyDoneLoading = true;
            __instance.sceneChanger.AllowFinishLoadingScene();
            __instance.startedSceneLoad = true;
            __instance.loadingObject.SetActive(true);
            return true;
        }

        return false;
    }
}