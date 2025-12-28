using UnityEngine;

public class Radar : MonoBehaviour
{
    [SerializeField] private float FloatingBallDistance;
    public string Name;
    
    private void Update()
    {
        TargetCoordsUpdate();
    }
    
    private void TargetCoordsUpdate()
    {
        var pos = Informations.PlayerPosition;
        var targetCoords = gameObject.GetComponentInParent<Status>().TargetCoords;
        foreach (var tc in targetCoords)
        {
            if(tc.Name != Name) continue;
            var dir = (tc.Position - pos).normalized;
            tc.Target.transform.position = new Vector2(dir.x * FloatingBallDistance + pos.x, dir.y * FloatingBallDistance + pos.y);
        }
    }
}
