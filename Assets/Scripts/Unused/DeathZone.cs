using UnityEngine;
using Photon.Pun;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Try to find AOSController in the hierarchy
        var aos = other.GetComponentInParent<AOSController>();
        var pv = other.GetComponentInParent<PhotonView>();
        var root = pv != null ? pv.gameObject : (aos != null ? aos.gameObject : other.transform.root.gameObject);

        if (other.CompareTag("Player") || pv != null || aos != null)
        {
            Debug.Log(root.name + " fell into the lava!");
            
            // If it's the local player (or non-networked), handle respawn
            if (pv == null || pv.IsMine)
            {
                // Stop the pull coroutine so they don't keep moving after respawn
                if (aos != null)
                {
                    aos.StopAllCoroutines();
                    // Reset health
                    aos.currentHealth = aos.maxHealth;
                    // Re-enable agent if it was disabled during pull (though pull is gone, good for safety)
                    var agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null) agent.enabled = true;
                }

                // Determine respawn side based on initial position or actor number
                float side = 1f;
                if (pv != null)
                {
                    side = (pv.Owner.ActorNumber % 2 == 0) ? 1f : -1f;
                }
                else
                {
                    side = root.transform.position.x > 0 ? 1f : -1f;
                }

                root.transform.position = new Vector3(120f * side, 55f, 0f);
}
        }
    }
}
