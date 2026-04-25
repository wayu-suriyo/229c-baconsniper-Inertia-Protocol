using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WindZone : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector2 windDirection = Vector2.up;
    public float windForce = 15f;

    [Header("Gust Settings")]
    public bool isGusting = false;
    public float gustMinForce = 5f;
    public float gustMaxForce = 25f;
    public float gustSpeed = 2f;

    [Header("Visuals")]
    public ParticleSystem windParticles;

    private float currentForce;

    void Start()
    {
        currentForce = windForce;
        UpdateParticleDirection();
    }

    void Update()
    {
        if (isGusting)
        {
            currentForce = Mathf.Lerp(gustMinForce, gustMaxForce, Mathf.PingPong(Time.time * gustSpeed, 1f));
        }
        else
        {
            currentForce = windForce;
        }

        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateParticleDirection();
        }
        #endif
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<DroneController>() != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(windDirection.normalized * currentForce, ForceMode2D.Force);
            }
        }
    }

    private void UpdateParticleDirection()
    {
        if (windParticles != null)
        {
            Vector3 dir3D = new Vector3(windDirection.x, windDirection.y, 0).normalized;
            if (dir3D != Vector3.zero)
            {
                windParticles.transform.rotation = Quaternion.LookRotation(dir3D, Vector3.back);
            }
            
            var main = windParticles.main;
            main.startSpeed = windForce * 0.5f; 
            
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                var shape = windParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                
                if (Mathf.Abs(windDirection.y) > Mathf.Abs(windDirection.x))
                {
                    shape.scale = new Vector3(col.size.x, 0.1f, 1f);
                }
                else
                {
                    shape.scale = new Vector3(col.size.y, 0.1f, 1f);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
        Gizmos.DrawCube(transform.position, GetComponent<BoxCollider2D>() != null ? GetComponent<BoxCollider2D>().size : Vector3.one);
        
        Gizmos.color = Color.cyan;
        Vector3 direction = new Vector3(windDirection.x, windDirection.y, 0).normalized;
        Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
        Gizmos.DrawSphere(transform.position + direction * 2f, 0.2f);
    }
}
