using UnityEngine;

/// <summary>
/// Debug hierarchy dump utility. Disabled in production builds.
/// Enable via KEBAB_DEBUG_HIERARCHY scripting define if needed.
/// </summary>
public class DumpHierarchy : MonoBehaviour
{
#if UNITY_EDITOR
    private bool dumped = false;

    void Update()
    {
        if (dumped) return;
        if (Time.time > 5f)
        {
            var sb = new System.Text.StringBuilder(4096);
            foreach (var o in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (o.transform.parent == null) Dump(sb, o, 0);
            }

            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(Application.persistentDataPath, "hierarchy.txt"),
                    sb.ToString());
                Debug.Log("[DumpHierarchy] Hierarchy written.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[DumpHierarchy] Write failed: " + e.Message);
            }

            dumped = true;
        }
    }

    void Dump(System.Text.StringBuilder sb, GameObject o, int level)
    {
        sb.Append(' ', level * 2);
        sb.Append(o.name);
        if (!o.activeSelf) sb.Append(" (I)");
        sb.AppendLine();
        foreach (Transform c in o.transform) Dump(sb, c.gameObject, level + 1);
    }
#endif
}
