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
        /*
        if (Input.GetKeyDown(KeyCode.P))
        {
            Time.timeScale = (Time.timeScale == 0f) ? 1f : 0f;
            Debug.Log("TimeScale toggled to: " + Time.timeScale);
        }
        */
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
        List<GameObject> maps = new List<GameObject>();
        for (int i = 0; i < bundledMapCount; i++)
        {
            GameObject map = ((GameObject)bundleObjects[0]).transform.GetChild(i).gameObject;
            Debug.Log($"Found object {map.name} in bundle");
            Utils.FixShaders(map);
            map.SetActive(true);
            currentMaps.Add(map);
            maps.Add(map);


        }
        for (int i = 0; i < maps.Count; i++)
        {
            maps[i].transform.parent = null;
            maps[i].transform.position = Vector3.zero;
            maps[i].transform.rotation = Quaternion.identity;
            if (maps[i].GetComponent<PhotonView>() == null)
            {
                PhotonView pv = maps[i].AddComponent<PhotonView>();
                pv.ViewID = 0;
            }
            Utils.RegisterPrefabWithPhoton(maps[i], "Maps");
        }
        __instance.maps = currentMaps.ToArray();
        for (int i = 0; i < __instance.maps.Length; i++)
        {
            Debug.Log($"Setting mapid of map {i}...");
            __instance.maps[i].GetComponent<Map>().mapID = i;
        }
        GameObject bombMapSample = __instance.maps[2];
        GameObject bombPrefab = null;
        foreach (Transform child in bombMapSample.GetComponentsInChildren<Transform>(true))
        {
            if (child.gameObject.name.Contains("MO_A_Bomb"))
            {
                bombPrefab = child.gameObject;
                bombPrefab.name = "MO_A_Bomb";
                bombPrefab.transform.parent = null;
                if (bombPrefab.GetComponent<PhotonView>() == null)
                {
                    PhotonView pv = bombPrefab.AddComponent<PhotonView>();
                    pv.ViewID = 0;
                }
                break;
            }
        }
        if (bombPrefab != null)
        {
            Utils.RegisterPrefabWithPhoton(bombPrefab, "Misc");
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
        __instance.currentMap = PhotonNetwork.Instantiate("Maps/" + __instance.maps[num].name, Vector3.zero, Quaternion.identity, 0, null);
        GameObject gameObject = __instance.currentMap;

        if (gameObject.name.ToLower().Contains("oldmap"))
        {
            Debug.Log("Scaling up old map!");
            gameObject.transform.localScale = new Vector3(100, 100, 100);
            gameObject.transform.position = new Vector3(0, -20, 0);

            foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains("Spawn"))
                {

                    child.gameObject.AddComponent<MapSpawnPosition>();
                    Debug.Log($"Successfully converted {child.name} to a functional MapSpawnPosition!");

                }
            }
            foreach (MeshCollider cl in gameObject.GetComponentsInChildren<MeshCollider>())
            {
                GameObject current = cl.gameObject;
                UnityEngine.Object.Destroy(cl);
                current.AddComponent<BoxCollider>();
            }
            Physics.SyncTransforms();
            foreach (Rigidbody rb in gameObject.GetComponentsInChildren<Rigidbody>())
            {
                rb.solverIterations = 20;
                rb.solverVelocityIterations = 20;
                rb.mass = 700;
                rb.ResetCenterOfMass();
                rb.ResetInertiaTensor();
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            Physics.SyncTransforms();

            foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
            {
                GameObject current = child.gameObject;
                Rigidbody rb = current.GetComponent<Rigidbody>();
                if (current.name.Contains("Bomb"))
                {
                    Vector3 position = current.transform.position;

                    PhotonNetwork.Instantiate("Misc/" + "MO_A_Bomb", position, Quaternion.identity);
                }
                else if (rb != null)
                {
                    Effectable effectable = current.AddComponent<Effectable>();
                    Effectable_MapObject effectableMap = current.AddComponent<Effectable_MapObject>();


                }
            }
        }


        //Time.timeScale = 0f;
        Debug.Log($"Successfully loaded map: {gameObject.name} (Index: {num})");
        return false;
    }
}
public static class Utils
{
    public static void RegisterPrefabWithPhoton(GameObject prefab, string prefix)
    {
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;

        if (pool != null)
        {
            string photonPath = prefix + "/" + prefab.name;

            if (!pool.ResourceCache.ContainsKey(photonPath))
            {
                pool.ResourceCache.Add(photonPath, prefab);
                Debug.Log($"Successfully registered {photonPath} in Photon PrefabPool at {photonPath}");
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