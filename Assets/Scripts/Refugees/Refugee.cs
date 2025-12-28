using UnityEngine;

public class Refugee : MonoBehaviour
{
    public bool IsFollowing = true;
    public bool OpenFollowAfterAppoarchingMode = true;
    public int Id;
    public float DetectDistance;
    private GameObject MainCharacter;
    [SerializeField] private Rigidbody2D Rigidbody2d;
    [SerializeField] private float Speed;
    [SerializeField] private float StoppingDistance;
    
    private void Awake() => MainCharacter = GameObject.FindWithTag("Player");

    private void Start() => IsFollowing = !OpenFollowAfterAppoarchingMode;

    private void FixedUpdate()
    {
        var followingObject = Id > 0 ? GetComponentInParent<RefugeeManager>().GetRefugee(Id - 1) : null;
        if(followingObject != null && !followingObject.GetComponent<Refugee>().IsFollowing)
            followingObject = MainCharacter;
        followingObject = followingObject ?? MainCharacter;

        if (OpenFollowAfterAppoarchingMode && Vector3.Distance(MainCharacter.transform.position, transform.position) <= DetectDistance && !IsFollowing)
        {
            IsFollowing = true;
            OpenFollowAfterAppoarchingMode = false;
        }
        if (IsFollowing && Vector2.Distance(this.transform.position, followingObject.transform.position) > StoppingDistance)
            Rigidbody2d.linearVelocity = ((Vector2)followingObject.transform.position - (Vector2)this.transform.position).normalized * Speed;
        else
            Rigidbody2d.linearVelocity *= 0.9f;
    }
}
