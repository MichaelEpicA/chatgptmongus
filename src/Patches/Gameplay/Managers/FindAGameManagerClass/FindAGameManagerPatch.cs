using BetterAmongUs.Helpers;
using HarmonyLib;
using InnerNet;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Managers;

[HarmonyPatch]
internal static class FindAGameManagerPatch
{
    public static Scroller? Scroller;

    [HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.Start))]
    [HarmonyPrefix]
    private static void FindAGameManager_Start_Prefix(FindAGameManager __instance)
    {
        __instance.refreshButton.gameObject.SetUIColors();
        __instance.BackButton.gameObject.SetUIColors();
        __instance.clearFilterButton.gameObject.SetUIColors("Disabled");
        __instance.serverButton.gameObject.SetUIColors("Inactive", "Disabled", "Background");
        __instance.serverButton.activeTextColor = Color.cyan * 0.3f;

        foreach (var con in __instance.gameContainers)
        {
            var roll = con.GetComponent<ButtonRolloverHandler>();
            roll.OverColor = (roll.OverColor * 0.6f) + (Color.green * 0.5f);
        }

        var prefab = __instance.gameContainers[4];
        var list = new GameObject("GameListScroller");
        list.transform.SetParent(prefab.transform.parent);

        Scroller = list.AddComponent<Scroller>();
        Scroller.Inner = list.transform;
        Scroller.MouseMustBeOverToScroll = true;
        var box = prefab.transform.parent.gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(100f, 100f);
        Scroller.ClickMask = box;
        Scroller.ScrollWheelSpeed = 0.3f;
        Scroller.SetYBoundsMin(0f);
        Scroller.SetYBoundsMax(3.5f);
        Scroller.allowY = true;

        foreach (var con in __instance.gameContainers)
        {
            con.transform.SetParent(list.transform);
            var oldPos = con.transform.position;
            con.transform.position = new Vector3(oldPos.x, oldPos.y, 25);
        }

        var oldGameContainers = __instance.gameContainers.ToList();

        for (int i = 0; i < 5; i++)
        {
            var GameContainer = UnityEngine.Object.Instantiate(prefab, list.transform);
            var oldPos = GameContainer.transform.position;
            GameContainer.transform.position = new Vector3(oldPos.x, oldPos.y - 0.75f * (i + 1), 25);
            oldGameContainers.Add(GameContainer);
        }

        __instance.gameContainers = oldGameContainers.ToArray();

        var cutOffTop = CreateBlackSquareSprite();
        cutOffTop.transform.SetParent(list.transform.parent);
        cutOffTop.transform.localPosition = new Vector3(0, 3, 1);
        cutOffTop.transform.localScale = new Vector3(1500, 200, 100);
    }

    [HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.RefreshList))]
    [HarmonyPostfix]
    private static void FindAGameManager_RefreshList_Postfix(FindAGameManager __instance)
    {
        Scroller?.ScrollRelative(new(0f, -100f));
    }

    [HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.HandleList))]
    [HarmonyPostfix]
    private static void FindAGameManager_HandleList_Postfix(FindAGameManager __instance, HttpMatchmakerManager.FindGamesListFilteredResponse response)
    {
        __instance.ResetContainers();
        GameListing[] games = response.Games.ToArray();

        games = [.. games.OrderByDescending(game => game.PlayerCount).ThenBy(game => game.TrueHostName)];
        int gameNum = 0;
        int count = 0;
        while (count < __instance.gameContainers.Length && count < games.Count())
        {
            if (games[count].Options != null)
            {
                __instance.gameContainers[gameNum].gameObject.SetActive(true);
                __instance.gameContainers[gameNum].SetGameListing(games[count]);
                __instance.gameContainers[gameNum].SetupGameInfo();
                gameNum++;
            }
            count++;
        }

        foreach (var container in __instance.gameContainers)
        {
            Transform child = container.transform.Find("Container");
            Transform tmproObject = child.Find("TrueHostName_TMP");

            TMP_Text tmpro = tmproObject != null
                ? tmproObject.GetComponent<TextMeshPro>()
                : CreateNewTextMeshPro(child);

            tmpro.font = container.capacity.font;
            tmpro.fontSize = 3f;
            tmpro.text = FormatGameInfoText(container.gameListing);
        }
    }

    private static TMP_Text CreateNewTextMeshPro(Transform parent)
    {
        var tmproObject = new GameObject("TrueHostName_TMP").transform;
        tmproObject.SetParent(parent, true);
        var aspectPos = tmproObject.gameObject.AddComponent<AspectPosition>();
        aspectPos.Alignment = AspectPosition.EdgeAlignments.Center;
        aspectPos.anchorPoint = new Vector2(0.2f, 0.5f);
        aspectPos.DistanceFromEdge = new Vector3(10.9f, -2.17f, -2f);
        aspectPos.AdjustPosition();
        return tmproObject.gameObject.AddComponent<TextMeshPro>();
    }

    private static string FormatGameInfoText(GameListing listing)
    {
        var hostStr = !string.IsNullOrEmpty(listing.TrueHostName) ? listing.TrueHostName : listing.HostName;
        return @$"{hostStr}{Environment.NewLine}<size=65%>{Utils.GetPlatformName(listing.Platform)} ({GameCode.IntToGameName(listing.GameId)})";
    }

    private static SpriteRenderer CreateBlackSquareSprite()
    {
        var square = new GameObject("CutOffTop");
        var renderer = square.AddComponent<SpriteRenderer>();
        Texture2D texture = new(100, 100);
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.black;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        renderer.sprite = sprite;
        square.transform.localScale = new Vector3(100, 100, 1);
        return renderer;
    }
}
