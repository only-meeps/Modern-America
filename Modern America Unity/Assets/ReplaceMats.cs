using UnityEngine;

public class ReplaceMats : MonoBehaviour
{
    public Material replaceWith;
    public GameObject rootObject;

    public void PerformReplacement()
    {
        if (rootObject == null || replaceWith == null)
        {
            Debug.LogError("Assign both a Material and a Root Object!");
            return;
        }

        MeshRenderer[] renderers = rootObject.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.sharedMaterial == null)
            {
                renderer.sharedMaterial = replaceWith;
            }

        }

        Debug.Log($"Successfully replaced materials on {renderers.Length} objects.");
    }
}