using System;
using UnityEngine;

// Token: 0x02000043 RID: 67
public class Map : MonoBehaviour
{
    // Token: 0x060000D0 RID: 208 RVA: 0x00004ED0 File Offset: 0x000030D0
    //private void Awake()
    //{
    //    Map.instance = this;
    //    Rigidbody[] componentsInChildren = base.GetComponentsInChildren<Rigidbody>();
    //    for (int i = 0; i < componentsInChildren.Length; i++)
    //    {
    //        componentsInChildren[i].mass *= this.massMultiplier;
    //    }
    //    MapManager.instance.loadedMapID = this.mapID;
    //}

    // Token: 0x060000D1 RID: 209 RVA: 0x00004F1D File Offset: 0x0000311D
    private void OnEnable()
    {
        Map.instance = this;
    }

    // Token: 0x060000D2 RID: 210 RVA: 0x00004F25 File Offset: 0x00003125
    private void OnDisable()
    {
        Map.instance = null;
    }

    // Token: 0x040000E9 RID: 233
    public int mapID;

    // Token: 0x040000EA RID: 234
    public static Map instance;

    // Token: 0x040000EB RID: 235
    public float massMultiplier = 1f;
}