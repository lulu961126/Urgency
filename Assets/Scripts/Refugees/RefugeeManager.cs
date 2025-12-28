using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RefugeeManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> Refugees = new List<GameObject>();
    [SerializeField] private GameObject RefugeePrefab;
    private bool _isFollowing = true;
    
    public void AddRefugee(Vector3? summonPosition = null, bool? follow = null, bool? OpenFollowAfterAppoarchingMode = null)
    {
        Refugees.Add(Instantiate(RefugeePrefab, this.transform));
        Refugees[^1].transform.position = summonPosition ?? (Vector3)Informations.PlayerPosition + new Vector3(0, -1 , 0);
        Refugees[^1].GetComponent<Refugee>().Id = Refugees.Count - 1;
        Refugees[^1].GetComponent<Refugee>().IsFollowing = follow ?? _isFollowing;
        Refugees[^1].GetComponent<Refugee>().OpenFollowAfterAppoarchingMode = OpenFollowAfterAppoarchingMode ?? false;
    }

    public void AddRefugeeWithDefaultSettings() => AddRefugee(null, null);

    public void AddRefugeeAtWorldSpawn() => AddRefugee(Vector3.zero, false, true);

    public void RemoveRefugee()
    {
        Destroy(Refugees[^1]);
        Refugees.Remove(Refugees[^1]);
    }
    
    public List<GameObject> GetAllRefugees() => Refugees;

    public GameObject GetRefugee(int id) => Refugees[id];

    public void RefugeeFollow()
    {
        _isFollowing = true;
        foreach (var refugee in Refugees)
        {
            refugee.GetComponent<Refugee>().IsFollowing = true;
        }
    }

    public void RefugeeStay()
    {
        _isFollowing = false;
        foreach (var refugee in Refugees)
        {
            refugee.GetComponent<Refugee>().IsFollowing = false;
        }
    }

    public int GetRefugeeCountInRadius(Vector2 position, float radius)
     => Refugees.Where(a => Vector2.Distance(a.transform.position, position) < radius).Count();
    
    public List<GameObject> GetRefugeesInRadius(Vector2 position, float radius)
     => Refugees.Where(a => Vector2.Distance(a.transform.position, position) < radius).ToList();
}