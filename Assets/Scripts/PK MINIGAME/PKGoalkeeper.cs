using System.Collections;
using UnityEngine;
using Photon.Pun;

// 골키퍼 플레이어 오브젝트에 붙임
// 골대를 1~6번 구역으로 나누고, 마우스 클릭으로 방어할 구역을 선택
public class PKGoalkeeper : MonoBehaviourPun
{
    [Header("Dive Settings")]
    public float diveSpeed    = 12f;
    public float diveDuration = 0.55f;
    public float returnSpeed  = 4f;

    [Header("GK Body")]
    public Transform gkBody;       // 골키퍼 시각적 오브젝트
    public Camera    mainCamera;

    // ── 다이브 상태 ──────────────────────────────
    private bool    isMyTurn    = false;
    private bool    hasDived    = false;
    private bool    isDiving    = false;
    private int     selectedZone = 0;
    private Vector3 diveTarget  = Vector3.zero;
    private Vector3 startPos;
    private float   diveTimer   = 0f;

    public bool HasDived     => hasDived;
    public int  SelectedZone => selectedZone;

    private Coroutine timerRoutine;

    Vector3 GoalCenter    => PKGameManager.Instance != null ? PKGameManager.Instance.goalCenter    : new Vector3(0f, 1.22f, 10f);
    float   GoalHalfWidth => PKGameManager.Instance != null ? PKGameManager.Instance.goalHalfWidth : 3.66f;
    float   GoalHeight    => PKGameManager.Instance != null ? PKGameManager.Instance.goalHeight    : 2.44f;
    float   TurnTimeLimit => PKGameManager.Instance != null ? PKGameManager.Instance.turnTimeLimit : 10f;

    public bool IsMyTurn
    {
        get => isMyTurn;
        set
        {
            isMyTurn     = value;
            hasDived     = false;
            isDiving     = false;
            selectedZone = 0;
            if (value) startPos = new Vector3(0f, 0f, 10f);

            Animator anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsGoalkeeper", value);
            }

            if (timerRoutine != null) { StopCoroutine(timerRoutine); timerRoutine = null; }
            if (value) timerRoutine = StartCoroutine(TurnTimer());
        }
    }

    void Awake()
    {
        startPos = new Vector3(0f, 0f, 10f);
        if (gkBody == null) gkBody = transform;
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        bool canSelect = isMyTurn && !hasDived;

        if (canSelect)
            HandleZoneClick();

        if (isDiving)
            UpdateDive();
        else if (isMyTurn && !hasDived && gkBody != null)
            gkBody.position = Vector3.MoveTowards(gkBody.position, startPos, returnSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  마우스 클릭 → 골대 안 1~6번 구역 중 하나 선택
    // ─────────────────────────────────────────────
    void HandleZoneClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (mainCamera == null) return;

        Vector3 goalCenter = GoalCenter;
        Ray   ray   = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Mathf.Abs(ray.direction.z) < 0.0001f) return;

        float t = (goalCenter.z - ray.origin.z) / ray.direction.z;
        if (t < 0f) return;

        Vector3 worldHit = ray.origin + ray.direction * t;
        int zone = PKGoalZones.GetZoneFromPosition(worldHit, goalCenter, GoalHalfWidth, GoalHeight);
        CommitZone(zone);
    }

    void CommitZone(int zone)
    {
        hasDived     = true;
        selectedZone = zone;

        if (timerRoutine != null) { StopCoroutine(timerRoutine); timerRoutine = null; }

        if (PKGameManager.Instance != null)
        {
            if (PKGameManager.Instance.gkCursor != null)
                PKGameManager.Instance.gkCursor.SetActive(false);

            PKGameManager.Instance.SetGoalkeeperReady(zone);
        }
    }

    // 제한시간(10초) 초과 시 1~6 중 랜덤 구역 자동 선택
    public void ForceRandomDive()
    {
        if (hasDived) return;
        CommitZone(PKGoalZones.RandomZone());
    }

    IEnumerator TurnTimer()
    {
        float remaining = TurnTimeLimit;
        while (remaining > 0f && !hasDived)
        {
            PKUIManager.Instance?.UpdateTimer(remaining);
            yield return null;
            remaining -= Time.deltaTime;
        }

        if (!hasDived)
        {
            PKUIManager.Instance?.UpdateTimer(0f);
            ForceRandomDive();
        }
    }

    // 실행 연출 단계에서 확정된 구역으로 다이빙 애니메이션 재생
    public void ExecuteDive(int zone)
    {
        hasDived     = true;
        selectedZone = zone;
        isDiving     = true;
        diveTimer    = 0f;
        diveTarget   = PKGoalZones.GetZoneCenter(zone, GoalCenter, GoalHalfWidth, GoalHeight);

        Animator anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsGoalkeeper", true);
            anim.SetTrigger($"GoalkeeperDive{zone}");
        }
    }

    void UpdateDive()
    {
        if (gkBody == null) return;
        diveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(diveTimer / diveDuration);
        gkBody.position = Vector3.Lerp(gkBody.position, diveTarget, t * diveSpeed * Time.deltaTime * 3f);

        if (t >= 1f || Vector3.Distance(gkBody.position, diveTarget) < 0.05f)
        {
            gkBody.position = diveTarget;
            isDiving        = false;
        }
    }
}
