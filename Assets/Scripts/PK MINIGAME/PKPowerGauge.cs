using System;
using UnityEngine;
using UnityEngine.UI;

// Canvas 위 파워게이지 오브젝트에 붙임
// 마우스 클릭으로 게이지를 정지시키며, Perfect(중앙)와의 거리로 슛 정확도가 결정된다.
public class PKPowerGauge : MonoBehaviour
{
    [Header("UI")]
    public Slider gaugeSlider;
    public Image fillImage;
    public Color lowColor  = Color.green;
    public Color midColor  = Color.yellow;
    public Color highColor = Color.red;

    [Header("Speed")]
    public float oscillateSpeed = 2.4f;   // 클수록 빠름

    [Header("Perfect Zone")]
    [Range(0f, 0.49f)]
    public float perfectHalfWidth = 0.08f; // 0.5(중앙) 기준 정규화된 반폭

    private bool  isRunning  = false;
    private float gaugeValue = 0f;        // 0 ~ 1
    private float direction  = 1f;

    // 게이지가 멈췄을 때(정상 정지든 강제 정지든) 최종 값과 함께 호출됨
    public event Action<float> OnStopped;

    public float GetCurrentPower() => gaugeValue;
    public float GetAccuracyError() => PKTrajectoryUtil.GetAccuracyError(gaugeValue, perfectHalfWidth);

    void Update()
    {
        if (!isRunning) return;

        gaugeValue += direction * oscillateSpeed * Time.deltaTime;

        if (gaugeValue >= 1f) { gaugeValue = 1f; direction = -1f; }
        else if (gaugeValue <= 0f) { gaugeValue = 0f; direction =  1f; }

        if (gaugeSlider != null) gaugeSlider.value = gaugeValue;

        if (fillImage != null)
        {
            float t = gaugeValue;
            fillImage.color = t < 0.5f
                ? Color.Lerp(lowColor, midColor, t * 2f)
                : Color.Lerp(midColor, highColor, (t - 0.5f) * 2f);
        }

        if (Input.GetMouseButtonDown(0))
        {
            StopGaugeInternal();
        }
    }

    public void StartGauge()
    {
        isRunning  = true;
        gaugeValue = 0f;
        direction  = 1f;
        if (gaugeSlider != null) gaugeSlider.gameObject.SetActive(true);
    }

    // 정상 정지(클릭)든 강제 정지(타임아웃)든 공용으로 사용
    void StopGaugeInternal()
    {
        if (!isRunning) return;
        isRunning = false;
        if (gaugeSlider != null) gaugeSlider.gameObject.SetActive(false);
        OnStopped?.Invoke(gaugeValue);
    }

    // 외부에서 강제로 즉시 정지시킬 때 사용 (이벤트 발생 없이 조용히 멈춤)
    public void StopGauge()
    {
        isRunning = false;
        if (gaugeSlider != null) gaugeSlider.gameObject.SetActive(false);
    }

    public void ResetGauge()
    {
        StopGauge();
        gaugeValue = 0f;
        if (gaugeSlider != null) gaugeSlider.value = 0f;
    }
}
