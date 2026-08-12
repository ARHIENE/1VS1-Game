using UnityEngine;

// 골대를 2행 3열(1~6번) 구역으로 나누는 공용 계산 유틸
// 1=좌상 2=중상 3=우상 4=좌하 5=중하 6=우하
public static class PKGoalZones
{
    public const int Count = 6;

    public static bool IsInsideGoal(Vector3 pos, Vector3 goalCenter, float goalHalfWidth, float goalHeight)
    {
        float halfHeight = goalHeight * 0.5f;
        return pos.x >= goalCenter.x - goalHalfWidth && pos.x <= goalCenter.x + goalHalfWidth &&
               pos.y >= goalCenter.y - halfHeight && pos.y <= goalCenter.y + halfHeight;
    }

    public static int GetZoneFromPosition(Vector3 pos, Vector3 goalCenter, float goalHalfWidth, float goalHeight)
    {
        float halfHeight = goalHeight * 0.5f;
        float relX = Mathf.Clamp(pos.x - goalCenter.x, -goalHalfWidth, goalHalfWidth);
        float relY = Mathf.Clamp(pos.y - goalCenter.y, -halfHeight, halfHeight);

        float colWidth = (goalHalfWidth * 2f) / 3f;
        int col = Mathf.Clamp(Mathf.FloorToInt((relX + goalHalfWidth) / colWidth), 0, 2);
        int row = relY >= 0f ? 0 : 1; // 0 = 위쪽, 1 = 아래쪽

        return row * 3 + col + 1;
    }

    public static Vector3 GetZoneCenter(int zone, Vector3 goalCenter, float goalHalfWidth, float goalHeight)
    {
        zone = Mathf.Clamp(zone, 1, 6);
        int index = zone - 1;
        int col = index % 3;
        int row = index / 3; // 0 = 위쪽, 1 = 아래쪽

        float colWidth = (goalHalfWidth * 2f) / 3f;
        float x = goalCenter.x - goalHalfWidth + colWidth * (col + 0.5f);
        float quarterHeight = goalHeight * 0.25f;
        float y = goalCenter.y + (row == 0 ? quarterHeight : -quarterHeight);

        return new Vector3(x, y, goalCenter.z);
    }

    public static int RandomZone() => Random.Range(1, Count + 1);
}
