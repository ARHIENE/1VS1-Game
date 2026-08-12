using System.Collections.Generic;
using UnityEngine;

// 파워 게이지 정확도를 궤적 오차로 변환하는 공용 유틸
public static class PKTrajectoryUtil
{
    // gaugeValue: 0~1 (게이지 정지 위치). perfectHalfWidth: 0.5 기준 Perfect 구간의 정규화된 반폭
    // 반환값 0 = 완벽(오차 없음), 1 = 최대 오차
    public static float GetAccuracyError(float gaugeValue, float perfectHalfWidth)
    {
        float distFromCenter = Mathf.Abs(gaugeValue - 0.5f) / 0.5f; // 0(중앙) ~ 1(끝)
        if (distFromCenter <= perfectHalfWidth) return 0f;
        return Mathf.Clamp01((distFromCenter - perfectHalfWidth) / (1f - perfectHalfWidth));
    }

    // 원본 궤적에 오차를 적용해 실제 이동 궤적을 생성한다.
    // 오차는 시작점에서 0, 끝점으로 갈수록 커지도록 선형 보간하여 원본 궤적의 곡선 형태는 유지한다.
    public static Vector3[] ApplyError(List<Vector3> original, float errorAmount, float maxDeviation)
    {
        if (original == null || original.Count < 2)
            return original != null ? original.ToArray() : new Vector3[0];

        if (errorAmount <= 0f) return original.ToArray();

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (maxDeviation * errorAmount);

        Vector3 start = original[0];
        Vector3 end = original[original.Count - 1];
        float totalDist = Vector3.Distance(start, end);

        Vector3[] result = new Vector3[original.Count];
        for (int i = 0; i < original.Count; i++)
        {
            float t = totalDist > 0.001f
                ? Vector3.Distance(start, original[i]) / totalDist
                : (float)i / (original.Count - 1);
            result[i] = original[i] + offset * t;
        }
        return result;
    }
}
