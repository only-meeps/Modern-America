using BepInEx;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
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
        MapLoadPatch networkListener = new MapLoadPatch();
        PhotonNetwork.AddCallbackTarget(networkListener);
        ModVersion.version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
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

public static class ModVersion
{
    public static string version;
}

[HarmonyPatch(typeof(GM_Test), "GetSpawnPoint")]
class PatchSpawn
{
    static bool Prefix(GM_Test __instance, ref Transform __result, int teamID)
    {
        __result = Object.FindObjectsOfType<MapSpawnPosition>()[teamID].transform;
        Debug.Log("Spawn points in map: " + Object.FindObjectsOfType<MapSpawnPosition>().Length);
        return false;
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


            GameObject gameObject = maps[i];
            if (gameObject.name.ToLower().Contains("oldmap"))
            {
                Debug.Log("Scaling up old map!");

                if (gameObject.GetComponent<PhotonView>() == null)
                {
                    PhotonView pv = gameObject.AddComponent<PhotonView>();
                    //PhotonTransformView view = gameObject.AddComponent<PhotonTransformView>();
                    pv.ViewID = 0;
                    //pv.ObservedComponents.Add(view);
                }


                gameObject.transform.localScale = new Vector3(100, 100, 100);


                Debug.Log("Extracting textures from retail maps...");
                List<Material> defaultMats = new List<Material>();
                List<Transform> children = __instance.maps[0].GetComponentsInChildren<Transform>().ToList();
                children.AddRange(__instance.maps[1].GetComponentsInChildren<Transform>().ToList());
                children.AddRange(__instance.maps[2].GetComponentsInChildren<Transform>().ToList());
                children.AddRange(__instance.maps[3].GetComponentsInChildren<Transform>().ToList());
                children.AddRange(__instance.maps[4].GetComponentsInChildren<Transform>().ToList());
                children.AddRange(__instance.maps[5].GetComponentsInChildren<Transform>().ToList());

                foreach (Transform child in children)
                {
                    if (child.GetComponent<MeshRenderer>() != null)
                    {
                        if (defaultMats.FindIndex(x => child.GetComponent<MeshRenderer>().sharedMaterial.name.Contains(x.name)) == -1)
                        {
                            defaultMats.Add(child.GetComponent<MeshRenderer>().sharedMaterial);
                        }
                    }
                }
                Debug.Log("Textures extracted! List is as follows...");
                foreach (Material mat in defaultMats)
                {
                    Debug.Log(mat.name);
                }
                Debug.Log("Loading textures...");
                foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
                {
                    if (child.GetComponent<MeshRenderer>() != null)
                    {
                        int findIdx = defaultMats.FindIndex(x => child.GetComponent<MeshRenderer>().sharedMaterial.name.Contains(x.name));
                        if (findIdx != -1)
                        {
                            //child.GetComponent<MeshRenderer>().sharedMaterial = defaultMats[findIdx];
                            Material stolenMat = defaultMats[findIdx];
                            Material instanceMat = new Material(stolenMat);
                            instanceMat.DisableKeyword("_WORLDPOS_ON");
                            instanceMat.DisableKeyword("_TRIPLANAR_ON");
                            instanceMat.SetFloat("_WorldSpaceMapping", 0f);

                            child.GetComponent<MeshRenderer>().material = instanceMat;
                        }
                    }
                    Debug.Log($"Object {child.gameObject.name} : Active: {child.gameObject.activeSelf} : Map : {i}");
                    if (child.name.ToLower().Contains("spawn"))
                    {
                        if (child.GetComponent<MapSpawnPosition>() != null)
                        {
                            UnityEngine.Object.Destroy(child.GetComponent<MapSpawnPosition>());
                        }
                        child.gameObject.AddComponent<MapSpawnPosition>();
                        Debug.Log("Fixed spawn");
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

                    if (rb != null)
                    {
                        Effectable effectable = current.AddComponent<Effectable>();
                        Effectable_MapObject effectableMap = current.AddComponent<Effectable_MapObject>();


                    }
                }

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
class MapLoadPatch : IOnEventCallback
{
    private const byte MAP_MOVE_EVENT_CODE = 99;

    static bool Prefix(MapManager __instance)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            if (__instance.currentMap) PhotonNetwork.Destroy(__instance.currentMap);
            return false;
        }

        if (__instance.currentMap)
        {
            PhotonNetwork.Destroy(__instance.currentMap);
        }

        int num = Random.Range(0, __instance.maps.Length);
        __instance.requestedMapID = num;
        string mapName = __instance.maps[num].name;

        __instance.currentMap = PhotonNetwork.Instantiate("Maps/" + mapName, Vector3.zero, Quaternion.identity, 0, null);

        if (mapName.ToLower().Contains("oldmap"))
        {
            __instance.currentMap.transform.position = new Vector3(0, -25, 0);

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.AddToRoomCache
            };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            foreach (Transform child in __instance.currentMap.GetComponentsInChildren<Transform>())
            {
                if (child.name.Contains("Bomb"))
                {
                    Vector3 position = child.transform.position;

                    PhotonNetwork.Instantiate("Misc/" + "MO_A_Bomb", position, Quaternion.identity);
                }
            }

            int mapViewID = __instance.currentMap.GetComponent<PhotonView>().ViewID;
            PhotonNetwork.RaiseEvent(MAP_MOVE_EVENT_CODE, mapViewID, raiseEventOptions, sendOptions);
        }

        Debug.Log($"Successfully loaded map: {mapName} (Index: {num})");
        return false;
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == MAP_MOVE_EVENT_CODE)
        {
            int targetViewID = (int)photonEvent.CustomData;
            PhotonView targetView = PhotonView.Find(targetViewID);

            if (targetView != null)
            {
                targetView.gameObject.transform.position = new Vector3(0, -23, 0);
                Debug.Log("Successfully moved map on client via Network Event.");
            }
        }
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