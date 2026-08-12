using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 골키퍼 화면 전용 UI — 골대 위에 반투명 1~6번 구역 패널을 표시
// 공격자 화면에서는 절대 활성화되지 않음 (PKGameManager가 역할에 따라 SetActive 호출)
public class PKGoalkeeperCursor : MonoBehaviour
{
    [Header("References")]
    public PKGoalkeeper goalkeeper;
    public Camera        mainCamera;
    public Canvas        canvas;

    [Header("Zone Panel Look")]
    public Vector2 panelSize   = new Vector2(90f, 70f);
    public Color   normalColor = new Color(1f, 1f, 1f, 0.25f);
    public Color   hoverColor  = new Color(1f, 0.85f, 0.2f, 0.5f);
    public int     fontSize    = 36;

    private RectTransform[] zonePanels = new RectTransform[PKGoalZones.Count];
    private Image[]         zoneImages = new Image[PKGoalZones.Count];
    private bool            isActive   = false;
    private bool            built      = false;

    void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        BuildZonePanels();
        SetActive(false);
    }

    void BuildZonePanels()
    {
        if (canvas == null || built) return;
        built = true;

        for (int i = 0; i < PKGoalZones.Count; i++)
        {
            int zone = i + 1;

            GameObject go = new GameObject($"Zone_{zone}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = panelSize;
            zonePanels[i] = rt;

            Image img = go.GetComponent<Image>();
            img.color = normalColor;
            img.raycastTarget = false;
            zoneImages[i] = img;

            GameObject textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = zone.ToString();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        for (int i = 0; i < zonePanels.Length; i++)
        {
            if (zonePanels[i] != null) zonePanels[i].gameObject.SetActive(active);
        }
    }

    void Update()
    {
        if (!isActive || canvas == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (goalkeeper == null && PKGameManager.Instance != null)
            goalkeeper = PKGameManager.Instance.goalkeeper;

        if (goalkeeper != null && goalkeeper.HasDived) { SetActive(false); return; }
        if (PKGameManager.Instance == null) return;

        Vector3 goalCenter = PKGameManager.Instance.goalCenter;
        float   halfWidth  = PKGameManager.Instance.goalHalfWidth;
        float   height     = PKGameManager.Instance.goalHeight;

        int hoverZone = ComputeHoverZone(goalCenter, halfWidth, height);

        for (int i = 0; i < PKGoalZones.Count; i++)
        {
            int zone = i + 1;
            Vector3 worldCenter = PKGoalZones.GetZoneCenter(zone, goalCenter, halfWidth, height);
            Vector3 screenPos   = mainCamera.WorldToScreenPoint(worldCenter);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out Vector2 localPoint
            );

            zonePanels[i].anchoredPosition = localPoint;
            zoneImages[i].color = zone == hoverZone ? hoverColor : normalColor;
        }
    }

    int ComputeHoverZone(Vector3 goalCenter, float halfWidth, float height)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Mathf.Abs(ray.direction.z) < 0.0001f) return 0;

        float t = (goalCenter.z - ray.origin.z) / ray.direction.z;
        if (t < 0f) return 0;

        Vector3 worldHit = ray.origin + ray.direction * t;
        if (!PKGoalZones.IsInsideGoal(worldHit, goalCenter, halfWidth, height)) return 0;

        return PKGoalZones.GetZoneFromPosition(worldHit, goalCenter, halfWidth, height);
    }
}
