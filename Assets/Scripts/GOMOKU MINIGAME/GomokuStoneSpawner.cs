using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 그리드 좌표 <-> 월드 좌표 변환, 바둑판 베이스/격자선 생성, 돌 3D 오브젝트 생성/배치 담당
// 보드 전체(플레인+격자선+돌)는 회전 가능한 boardRoot 자식으로 묶여 있어서,
// GomokuTurnManager가 SetBoardRotation()을 부르면 이미 놓인 돌까지 통째로 같이 돌아간다.
public class GomokuStoneSpawner : MonoBehaviour
{
    [Header("Board Layout")]
    [Tooltip("(0,0,0) 교차점의 월드 좌표")]
    public Vector3 origin = Vector3.zero;
    public float cellSize = 1.2f;

    [Header("Stone")]
    [Tooltip("비워두면 기본 Sphere로 생성")]
    public GameObject stonePrefab;
    public Material player1Material;
    public Material player2Material;

    [Header("Board Base")]
    [Tooltip("비워두면 기본 나무색으로 생성")]
    public Material boardMaterial;
    public Color boardFallbackColor = new Color(0.62f, 0.45f, 0.26f);
    public Color gridLineColor = new Color(0.15f, 0.09f, 0.04f);
    public float gridLineWidth = 0.03f;
    [Tooltip("보드 가장자리에 남길 여유 공간(칸 단위)")]
    public float boardMargin = 1f;

    [Header("Rotation")]
    [Tooltip("Q/E로 90도 돌릴 때 애니메이션 시간(초)")]
    public float rotateDuration = 0.35f;

    private readonly Dictionary<Vector3Int, GameObject> stones = new Dictionary<Vector3Int, GameObject>();
    private Transform boardRoot;
    private Transform container;
    private Coroutine rotateRoutine;

    // 다른 스크립트(GomokuInputHandler)가 클릭 좌표를 boardRoot 기준 로컬 좌표로 역변환할 때 사용
    public Transform BoardRoot => boardRoot;

    void Awake()
    {
        float half = (GomokuGrid.Size - 1) * cellSize * 0.5f;

        // boardRoot의 피벗을 보드 "중심"에 둠 — 모서리(원점)에 두면 회전축이 꼭짓점이 되어
        // 돌리는 순간 보드 전체가 화면 밖으로 크게 튕겨나가는 문제가 있었음
        boardRoot = new GameObject("BoardRoot").transform;
        boardRoot.SetParent(transform, true);
        boardRoot.position = origin + new Vector3(half, 0f, half);
        boardRoot.rotation = Quaternion.identity;

        container = new GameObject("Stones").transform;
        container.SetParent(boardRoot, false);

        BuildBoardBase();
    }

    void BuildBoardBase()
    {
        float extent = (GomokuGrid.Size - 1) * cellSize;

        // 나무 재질 베이스 플레인 — boardRoot가 이미 중심에 있으므로 로컬 (0,0,0) 기준
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "BoardPlane";
        plane.transform.SetParent(boardRoot, false);

        float planeSize = extent + boardMargin * 2f * cellSize;
        plane.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        plane.transform.localScale = new Vector3(planeSize / 10f, 1f, planeSize / 10f); // 기본 Plane은 10x10 유닛

        Renderer planeRend = plane.GetComponent<Renderer>();
        if (planeRend != null)
        {
            if (boardMaterial != null) planeRend.material = boardMaterial;
            else planeRend.material.color = boardFallbackColor;
        }

        // 클릭 레이캐스트는 기둥 콜라이더만 반응하도록 베이스 콜라이더는 제거
        Collider planeCollider = plane.GetComponent<Collider>();
        if (planeCollider != null) Destroy(planeCollider);

        BuildGridLines(extent);
    }

    void BuildGridLines(float extent)
    {
        float half = extent * 0.5f;

        Transform linesRoot = new GameObject("BoardLines").transform;
        linesRoot.SetParent(boardRoot, false);

        for (int i = 0; i < GomokuGrid.Size; i++)
        {
            float offset = i * cellSize - half;
            CreateGridLine(linesRoot,
                new Vector3(offset, 0.005f, -half),
                new Vector3(offset, 0.005f, half));
            CreateGridLine(linesRoot,
                new Vector3(-half, 0.005f, offset),
                new Vector3(half, 0.005f, offset));
        }
    }

