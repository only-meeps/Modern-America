using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
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

[HarmonyPatch(typeof(MapManager), "Start")]
class InjectOldMaps
{
    static bool Prefix(MapManager __instance)
    {
        Debug.Log("Starting MapManager...");
        MapManager.instance = __instance;
        //Debug.Log("Cloning List...");
        List<GameObject> currentMaps = new List<GameObject>(__instance.maps);
        //Debug.Log("Finding Parent...");
        //GameObject oldMapsObject = GameObject.Find("OldMaps");
        GameObject hiddenMap = null;

        //if (oldMapsObject != null)
        //{
        //    Debug.Log("Finding Child...");
        //    hiddenMap = oldMapsObject.transform.GetChild(0).gameObject;
        //    Debug.Log("Verifying...");
        //}
        //else
        //{
        //    Debug.Log("Unable to find parent");
        //}
        Debug.Log("Logging CurrentMaps");
        for (int i = 0; i < currentMaps.Count; i++)
        {
            Debug.Log("Idx: " + i);
            Debug.Log("Name: " + currentMaps[i].name);
            if (currentMaps[i].transform.parent != null)
            {
                Debug.Log("Parent: " + currentMaps[i].transform.parent.gameObject.name);
            }
            else
            {
                Debug.Log("Parent: None");
            }

        }
        hiddenMap = Resources.Load<GameObject>("OldMaps/Map1");
        if (hiddenMap != null)
        {
            Debug.Log("Loaded map!");
            currentMaps.Add(hiddenMap);

        }
        else
        {
            Debug.Log("Unable to find map");
        }
        __instance.maps = currentMaps.ToArray();
        for (int i = 0; i < __instance.maps.Length; i++)
        {
            __instance.maps[i].GetComponent<Map>().mapID = i;
        }
        return false;
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

        int num = Random.Range(0, __instance.maps.Length);
        __instance.requestedMapID = num;
        GameObject gameObject = __instance.maps[num];

        __instance.currentMap = PhotonNetwork.Instantiate("Maps/" + gameObject.name, Vector3.zero, Quaternion.identity, 0, null);
        Debug.Log($"Successfully loaded map: {gameObject.name} (Index: {num})");
        return false;
    }
}