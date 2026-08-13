using UnityEngine;

// 1인칭 착석 시점 카메라: 바둑판을 사이에 두고 마주 앉은 자리에 배치
// pitch를 내리면 바둑판, 올리면 맞은편 상대방 얼굴이 보임. 좌우(yaw)는 제한적으로만 회전
// 우클릭 드래그로 시선 조절 — 좌클릭은 GomokuInputHandler의 기둥 선택에 쓰므로 겹치지 않게 분리
//
// 두 좌석 모두에 아바타(캡슐 몸통 + 구체 머리)를 로컬에서 동일하게 생성해 배치한다.
// 내 아바타는 내 화면에서는 보이지 않게 감추고, 머리 회전만 GomokuTurnManager의 RPC로 상대에게 전파해
// 상대 화면에 있는 "내 아바타"(상대 입장에선 맞은편 아바타)의 머리가 실시간으로 움직이게 한다.
// 별도 프리팹/Resources 등록 없이 기존 GomokuTurnManager의 PhotonView RPC만 재사용한다.
public class GomokuSeatedCamera : MonoBehaviour
{
    [Header("References")]
    public GomokuStoneSpawner spawner;
    [Tooltip("비워두면 캡슐+구체로 만든 기본 아바타 자동 생성")]
    public GameObject avatarPrefab;

    [Header("Seat")]
    // 15x15x15 보드는 cellSize 1.2 기준 가로 16.8 / 최대 높이 18에 달하는 거대한 규모라
    // 사람 스케일 고정값(예: eyeHeight 1.6)을 쓰면 카메라가 보드 바닥에 붙어 거의 수평으로 보임.
    // 보드 전체 폭(extent) 대비 배율로 계산해서, 보드 크기가 바뀌어도 항상 비례해 적절한 높이/거리가 나오게 함
    [Tooltip("보드 전체 폭 대비, 좌석이 보드 가장자리에서 추가로 떨어지는 거리 배율")]
    public float seatSetbackRatio = 0.375f;
    [Tooltip("보드 전체 폭 대비 눈높이 배율")]
    public float eyeHeightRatio = 0.6155f;

    [Header("Look")]
    public float lookSpeed = 0.15f;
    [Tooltip("아래로 내려다볼 수 있는 한계(바둑판 확인용, 음수)")]
    public float minPitch = -55f;
    [Tooltip("위로 올려볼 수 있는 한계(상대 얼굴 확인용, 양수)")]
    public float maxPitch = 20f;
    [Tooltip("좌우로 돌아볼 수 있는 한계(기준 정면에서 +-)")]
    public float maxYaw = 20f;
    [Tooltip("시작 시 pitch(음수 = 바둑판 쪽을 보고 시작)")]
    public float startPitch = -42.4f;

    private float baseYaw;
    private float yaw;
    private float pitch;
    private Vector3 lastMousePos;
    private bool dragging;

    private Transform myHead;
    private Transform opponentHead;

    void Start()
    {
        if (spawner == null) spawner = FindAnyObjectByType<GomokuStoneSpawner>();

        int localPlayer = GomokuTurnManager.Instance != null ? GomokuTurnManager.Instance.LocalPlayer : 1;

        Vector3 boardCenter = GetBoardCenter();
        float boardExtent = GetBoardHalfExtent() * 2f;
        float eyeHeight = boardExtent * eyeHeightRatio;
        float half = GetBoardHalfExtent() + boardExtent * seatSetbackRatio;

        Vector3 mySeat = boardCenter + new Vector3(0f, eyeHeight, localPlayer == 1 ? -half : half);
        Vector3 opponentSeat = boardCenter + new Vector3(0f, eyeHeight, localPlayer == 1 ? half : -half);

        transform.position = mySeat;
        pitch = startPitch;

        Vector3 flatDir = opponentSeat - mySeat;
        flatDir.y = 0f;
        baseYaw = flatDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(flatDir.normalized, Vector3.up).eulerAngles.y
            : 0f;

        ApplyLook();

        myHead = BuildAvatar(mySeat, opponentSeat, hideRenderers: true);
        opponentHead = BuildAvatar(opponentSeat, mySeat, hideRenderers: false);

        if (GomokuTurnManager.Instance != null)
        {
            GomokuTurnManager.Instance.OnOpponentHeadLook += HandleOpponentHeadLook;
        }
    }

    void OnDestroy()
    {
        if (GomokuTurnManager.Instance != null)
        {
            GomokuTurnManager.Instance.OnOpponentHeadLook -= HandleOpponentHeadLook;
        }
    }

    Vector3 GetBoardCenter()
    {
        float half = GetBoardHalfExtent();
        Vector3 origin = spawner != null ? spawner.origin : Vector3.zero;
        return origin + new Vector3(half, 0f, half);
    }

    float GetBoardHalfExtent()
    {
        float cell = spawner != null ? spawner.cellSize : 1.2f;
        return (GomokuGrid.Size - 1) * cell * 0.5f;
    }

    // seatPos에 아바타를 만들어 facingFrom 쪽을 바라보게 하고, 머리 Transform을 반환
    Transform BuildAvatar(Vector3 seatPos, Vector3 facingFrom, bool hideRenderers)
    {
        GameObject avatar;
        if (avatarPrefab != null)
        {
            avatar = Instantiate(avatarPrefab, seatPos, Quaternion.identity);
        }
        else
        {
            avatar = new GameObject(hideRenderers ? "MyAvatar" : "OpponentAvatar");
            avatar.transform.position = seatPos;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(avatar.transform, false);
            body.transform.localPosition = Vector3.down * 0.4f;
            body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Destroy(body.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(avatar.transform, false);
            head.transform.localPosition = Vector3.up * 0.55f;
            head.transform.localScale = Vector3.one * 0.4f;
            Destroy(head.GetComponent<Collider>());

            Renderer headRend = head.GetComponent<Renderer>();
            if (headRend != null) headRend.material.color = new Color(0.93f, 0.78f, 0.65f);
        }

        Vector3 lookDir = facingFrom - seatPos;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            avatar.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }

        if (hideRenderers)
        {
            // 내 아바타는 1인칭 시점상 내 눈에는 보이지 않아야 함(렌더러만 끔, 회전 로직은 계속 동작)
            foreach (Renderer rend in avatar.GetComponentsInChildren<Renderer>())
            {
                rend.enabled = false;
            }
        }

        Transform head2 = avatar.transform.Find("Head");
        return head2 != null ? head2 : avatar.transform;
    }

    void HandleOpponentHeadLook(float oPitch, float oYaw)
    {
        if (opponentHead != null)
        {
            opponentHead.localRotation = Quaternion.Euler(-oPitch, oYaw, 0f);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragging = true;
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1))
        {
            dragging = false;
        }

        if (dragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            yaw = Mathf.Clamp(yaw + delta.x * lookSpeed, -maxYaw, maxYaw);
            pitch = Mathf.Clamp(pitch + delta.y * lookSpeed, minPitch, maxPitch);

            ApplyLook();
        }

        if (myHead != null)
        {
            myHead.localRotation = Quaternion.Euler(-pitch, yaw, 0f);
        }

        GomokuTurnManager.Instance?.ReportLocalHeadLook(pitch, yaw);
    }

    void ApplyLook()
    {
        transform.rotation = Quaternion.Euler(-pitch, baseYaw + yaw, 0f);
    }
}
