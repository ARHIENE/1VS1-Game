using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Collections;

namespace FPSMinigame
{
public class GunController : MonoBehaviourPun
{
    [Header("총 설정")]
    public float fireRate = 0.1f;
    public float range = 100f;

    [Header("레이어 설정")]
    public LayerMask hitLayer;

    [Header("UI")]
    public RectTransform crosshair;
    public float crosshairBaseSize = 10f;
    public float crosshairMaxSize = 35f;

    private FPSController fpsController;
    private float nextFireTime = 0f;
    private Camera cam;
    private float currentCrosshairSize = 10f;

    void Start()
    {
        if (!photonView.IsMine) return;

        fpsController = GetComponent<FPSController>();
        cam = GetComponentInChildren<Camera>();
        currentCrosshairSize = crosshairBaseSize;
        
        GameObject chObj = GameObject.Find("CrossHair");
        if (chObj != null)
        {
            crosshair = chObj.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        UpdateCrosshair();

        bool isShooting = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (isShooting && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void UpdateCrosshair()
    {
        if (crosshair == null || fpsController == null) return;

        float spread = fpsController.GetCurrentSpread();
        float maxSpreadVal = fpsController.maxSpread > 0f ? fpsController.maxSpread : 3f;
        
        float targetSize = Mathf.Lerp(crosshairBaseSize, crosshairMaxSize, spread / maxSpreadVal);
        
        // Smoothly interpolate the crosshair size with Time.deltaTime
        currentCrosshairSize = Mathf.Lerp(currentCrosshairSize, targetSize, Time.deltaTime * 18f);
        crosshair.sizeDelta = new Vector2(currentCrosshairSize, currentCrosshairSize);
    }

    void Shoot()
    {
        float spread = fpsController != null ? fpsController.GetCurrentSpread() : 0f;

        Vector3 shootDir = cam.transform.forward;
        
        // Add random spread based on current spread state
        shootDir += new Vector3(
            Random.Range(-spread, spread) * 0.015f,
            Random.Range(-spread, spread) * 0.015f,
            0
        );

        // Apply counter-inertia drift: offset bullet direction slightly opposite to current local velocity
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            Vector3 localVel = transform.InverseTransformDirection(cc.velocity);
            // Strafing right (localVel.x > 0) drifts bullet left, moving forward (localVel.z > 0) drifts bullet down
            Vector3 driftOffset = -cam.transform.right * (localVel.x * 0.005f) - cam.transform.up * (localVel.z * 0.003f);
            shootDir += driftOffset;
        }

        shootDir.Normalize();

        Ray ray = new Ray(cam.transform.position, shootDir);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

        // Force synchronization of bone transform animations to physics engine before casting rays!
        // This ensures the Head collider perfectly aligns with the current visible animation frame.
        Physics.SyncTransforms();

        RaycastHit[] hits = Physics.RaycastAll(ray, range, hitLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 endPoint = ray.origin + ray.direction * range;

        RaycastHit targetHeadHit = default;
        bool hitHead = false;
        
        RaycastHit targetBodyHit = default;
        bool hitBody = false;

        RaycastHit blockingObstacle = default;
        bool hitObstacle = false;

        foreach (var hit in hits)
        {
            // Ignore self-collisions
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            PhotonView pv = hit.collider.GetComponentInParent<PhotonView>();
            if (pv != null) // Hit a player (body or head)
            {
                if (!pv.IsMine)
                {
                    if (hit.collider.CompareTag("Head"))
                    {
                        if (!hitHead)
                        {
                            targetHeadHit = hit;
                            hitHead = true;
                        }
                    }
                    else
                    {
                        if (!hitBody)
                        {
                            targetBodyHit = hit;
                            hitBody = true;
                        }
                    }
                }
            }
            else // Hit wall, cover, floor
            {
                if (!hitObstacle)
                {
                    blockingObstacle = hit;
                    hitObstacle = true;
                }
            }
        }

        // Determine actual hit outcome
        float obstacleDist = hitObstacle ? blockingObstacle.distance : range;
        float headDist = hitHead ? targetHeadHit.distance : range;
        float bodyDist = hitBody ? targetBodyHit.distance : range;

        if (hitHead && headDist < obstacleDist)
        {
            // Prioritize Headshot!
            endPoint = targetHeadHit.point;
            
            PhotonView targetView = targetHeadHit.collider.GetComponentInParent<PhotonView>();
            if (targetView != null && !targetView.IsMine)
            {
                PlayerHealth targetHealth = targetView.GetComponent<PlayerHealth>();

                if (targetHealth != null && !targetHealth.IsDead)
                {
                    KillCam targetKillCam = targetView.GetComponent<KillCam>();
                    if (targetKillCam != null)
                        targetKillCam.photonView.RPC("SetKillerRPC",
                            targetView.Owner,
                            transform.position,
                            transform.rotation);

                    KillCam myKillCam = GetComponent<KillCam>();
                    if (myKillCam != null)
                        StartCoroutine(myKillCam.PlayKillerCam(targetView.transform));

                    targetView.RPC("OnHeadShot", RpcTarget.All);
                }
            }
        }
        else if (hitBody && bodyDist < obstacleDist)
        {
            // Blocked by opponent body capsule
            endPoint = targetBodyHit.point;
        }
        else if (hitObstacle)
        {
            // Blocked by wall/cover
            endPoint = blockingObstacle.point;
            
            // Spawn bullet hole decal on all clients
            photonView.RPC("RPC_SpawnBulletHole", RpcTarget.All, blockingObstacle.point, blockingObstacle.normal);
        }

        // Determine starting point (FirePoint child if present, else fallback to cam position)
        Vector3 startPoint = cam != null ? cam.transform.position : transform.position;
        Transform firePoint = transform.Find("FirePoint");
        if (firePoint != null)
        {
            startPoint = firePoint.position;
        }

        // Send RPC to show tracer to all clients
        photonView.RPC("RPC_ShowTracer", RpcTarget.All, startPoint, endPoint);
    }

    [PunRPC]
    void RPC_ShowTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerGO = new GameObject("BulletTracer");
        LineRenderer lr = tracerGO.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        
        // WHITE for local shooter, bright orange/red for enemy shooters
        Color laserColor = photonView.IsMine ? Color.white : new Color(1f, 0.2f, 0f, 1f);
        lr.startColor = laserColor;
        lr.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.1f);
        
        lr.startWidth = 0.04f;
        lr.endWidth = 0.01f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        StartCoroutine(FadeAndDestroyTracer(tracerGO, lr));
    }

    IEnumerator FadeAndDestroyTracer(GameObject obj, LineRenderer lr)
    {
        float duration = 0.12f;
        float elapsed = 0f;
        Color startCol = lr.startColor;
        Color endCol = lr.endColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (lr != null)
            {
                lr.startColor = Color.Lerp(startCol, new Color(startCol.r, startCol.g, startCol.b, 0f), t);
                lr.endColor = Color.Lerp(endCol, new Color(endCol.r, endCol.g, endCol.b, 0f), t);
            }
            yield return null;
        }

        Destroy(obj);
    }

    private static Sprite circleDecalSprite;

    private Sprite GetCircleDecalSprite()
    {
        if (circleDecalSprite == null)
        {
            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - 15.5f;
                    float dy = y - 15.5f;
                    float distSq = dx * dx + dy * dy;
                    if (distSq <= 15.5f * 15.5f)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            circleDecalSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        return circleDecalSprite;
    }

    [PunRPC]
    void RPC_SpawnBulletHole(Vector3 point, Vector3 normal)
    {
        GameObject hole = new GameObject("BulletHole");
        hole.transform.position = point + normal * 0.01f; // Offset slightly to prevent z-fighting
        hole.transform.rotation = Quaternion.LookRotation(normal);
        hole.transform.localScale = new Vector3(0.08f, 0.08f, 1f); // Small decal

        SpriteRenderer sr = hole.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleDecalSprite();
        sr.color = Color.black;

        Destroy(hole, 3.0f); // Destroy after 3 seconds
    }
}
}