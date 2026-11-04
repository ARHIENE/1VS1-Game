using UnityEngine;
using UnityEngine.UI;

// 파워 게이지 UI. 프리팹 없이 Canvas/Slider를 런타임에 직접 생성한다
// (GomokuStoneSpawner의 "프리팹 비우면 자동 생성" 관행을 UI에도 적용해 수동 Unity 작업을 최소화)
public class PasaboloPowerGaugeUI : MonoBehaviour
{
    [Header("직접 만든 UI를 쓰고 싶으면 연결, 비워두면 자동 생성")]
    public Slider gaugeSlider;
    public Image fillImage;

    public Color lowColor = Color.green;
    public Color highColor = Color.red;

    private GameObject generatedRoot;

    void Awake()
    {
        if (gaugeSlider == null) BuildRuntimeGauge();
        Hide();
    }

    void BuildRuntimeGauge()
    {
        GameObject canvasGO = new GameObject("PasaboloPowerGaugeCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject sliderGO = new GameObject("PowerSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        RectTransform sliderRt = sliderGO.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0.06f);
        sliderRt.anchorMax = new Vector2(0.5f, 0.06f);
        sliderRt.sizeDelta = new Vector2(300f, 24f);

        Slider slider = sliderGO.AddComponent<Slider>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.4f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero; fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero; fillAreaRt.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = lowColor;
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

        slider.fillRect = fillRt;
        slider.targetGraphic = fillImage;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        gaugeSlider = slider;
        generatedRoot = canvasGO;
    }

    public void Show()
    {
        if (generatedRoot != null) generatedRoot.SetActive(true);
        else if (gaugeSlider != null) gaugeSlider.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (generatedRoot != null) generatedRoot.SetActive(false);
        else if (gaugeSlider != null) gaugeSlider.gameObject.SetActive(false);
    }

    public void SetPower(float t)
    {
        t = Mathf.Clamp01(t);
        if (gaugeSlider != null) gaugeSlider.value = t;
        if (fillImage != null) fillImage.color = Color.Lerp(lowColor, highColor, t);
    }
}