    void CreateGridLine(Transform parent, Vector3 localFrom, Vector3 localTo)
    {
        GameObject lineGO = new GameObject("GridLine");
        lineGO.transform.SetParent(parent, false);

        LineRenderer lr = lineGO.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = false;
        lr.SetPosition(0, localFrom);
        lr.SetPosition(1, localTo);
        lr.startWidth = lr.endWidth = gridLineWidth;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null) lr.material = new Material(shader);
        lr.startColor = lr.endColor = gridLineColor;
    }

    // 회전 중에도 항상 올바른 "현재 보드 기준" 월드 좌표를 반환(boardRoot의 현재 회전을 그대로 반영)
    // boardRoot 피벗이 보드 중심에 있으므로, (x,y)=(0,0) 교차점은 중심에서 half만큼 떨어진 로컬 오프셋이 됨
    public Vector3 GridToWorld(int x, int y, int z)
    {
        float half = (GomokuGrid.Size - 1) * cellSize * 0.5f;
        Vector3 localOffset = new Vector3(x * cellSize - half, z * cellSize, y * cellSize - half);
        return boardRoot != null ? boardRoot.TransformPoint(localOffset) : origin + new Vector3(x * cellSize, z * cellSize, y * cellSize);
    }

    public Vector3 GridToWorld(Vector3Int pos) => GridToWorld(pos.x, pos.y, pos.z);

    // steps: 0~3 (90도 단위). GomokuTurnManager가 RPC로 동기화한 뒤 호출
    public void SetBoardRotation(int steps)
    {
        if (boardRoot == null) return;

        float targetY = ((steps % 4) + 4) % 4 * 90f;
        Quaternion target = Quaternion.Euler(0f, targetY, 0f);

        if (rotateRoutine != null) StopCoroutine(rotateRoutine);
        rotateRoutine = StartCoroutine(RotateBoardRoutine(target));
    }

    IEnumerator RotateBoardRoutine(Quaternion target)
    {
        Quaternion start = boardRoot.rotation;
        float t = 0f;

        while (t < rotateDuration)
        {
            t += Time.deltaTime;
            boardRoot.rotation = Quaternion.Slerp(start, target, Mathf.Clamp01(t / rotateDuration));
            yield return null;
        }

        boardRoot.rotation = target;
    }

    public GameObject SpawnStone(Vector3Int pos, int player)
    {
        // GridToWorld(z)는 층의 바닥 기준 좌표이므로, 구 반지름(cellSize*0.4)만큼 띄워 바둑판/아래층 위에 걸치지 않고 얹히도록 보정
        Vector3 worldPos = GridToWorld(pos) + boardRoot.up * (cellSize * 0.4f);

        GameObject go;
        if (stonePrefab != null)
        {
            go = Instantiate(stonePrefab, worldPos, boardRoot.rotation, container);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(container, false);
            go.transform.position = worldPos;
            go.transform.rotation = boardRoot.rotation;
            go.transform.localScale = Vector3.one * (cellSize * 0.8f);
        }

        go.name = $"Stone_P{player}_{pos.x}_{pos.y}_{pos.z}";

        // 돌은 시각 전용 오브젝트 — 콜라이더를 두면 GomokuInputHandler의 기둥 클릭 레이캐스트를 가로막으므로 제거
        Collider stoneCollider = go.GetComponent<Collider>();
        if (stoneCollider != null) Destroy(stoneCollider);

        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = player == 1 ? player1Material : player2Material;
            if (mat != null)
            {
                rend.material = mat;
            }
            else
            {
                rend.material.color = player == 1 ? Color.black : Color.white;
            }
        }

        stones[pos] = go;
        return go;
    }

    public GameObject GetStoneAt(Vector3Int pos) => stones.TryGetValue(pos, out var go) ? go : null;

    public void ClearAll()
    {
        foreach (var kv in stones)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        stones.Clear();
    }
}
