using Il2CppInterop.Runtime;
using UnityEngine;

namespace BetterAmongUs.Helpers;

/// <summary>
/// Provides helper methods for working with Unity game prefabs in an Il2Cpp environment.
/// </summary>
internal static class GamePrefabHelper
{
    /// <summary>
    /// Retrieves a Unity prefab by its name from the loaded resources.
    /// </summary>
    /// <param name="objectName">The name of the prefab to find.</param>
    /// <returns>The Unity Object if found, or null if not found.</returns>
    internal static UnityEngine.Object? GetPrefabByName(string objectName)
    {
        UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(Il2CppType.Of<UnityEngine.Object>());

        var obj = allObjects.FirstOrDefault(obj => obj.hideFlags == HideFlags.None && obj.name == objectName);
        if (obj != null)
        {
            return obj;
        }

        return null;
    }
}