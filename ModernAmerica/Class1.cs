using BepInEx;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[BepInPlugin("com.meeps.ModernAmerica", "Medieval America Overhaul Mod", "0.0.1")]
public class ModernAmerica : BaseUnityPlugin
{
    void Awake()
    {
        var harmony = new Harmony("com.meeps.ModernAmerica");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(MapManager), "LoadRandomLevel")]
class MapLoadPatch
{

    static bool Prefix(MapManager __instance)
    {
        if (__instance.currentMap)
        {
            PhotonNetwork.Destroy(__instance.currentMap);
        }

        int num = Random.Range(0, __instance.maps.Length - 1);
        __instance.requestedMapID = 0;
        GameObject gameObject = __instance.maps[0];

        __instance.currentMap = PhotonNetwork.Instantiate("Maps/" + gameObject.name, Vector3.zero, Quaternion.identity, 0, null);
        Debug.Log($"Successfully loaded map: {gameObject.name} (Index: {num})");
        return false;
    }
}