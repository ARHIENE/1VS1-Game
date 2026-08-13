using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 승리 라인에 해당하는 돌들을 색상 변경 + 펄스 애니메이션으로 강조
public class GomokuVisualHighlighter : MonoBehaviour
{
    public GomokuStoneSpawner spawner;
    public Color highlightColor = Color.red;
    public float pulseScale = 1.3f;
    public float pulseSpeed = 3f;

    private Coroutine pulseRoutine;

    void Awake()
    {
        if (spawner == null) spawner = FindAnyObjectByType<GomokuStoneSpawner>();
    }

    public void HighlightWinLine(List<Vector3Int> winLine)
    {
        if (spawner == null || winLine == null) return;

        var stones = new List<GameObject>();
        foreach (Vector3Int pos in winLine)
        {
            GameObject stone = spawner.GetStoneAt(pos);
            if (stone != null) stones.Add(stone);
        }

        foreach (GameObject stone in stones)
        {
            Renderer rend = stone.GetComponent<Renderer>();
            if (rend != null) rend.material.color = highlightColor;
        }

        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseStones(stones));
    }

    IEnumerator PulseStones(List<GameObject> stones)
    {
        Vector3[] baseScales = new Vector3[stones.Count];
        for (int i = 0; i < stones.Count; i++)
        {
            baseScales[i] = stones[i] != null ? stones[i].transform.localScale : Vector3.one;
        }

        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float s = 1f + (Mathf.Sin(t) * 0.5f + 0.5f) * (pulseScale - 1f);

            for (int i = 0; i < stones.Count; i++)
            {
                if (stones[i] != null) stones[i].transform.localScale = baseScales[i] * s;
            }
            yield return null;
        }
    }

    public void ClearHighlight()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }
}
