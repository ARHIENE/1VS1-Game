using UnityEngine;

// 원작 파사볼로의 득점선(rayas) 가중치 채점 — 거리 -> 점수 변환 static 유틸
// (GomokuWinChecker/PKGoalZones와 동일한 순수 로직 컨벤션)
public static class PasaboloRayasScorer
{
    // rayaDistances/rayaWeights는 오름차순 병렬 배열. 넘은 마지막 선의 가중치를 반환하며,
    // 첫 번째 선 이전이라도 핀이 날아갔다면 최소 1점(스펙: "첫 번째 선 이전 = 1점")을 준다.
    public static int ComputeScore(float distance, float[] rayaDistances, int[] rayaWeights)
    {
        int score = 1;

        if (rayaDistances == null || rayaWeights == null) return score;

        int count = Mathf.Min(rayaDistances.Length, rayaWeights.Length);
        for (int i = 0; i < count; i++)
        {
            if (distance >= rayaDistances[i]) score = rayaWeights[i];
        }

        return score;
    }
}
