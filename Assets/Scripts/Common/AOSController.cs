using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Photon.Pun;

public class AOSController : MonoBehaviourPun
{
    public float movementSpeed = 40f;
    public GameObject fireballPrefab; 
    public Transform firePoint;

    [Header("Health & Skill")]
    public float maxHealth = 730f;
    public float currentHealth = 730f;
    public float healthRegen = 1f;
    public float skillCost = 50f;
    public float hitDamage = 65f;
    public float hitRecovery = 50f;
    public float skillCooldown = 4f;
    
    private float lastSkillTime = -10f;
    private bool hasUsedD = false;
    private bool hasUsedF = false;

    private NavMeshAgent agent;
    private Camera mainCam;
    private Animator animator;

    void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
        
        animator = GetComponentInChildren<Animator>(); 
        mainCam = Camera.main;
        
        if (firePoint == null)
        {
            firePoint = transform.Find("FirePoint");
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.parent = transform;
                fp.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
                firePoint = fp.transform;
            }
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    void Update()
    {
        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame) HandleMovement();
            
            if (Keyboard.current.qKey.wasPressedThisFrame && Time.time >= lastSkillTime + skillCooldown)
            {
                if (currentHealth > skillCost) HandleSkill();
            }
            
            if (Keyboard.current.dKey.wasPressedThisFrame && !hasUsedD)
                HandleBlink(ref hasUsedD);
            
            if (Keyboard.current.fKey.wasPressedThisFrame && !hasUsedF)
                HandleBlink(ref hasUsedF);

            RegenHealth();
            UpdateAnimations();
        }
    }

    void RegenHealth()
    {
        if (currentHealth < maxHealth)
        {
            currentHealth += healthRegen * Time.deltaTime;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
        }
    }

    void HandleBlink(ref bool usedFlag)
    {
        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            usedFlag = true;
            Vector3 targetPos = hit.point;
            targetPos.y = transform.position.y;
            
            float maxBlinkDist = 80f;
            float actualDist = Vector3.Distance(transform.position, targetPos);
            
            Vector3 blinkPos;
            if (actualDist <= maxBlinkDist)
            {
                blinkPos = targetPos;
            }
            else
            {
                Vector3 targetDir = (targetPos - transform.position).normalized;
                blinkPos = transform.position + targetDir * maxBlinkDist;
            }
            
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(blinkPos, out navHit, 15f, NavMesh.AllAreas)) blinkPos = navHit.position;

            transform.position = blinkPos;
            if (agent != null && agent.enabled) agent.Warp(blinkPos);
            Debug.Log(name + " Blinked!");
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;
        float targetSpeed = (agent.enabled && agent.hasPath) ? (agent.velocity.magnitude / agent.speed) : 0f;
        if (targetSpeed > 0.1f && targetSpeed < 0.5f) targetSpeed = 0.5f; 
        animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", true);
    }

    void HandleMovement()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            if (!hit.collider.CompareTag("Player")) agent.SetDestination(hit.point);
        }
    }

    void HandleSkill()
    {
        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            lastSkillTime = Time.time;
            Vector3 lookTarget = hit.point;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);

            Vector3 targetPoint = hit.point;
            targetPoint.y = firePoint.position.y; 
            Vector3 launchDir = (targetPoint - firePoint.position).normalized;

            TakeDamage(skillCost);

            if (PhotonNetwork.IsConnected)
                photonView.RPC("RPC_FireProjectile", RpcTarget.All, firePoint.position, launchDir);
            else
                FireProjectile(firePoint.position, launchDir);
        }
    }

    [PunRPC]
    void RPC_FireProjectile(Vector3 pos, Vector3 dir) => FireProjectile(pos, dir);

    void FireProjectile(Vector3 pos, Vector3 dir)
    {
        if (fireballPrefab == null) return;
        GameObject projectile = Instantiate(fireballPrefab, pos, Quaternion.LookRotation(dir));
        ChainProjectile proj = projectile.GetComponent<ChainProjectile>();
        if (proj != null)
        {
            proj.owner = transform;
            proj.ownerAOS = this;
            proj.Launch(dir);
        }
    }

    void OnGUI()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
        GUIStyle style = new GUIStyle { fontSize = 24 };
        style.normal.textColor = Color.white;
        
        float cdRemaining = Mathf.Max(0, (lastSkillTime + skillCooldown) - Time.time);
        
        string status = string.Format("HP: {0} / {1}\nQ CD: {2:F1}s\nD Blink: {3}\nF Blink: {4}", 
            Mathf.RoundToInt(currentHealth), 
            maxHealth, 
            cdRemaining, 
            (hasUsedD ? "Used" : "Ready"), 
            (hasUsedF ? "Used" : "Ready"));
            
        GUI.Label(new Rect(20, 20, 600, 200), status, style);
        
        GUI.color = currentHealth < 200 ? Color.red : Color.green;
        GUI.Box(new Rect(20, 180, currentHealth * 0.5f, 20), "");
    }

    public void TakeDamage(float damage)
    {
        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            currentHealth -= damage;
            if (currentHealth <= 0) 
            { 
                currentHealth = 0; 
                if (PhotonNetwork.IsConnected) photonView.RPC("RPC_Die", RpcTarget.All);
                else Die(); 
            }
            if (PhotonNetwork.IsConnected) photonView.RPC("RPC_SyncHealth", RpcTarget.All, currentHealth);
        }
    }

    [PunRPC]
    void RPC_Die()
    {
        Die();
    }

    private void Die()
    {
        if (animator != null) animator.SetTrigger("Knockdown");
        if (agent != null) agent.isStopped = true;
        
        // Start respawn delay
        StartCoroutine(RespawnRoutine());
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f); // Wait for animation

        currentHealth = maxHealth;
        float side = (photonView.Owner != null && photonView.Owner.ActorNumber % 2 == 0) ? 1f : -1f;
        Vector3 spawnPos = new Vector3(140f * side, 75.5f, 0f);
        
        // Reset Position and state
        transform.position = spawnPos;
        if (agent != null && agent.enabled)
        {
            agent.Warp(spawnPos);
            agent.isStopped = false;
        }

        // Reset Animator
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        
        Debug.Log(name + " Respawned and state reset.");
    }

    [PunRPC]
    public void RPC_RecoverHealth(float amount)
    {
        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            if (PhotonNetwork.IsConnected) photonView.RPC("RPC_SyncHealth", RpcTarget.All, currentHealth);
        }
    }

    [PunRPC] void RPC_SyncHealth(float health) => currentHealth = health;
    [PunRPC] public void RPC_TakeDamage(float damage) => TakeDamage(damage);
}
