using UnityEngine;

// 테이블(판, 시각 전용/비네트워크)은 GomokuStoneSpawner 관행대로 완전 자동 생성.
// 공/핀은 Photon 씬 오브젝트라 PhotonView가 씬 저장 시점에 에디터에서 미리 붙어 있어야 하므로,
// 여기서는 씬에 배치된 Transform을 받아 외형/콜라이더/Rigidbody가 비어있을 때만 기본값을 채워 넣는다.
public class PasaboloTableSpawner : MonoBehaviour
{
    [Header("Table (완전 자동 생성)")]
    public Vector3 tableOrigin = Vector3.zero;
    public float tableLength = 20f;
    public float tableWidth = 5f;
    [Tooltip("비워두면 기본 색상으로 생성")]
    public Material tableMaterial;
    public Color tableFallbackColor = new Color(0.55f, 0.4f, 0.24f);

    [Header("Rayas 득점선 (tableOrigin.z 기준 거리, 오름차순)")]
    public float[] rayaDistances = { 5f, 10f, 15f };
    public Color rayaLineColor = new Color(0.9f, 0.85f, 0.2f);

    [Header("Ball / Pins (PhotonView 필요 — 씬에 미리 배치한 오브젝트 연결)")]
    [Tooltip("씬에 미리 만든 공 오브젝트(PhotonView 부착). 외형/Rigidbody/Collider가 없으면 자동으로 채움")]
    public Transform ball;
    [Tooltip("씬에 미리 만든 핀 3개(PhotonView 부착)")]
    public Transform[] pins = new Transform[3];

    public float ballRadius = 0.25f;
    public float pinRadius = 0.15f;
    public float pinHeight = 0.6f;

    public Vector3 TableCenter => tableOrigin + new Vector3(0f, 0f, tableLength * 0.5f);
    public Vector3 BallStartPosition { get; private set; }
    public Vector3[] PinStartPositions { get; private set; }

    void Awake()
    {
        BuildTable();
        BuildRayaLines();
        EnsureBall();
        EnsurePins();
    }

    void BuildTable()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "PasaboloTable";
        plane.transform.SetParent(transform, false);
        plane.transform.position = TableCenter;
        plane.transform.localScale = new Vector3(tableWidth / 10f, 1f, tableLength / 10f); // 기본 Plane은 10x10 유닛

        Renderer rend = plane.GetComponent<Renderer>();
        if (rend != null)
        {
            if (tableMaterial != null) rend.material = tableMaterial;
            else rend.material.color = tableFallbackColor;
        }
    }

    void BuildRayaLines()
    {
        Transform linesRoot = new GameObject("RayaLines").transform;
        linesRoot.SetParent(transform, false);

        if (rayaDistances == null) return;

        foreach (float d in rayaDistances)
        {
            GameObject lineGO = new GameObject($"Raya_{d}");
            lineGO.transform.SetParent(linesRoot, false);

            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            Vector3 z = tableOrigin + Vector3.forward * d;
            lr.SetPosition(0, z + Vector3.left * (tableWidth * 0.5f) + Vector3.up * 0.01f);
            lr.SetPosition(1, z + Vector3.right * (tableWidth * 0.5f) + Vector3.up * 0.01f);
            lr.startWidth = lr.endWidth = 0.05f;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) lr.material = new Material(shader);
            lr.startColor = lr.endColor = rayaLineColor;
        }
    }

    void EnsureBall()
    {
        if (ball == null) return;

        if (ball.GetComponentInChildren<Renderer>() == null)
        {
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vis.name = "Visual";
            vis.transform.SetParent(ball, false);
            vis.transform.localScale = Vector3.one * ballRadius * 2f;
            Destroy(vis.GetComponent<Collider>()); // 콜라이더는 ball 루트에 별도로 둠
        }

        if (ball.GetComponent<Collider>() == null)
        {
            SphereCollider col = ball.gameObject.AddComponent<SphereCollider>();
            col.radius = ballRadius;
        }

        if (ball.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = ball.gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.2f;
        }

        BallStartPosition = ball.position;
    }

    void EnsurePins()
    {
        PinStartPositions = new Vector3[pins.Length];

        for (int i = 0; i < pins.Length; i++)
        {
            Transform pin = pins[i];
            if (pin == null) continue;

            if (pin.GetComponentInChildren<Renderer>() == null)
            {
                GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                vis.name = "Visual";
                vis.transform.SetParent(pin, false);
                vis.transform.localScale = new Vector3(pinRadius * 2f, pinHeight * 0.5f, pinRadius * 2f);
                Destroy(vis.GetComponent<Collider>());
            }

            if (pin.GetComponent<Collider>() == null)
            {
                CapsuleCollider col = pin.gameObject.AddComponent<CapsuleCollider>();
                col.radius = pinRadius;
                col.height = pinHeight;
            }

            if (pin.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = pin.gameObject.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.linearDamping = 0.3f;
                rb.angularDamping = 0.3f;
            }

            PinStartPositions[i] = pin.position;
        }
    }

    public void SnapPinsToPositions(Vector3[] positions)
    {
        if (positions == null) return;
        for (int i = 0; i < pins.Length && i < positions.Length; i++)
        {
            if (pins[i] != null) pins[i].position = positions[i];
        }
    }

    // 다음 턴을 위해 공/핀을 시작 위치로 되돌림. 양쪽 클라이언트가 동일한 결정론적 좌표를 사용하므로
    // 물리 권위 여부와 무관하게 호출 가능(비마스터는 Rigidbody가 isKinematic이라 물리와 충돌하지 않음)
    public void ResetForNextTurn()
    {
        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = BallStartPosition;
            }
            else
            {
                ball.position = BallStartPosition;
            }
        }

        for (int i = 0; i < pins.Length; i++)
        {
            if (pins[i] == null) continue;

            Rigidbody rb = pins[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = PinStartPositions[i];
            }
            else
            {
                pins[i].position = PinStartPositions[i];
            }
        }
    }
}
