using UnityEngine;
using System.Collections;

public class ChainProjectile : MonoBehaviour
{
    public float speed = 600f; // Extreme speed
    public float maxDistance = 3000f; // Map-wide range
    public float pullSpeed = 200f; // Super fast pull
    public float hitRadius = 10f; // Accurate hit radius for giant characters (approx human size at 25x)

    public Transform owner;
    public AOSController ownerAOS;

    private Vector3 startPos;
    private bool isPulling = false;
    private Transform targetHit;

    public void Launch(Vector3 direction)
    {
        startPos = transform.position;
        // Projectile will be moved manually in FixedUpdate for better precision
        StartCoroutine(DistanceCheck());
    }

    void FixedUpdate()
    {
        if (isPulling) return;

        Vector3 moveStep = transform.forward * speed * Time.fixedDeltaTime;
        
        // SphereCast for guaranteed hits
        RaycastHit hit;
        // Use a layer mask that includes the Default/Player layers
        int mask = ~LayerMask.GetMask("Ignore Raycast", "Water", "UI"); 

        if (Physics.SphereCast(transform.position, hitRadius, transform.forward, out hit, moveStep.magnitude, mask))
        {
            if (hit.transform.root != owner && !isPulling)
            {
                var targetAOS = hit.transform.root.GetComponentInChildren<AOSController>();
                if (targetAOS != null)
                {
                    Debug.Log("Chain hit target: " + targetAOS.name);
                    OnHitTarget(targetAOS);
                    return;
                }
            }
        }

        transform.position += moveStep;
    }

    void OnHitTarget(AOSController targetAOS)
    {
        isPulling = true; // Use this flag to stop projectile movement
        targetHit = targetAOS.transform;
        
        // Stop movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        // Deal Damage via RPC
        float damage = (ownerAOS != null) ? ownerAOS.hitDamage : 20f;
        targetAOS.photonView.RPC("RPC_TakeDamage", targetAOS.photonView.Owner, damage);

        // Spawn Hit Effect
        GameObject hitFx = Resources.Load<GameObject>("HitEffect");
        if (hitFx != null)
        {
            Instantiate(hitFx, transform.position, Quaternion.identity);
        }

        // Recover health for owner on successful hit
if (ownerAOS != null)
        {
            ownerAOS.photonView.RPC("RPC_RecoverHealth", ownerAOS.photonView.Owner, ownerAOS.hitRecovery);
        }
        
        Destroy(gameObject, 0.1f);
    }

    IEnumerator DistanceCheck()
    {
        while (!isPulling)
        {
            if (Vector3.Distance(startPos, transform.position) >= maxDistance)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator FollowTarget()
    {
        float duration = 2.5f; 
        float elapsed = 0;
        while (elapsed < duration && targetHit != null)
        {
            transform.position = targetHit.position;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // Secondary trigger check for safety
    void OnTriggerEnter(Collider other)
    {
        if (isPulling) return;
        if (other.transform == owner) return;

        var targetAOS = other.GetComponentInParent<AOSController>();
        if (targetAOS != null)
        {
            OnHitTarget(targetAOS);
        }
    }
}
