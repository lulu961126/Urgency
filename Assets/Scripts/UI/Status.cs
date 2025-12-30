using System;
using System.Collections.Generic;
using UnityEngine;

public class Status : MonoBehaviour
{
    public List<TargetCoords> TargetCoords = new List<TargetCoords>();
    [SerializeField] private List<TargetCoords> PresetCoords = new List<TargetCoords>();
    [SerializeField] private Transform Player;

    //private void Start() => InitializeTargetCoords();
    
    //private void InitializeTargetCoords()
    //{
    //    foreach (var tc in PresetCoords)
    //    {
    //        var obj = Instantiate(tc.Target);
    //        obj.transform.position = Player.position;
    //        obj.transform.localPosition = new Vector3(0, 0, 0);
    //        TargetCoords.Add(new TargetCoords
    //        {
    //            Position = tc.Position,
    //            Target = obj,
    //            Name = tc.Name,
    //        });
    //    }
    //}
    
    public void AddCoord(Vector2 position, GameObject prefab, string coordName)
    {
        var obj = Instantiate(prefab);
        obj.transform.position = Player.position;
        obj.transform.localPosition = new Vector3(0, 0, 0);
        TargetCoords.Add(new TargetCoords
        {
            Position = position,
            Target = obj,
            Name = coordName
        });
    }

    public void RemoveCoords(string coordName) => TargetCoords.RemoveAll(t => t.Name == coordName);
}

[Serializable]
public class TargetCoords
{
    public Vector2 Position;
    public GameObject Target;
    public string Name;
}