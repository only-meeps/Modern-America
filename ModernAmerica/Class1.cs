using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Time.timeScale = (Time.timeScale == 0f) ? 1f : 0f;
            Debug.Log("TimeScale toggled to: " + Time.timeScale);
        }
    }
}

[HarmonyPatch(typeof(MapManager), "Start")]
class InjectOldMaps
{
    static bool Prefix(MapManager __instance)
    {
        Debug.Log("Starting MapManager...");
        MapManager.instance = __instance;
        List<GameObject> currentMaps = new List<GameObject>(__instance.maps);
        List<GameObject> hiddenMap = new List<GameObject>();
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

        string bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "oldmaps");
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        Object[] bundleObjects = bundle.LoadAllAssets();
        Debug.Log("Reading assetbundle at " + bundlePath + "...");
        int bundledMapCount = ((GameObject)bundleObjects[0]).transform.childCount;
        for (int i = 0; i < bundledMapCount; i++)
        {
            GameObject map = ((GameObject)bundleObjects[0]).transform.GetChild(i).gameObject;
            Debug.Log($"Found object {map.name} in bundle");
            Utils.FixShaders(map);
            map.SetActive(true);
            currentMaps.Add(((GameObject)bundleObjects[0]).transform.GetChild(i).gameObject);
            if (map.GetComponent<PhotonView>() == null)
            {
                PhotonView pv = map.AddComponent<PhotonView>();
                pv.ViewID = 0;
            }
            Utils.RegisterPrefabWithPhoton(map);

        }
        __instance.maps = currentMaps.ToArray();
        for (int i = 0; i < __instance.maps.Length; i++)
        {
            Debug.Log($"Setting mapid of map {i}...");
            __instance.maps[i].GetComponent<Map>().mapID = i;
        }
        bundle.Unload(false);
        return false;
    }
}
[HarmonyPatch(typeof(GM_Test), "SpawnPlayerOnMap")]
class PositionDebugger
{
    static bool Prefix(GM_Test __instance)
    {

        return true;
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
        if (gameObject.name.ToLower().Contains("oldmap"))
        {
            gameObject.transform.localScale = new Vector3(100, 100, 100);
        }
        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains("Spawn"))
            {

                child.gameObject.AddComponent<MapSpawnPosition>();
                Debug.Log($"Successfully converted {child.name} to a functional MapSpawnPosition!");

            }
        }
        foreach (var rb in gameObject.GetComponentsInChildren<Rigidbody>())
        {
            if (!rb.gameObject.name.ToLower().Contains("spawn"))
            {
                Object.Destroy(rb);
                continue;
            }
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
        __instance.currentMap = PhotonNetwork.Instantiate("Maps/" + gameObject.name, Vector3.zero, Quaternion.identity, 0, null);
        Time.timeScale = 0f;
        Debug.Log($"Successfully loaded map: {gameObject.name} (Index: {num})");
        return false;
    }
}
public static class Utils
{
    public static void RegisterPrefabWithPhoton(GameObject prefab)
    {
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;

        if (pool != null)
        {
            string photonPath = "Maps/" + prefab.name;

            if (!pool.ResourceCache.ContainsKey(photonPath))
            {
                pool.ResourceCache.Add(photonPath, prefab);
                Debug.Log($"Successfully registered {photonPath} in Photon PrefabPool");
            }
        }
        else
        {
            Debug.LogError("Photon PrefabPool is still null! Cannot register prefab yet.");
        }
    }
    public static void FixShaders(GameObject mapObject)
    {
        Shader gameShader = Shader.Find("Universal Render Pipeline/Lit");

        foreach (Renderer renderer in mapObject.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material mat in renderer.materials)
            {
                mat.shader = gameShader;
            }
        }
    }
}