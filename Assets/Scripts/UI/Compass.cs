using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class Compass : MonoBehaviour
{
    [SerializeField] private List<CompassDirection> CompassDirections = new List<CompassDirection>();
    [SerializeField] private Transform Player;
    [SerializeField] private float CompassLength;

    private void Update()
    {
        Compass12DirectionsUpdate();
    }

    private void Compass12DirectionsUpdate()
    {
        var playerFacing = new Vector2(Player.up.x, Player.up.y).normalized;
        foreach (var cd in CompassDirections)
        {
            var newDir = cd.Direction.normalized;
            var facingFactor = Vector2.Dot(newDir, playerFacing);
            var degree = Mathf.Acos(facingFactor) * Mathf.Rad2Deg;
            var scale = Mathf.InverseLerp(0, 90, degree);
            var directionFactor = Vector3.Cross(playerFacing, newDir).normalized.z;
            var offset = CompassLength * scale * directionFactor;
            cd.Transform.localPosition = facingFactor > 0 ? new Vector3(offset, cd.Transform.localPosition.y) : new Vector3(10000, 0, 0);
        }
    }
}

[Serializable]
public class CompassDirection
{
    public Vector2 Direction;
    public RectTransform Transform;
}