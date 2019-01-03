using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour {

    private static Dictionary<string, Stack<GameObject>> pool = new Dictionary<string, Stack<GameObject>>();

	public static void Pool(GameObject obj, int amount)
    {
        Stack<GameObject> instances = new Stack<GameObject>(amount);
        GameObject pooledObj;

        for (int i = 0; i < amount; i++)
        {
            pooledObj = Instantiate(obj);
            pooledObj.SetActive(false);
            instances.Push(pooledObj);
        }
        pool.Add(obj.name, instances);
    }

    private static List<GameObject> getList = new List<GameObject>(1);
    public static GameObject Get(string name)
    {
        getList.Clear();
        Get(name, getList, 1);
        return getList[0];
    }

    public static void Get(string name, List<GameObject> refList, int amount)
    {
        Stack<GameObject> instances;
        GameObject pooledObj;
        pool.TryGetValue(name, out instances);
        for (int i = 0; i < amount; i++)
        {
            pooledObj = instances.Pop();
            pooledObj.SetActive(true);
            refList.Add(pooledObj);
        }
    }

    public static void Clear()
    {
        int count;
        foreach (KeyValuePair<string, Stack<GameObject>> list in pool)
        {
            count = list.Value.Count;
            for (int i = 0; i < count; i++)
                Destroy(list.Value.Pop());
        }

        pool.Clear();
    }
}
