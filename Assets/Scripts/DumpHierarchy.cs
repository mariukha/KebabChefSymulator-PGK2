using UnityEngine;

public class DumpHierarchy : MonoBehaviour
{
    private bool dumped = false;

    void Update()
    {
        if (dumped) return;
        if (Time.time > 5f)
        {
            string s = "";
            foreach(var o in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) 
            { 
                if(o.transform.parent == null) s += Dump(o, 0); 
            }
            System.IO.File.WriteAllText(Application.persistentDataPath + "/hierarchy.txt", s);
            Debug.Log("DUMPED HIERARCHY");
            dumped = true;
        }
    }

    string Dump(GameObject o, int l) 
    { 
        string s = new string(' ', l*2) + o.name + (o.activeSelf ? "" : " (I)") + "\n"; 
        foreach(Transform c in o.transform) s += Dump(c.gameObject, l+1); 
        return s; 
    }
}
