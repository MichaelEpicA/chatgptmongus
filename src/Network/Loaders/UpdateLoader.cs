using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Network.Configs;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using UnityEngine;

namespace BetterAmongUs.Network.Loaders;

/// <summary>
/// Handles downloading and processing of update data from a remote repository.
/// </summary>
internal sealed class UpdateLoader : MonoBehaviour
{
    /// <summary>
    /// Gets the update information retrieved from the remote repository.
    /// </summary>
    /// <value>The update data, or null if not loaded.</value>
    internal static UpdateData? UpdateInfo { get; private set; }

    /// <summary>
    /// Coroutine to fetch update data from the remote repository.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// If no internet connection is detected, it retries several times before giving up.
    /// After successfully loading update data, it initializes the UpdateManager.
    /// </remarks>
    [HideFromIl2Cpp]
    internal IEnumerator CoFetchUpdateData()
    {
        int count = 0;
        float delay = 0;
        while (!GithubAPI.IsInternetAvailable())
        {
            count++;
            if (count >= 17)
            {
                Destroy(this);
                yield break;
            }
            if (delay < 30f) delay += 2.5f;
            yield return new WaitForSeconds(delay);
        }

        string callBack = "";
        yield return GitHubFile.CoDownloadManifest(GitUrlPath.RepositoryApi.Combine("update.json").ToString(), (string text) =>
        {
            callBack = text;
        });

        if (string.IsNullOrEmpty(callBack)) yield break;

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
        };

        var response = JsonSerializer.Deserialize<UpdateData>(callBack, options);

        if (response != null)
        {
            UpdateInfo = response;
            Logger_.Log($"Loaded update info");
        }

        UpdateManager.Init();

        Destroy(this);
    }
}