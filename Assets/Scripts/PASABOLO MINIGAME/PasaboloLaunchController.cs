using UnityEngine;

// 슬링샷 방식 발사 입력: 공을 클릭해 잡고 원하는 발사 방향의 반대쪽으로 당긴 뒤 놓으면
// 당김 벡터의 반대 방향으로 발사된다(새총 방식). 현재 로컬 턴일 때만 입력을 받는다.
// GomokuInputHandler처럼 씬에 하나만 존재하는 입력 컴포넌트라 별도 PhotonView는 필요 없고,
// 실제 동기화는 PasaboloTurnManager.RequestLaunch()의 RPC로 위임한다.
public class PasaboloLaunchController : MonoBehaviour
{
    [Header("References")]
    public Transform ballTransform;
    public Camera targetCamera;
    public PasaboloPowerGaugeUI powerGauge;
    public LineRenderer pullLine;

    [Header("Slingshot")]
    [Tooltip("공 근처 클릭 인식 반경(픽셀)")]
    public float startClickRadius = 80f;
    [Tooltip("최대 당김 거리(월드 단위) — 파워 상한선")]
    public float maxPullDistance = 3f;
    [Tooltip("최대로 당겼을 때의 발사 임펄스 크기")]
    public float maxLaunchForce = 15f;

    private bool dragging;
    private Vector3 currentPullVector;
    private Plane dragPlane;

    // 카메라 조준 시점이 참조하는 현재 당김 벡터(공 기준)
    public Vector3 CurrentPullVector => currentPullVector;
    public bool IsDragging => dragging;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (powerGauge == null) powerGauge = FindAnyObjectByType<PasaboloPowerGaugeUI>();

        if (ballTransform == null)
        {
            PasaboloTableSpawner spawner = FindAnyObjectByType<PasaboloTableSpawner>();
            if (spawner != null) ballTransform = spawner.ball;
        }

        if (pullLine == null)
        {
            pullLine = gameObject.AddComponent<LineRenderer>();
            pullLine.startWidth = pullLine.endWidth = 0.05f;
            pullLine.positionCount = 0;
            pullLine.useWorldSpace = true;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) pullLine.material = new Material(shader);
            pullLine.startColor = pullLine.endColor = Color.cyan;
        }
    }

    void Update()
    {
        if (PasaboloTurnManager.Instance == null || !PasaboloTurnManager.Instance.IsLocalTurn())
        {
            CancelDrag();
            return;
        }

        if (ballTransform == null || targetCamera == null) return;

        if (!dragging) HandleDragStart();
        else HandleDragging();
    }

    void HandleDragStart()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 ballScreen = targetCamera.WorldToScreenPoint(ballTransform.position);
        if (Vector2.Distance(Input.mousePosition, ballScreen) > startClickRadius) return;

        dragging = true;
        dragPlane = new Plane(Vector3.up, ballTransform.position);
        powerGauge?.Show();
    }

    void HandleDragging()
    {
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 planePoint = ray.GetPoint(enter);
            Vector3 pull = Vector3.ClampMagnitude(planePoint - ballTransform.position, maxPullDistance);
            currentPullVector = pull;

            DrawPullLine(ballTransform.position, ballTransform.position + pull);
            powerGauge?.SetPower(pull.magnitude / maxPullDistance);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseLaunch();
        }
    }

    void ReleaseLaunch()
    {
        dragging = false;
        ClearPullLine();
        powerGauge?.Hide();

        float power = Mathf.Clamp01(currentPullVector.magnitude / maxPullDistance);
        if (power <= 0.01f)
        {
            currentPullVector = Vector3.zero;
            return;
        }

        Vector3 launchDir = -currentPullVector.normalized;
        Vector3 force = launchDir * (power * maxLaunchForce);
        currentPullVector = Vector3.zero;

        PasaboloTurnManager.Instance?.RequestLaunch(force);
    }

    void CancelDrag()
    {
        if (!dragging) return;

        dragging = false;
        currentPullVector = Vector3.zero;
        ClearPullLine();
        powerGauge?.Hide();
    }

    void DrawPullLine(Vector3 from, Vector3 to)
    {
        if (pullLine == null) return;
        pullLine.positionCount = 2;
        pullLine.SetPosition(0, from);
        pullLine.SetPosition(1, to);
        pullLine.enabled = true;
    }

    void ClearPullLine()
    {
        if (pullLine == null) return;
        pullLine.positionCount = 0;
        pullLine.enabled = false;
    }
}
