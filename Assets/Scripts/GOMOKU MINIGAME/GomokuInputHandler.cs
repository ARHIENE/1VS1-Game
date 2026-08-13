using UnityEngine;

// 바둑판 지면(y = origin.y)과의 수학적 평면 교차로 (x,y) 교차점 클릭 처리
// + 선택 가능한 교차점에 바둑판 위 커서 마커 표시
//
// 예전엔 각 기둥마다 전체 높이(최대 15층)를 감싸는 콜라이더로 레이캐스트를 받았는데,
// 그 콜라이더들이 바닥부터 꼭대기까지 뚫려있는 벽처럼 동작해서 카메라가 낮은 각도로 볼 때
// 앞줄 기둥이 뒷줄 기둥들을 전부 가려버려 첫 줄 외에는 클릭이 씹히는 문제가 있었음.
// 콜라이더 대신 평면 수식(Plane.Raycast)으로 지면 교차점만 구하면 그런 가림 문제 자체가 없다.
public class GomokuInputHandler : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public GomokuStoneSpawner spawner;

    [Header("Column Marker")]
    [Tooltip("비워두면 기본 Quad로 생성")]
    public GameObject markerPrefab;
    public Color validColor = new Color(0f, 1f, 0f, 0.35f);
    public Color hoverColor = new Color(1f, 1f, 0f, 0.65f);

    private GameObject[,] markers;
    private Renderer[,] markerRenderers;

    private int hoverX = -1;
    private int hoverY = -1;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (spawner == null) spawner = FindAnyObjectByType<GomokuStoneSpawner>();
    }

    void Start()
    {
        BuildMarkers();
    }

    void BuildMarkers()
    {
        int size = GomokuGrid.Size;
        markers = new GameObject[size, size];
        markerRenderers = new Renderer[size, size];

        Transform root = new GameObject("GomokuMarkers").transform;
        root.SetParent(transform, false);

        float cell = spawner != null ? spawner.cellSize : 1.2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector3 basePos = spawner != null ? spawner.GridToWorld(x, y, 0) : new Vector3(x * cell, 0f, y * cell);

                // 선택 가능 여부를 나타내는 바둑판 위 교차점 마커
                GameObject marker = markerPrefab != null
                    ? Instantiate(markerPrefab, root)
                    : GameObject.CreatePrimitive(PrimitiveType.Quad);

                if (markerPrefab == null)
                {
                    marker.transform.SetParent(root, false);
                    marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    marker.transform.localScale = Vector3.one * (cell * 0.9f);
                    Collider markerCol = marker.GetComponent<Collider>();
                    if (markerCol != null) Destroy(markerCol);
                }

                // 바둑판 격자선(y=0.005) 바로 위에 그려 z-fighting 방지
                marker.transform.position = basePos + Vector3.up * 0.01f;
                marker.name = $"Marker_{x}_{y}";
                markers[x, y] = marker;
                Renderer rend = marker.GetComponent<Renderer>();
                if (rend != null)
                {
                    Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (unlitShader != null)
                    {
                        Material markerMat = new Material(unlitShader);
                        markerMat.SetFloat("_Surface", 1); // Transparent
                        markerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        markerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        markerMat.SetInt("_ZWrite", 0);
                        markerMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        markerMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        rend.material = markerMat;
                    }
                }
                markerRenderers[x, y] = rend;
            }
        }
    }

    void Update()
    {
        UpdateHover();
        RefreshMarkers();

        if (Input.GetMouseButtonDown(0) && hoverX >= 0)
        {
            GomokuTurnManager.Instance?.RequestPlace(hoverX, hoverY);
        }

        // Q/E: 현재 턴인 로컬 플레이어만 보드를 90도씩 좌/우로 돌릴 수 있음(자기 턴 아니면 GomokuTurnManager가 무시)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            GomokuTurnManager.Instance?.RequestRotateBoard(-1);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            GomokuTurnManager.Instance?.RequestRotateBoard(1);
        }
    }

    void UpdateHover()
    {
        hoverX = -1;
        hoverY = -1;
        if (targetCamera == null) return;

        float cell = spawner != null ? spawner.cellSize : 1.2f;
        Vector3 origin = spawner != null ? spawner.origin : Vector3.zero;
        Transform boardRoot = spawner != null ? spawner.BoardRoot : null;
        float half = (GomokuGrid.Size - 1) * cell * 0.5f;

        Plane boardPlane = new Plane(Vector3.up, origin);
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (!boardPlane.Raycast(ray, out float distance)) return;

        Vector3 hit = ray.GetPoint(distance);

        // 보드가 회전해 있어도 정확히 잡히도록, 보드 기준 로컬 좌표로 역변환 후 계산(회전과 무관하게 항상 정확)
        // boardRoot 피벗이 보드 "중심"이라 역변환 결과는 -half~+half 범위 → (0,0) 교차점 기준(0~extent)으로 다시 맞춰줌
        Vector3 local = boardRoot != null
            ? boardRoot.InverseTransformPoint(hit) + new Vector3(half, 0f, half)
            : hit - origin;

        int gx = Mathf.RoundToInt(local.x / cell);
        int gy = Mathf.RoundToInt(local.z / cell);

        if (gx < 0 || gx >= GomokuGrid.Size || gy < 0 || gy >= GomokuGrid.Size) return;

        // 가장 가까운 교차점에서 너무 멀면(칸 절반 이상) 선택 안 한 것으로 처리
        float dx = local.x - gx * cell;
        float dz = local.z - gy * cell;
        if (dx * dx + dz * dz > (cell * 0.5f) * (cell * 0.5f)) return;

        hoverX = gx;
        hoverY = gy;
    }

    void RefreshMarkers()
    {
        GomokuGrid grid = GomokuTurnManager.Instance?.Grid;
        if (grid == null || markers == null) return;

        for (int x = 0; x < GomokuGrid.Size; x++)
        {
            for (int y = 0; y < GomokuGrid.Size; y++)
            {
                bool canPlace = grid.CanPlace(x, y);
                GameObject marker = markers[x, y];
                if (marker == null) continue;

                marker.SetActive(canPlace);

                // 마커는 항상 현재 그 기둥에 쌓인 돌 맨 위(=다음에 놓일 자리)에 표시
                if (canPlace && spawner != null)
                {
                    int height = grid.GetColumnHeight(x, y);
                    marker.transform.position = spawner.GridToWorld(x, y, height) + Vector3.up * 0.01f;
                }

                if (markerRenderers[x, y] != null)
                {
                    bool isHover = (x == hoverX && y == hoverY);
                    markerRenderers[x, y].material.color = isHover ? hoverColor : validColor;
                }
            }
        }
    }
}
