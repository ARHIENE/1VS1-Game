using System.Collections.Generic;
using UnityEngine;

// 13방향 승리 판정 전용 static 유틸 (PKGoalZones/PKTrajectoryUtil과 동일한 순수 로직 컨벤션)
public static class GomokuWinChecker
{
    public const int WinCount = 5;

    public static readonly Vector3Int[] Directions =
    {
        // 축 방향 3개
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        // 평면 대각선 6개
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 0, -1),
        new Vector3Int(0, 1, 1),
        new Vector3Int(0, 1, -1),
        // 입체 대각선 4개
        new Vector3Int(1, 1, 1),
        new Vector3Int(1, 1, -1),
        new Vector3Int(1, -1, 1),
        new Vector3Int(1, -1, -1),
    };

    // lastPos: 방금 놓인 좌표, player: 그 돌의 플레이어 번호
    // 반환: 승리 여부. winLine에는 승리에 관여한 좌표(자기 자신 포함) 목록
    public static bool CheckWin(GomokuGrid grid, Vector3Int lastPos, int player, out List<Vector3Int> winLine)
    {
        foreach (var dir in Directions)
        {
            var line = new List<Vector3Int> { lastPos };
            AppendRun(grid, lastPos, dir, player, line);
            AppendRun(grid, lastPos, -dir, player, line);

            if (line.Count >= WinCount)
            {
                winLine = line;
                return true;
            }
        }

        winLine = null;
        return false;
    }

    // origin에서 step 방향으로 같은 player의 돌이 이어지는 동안 좌표를 line에 추가
    private static void AppendRun(GomokuGrid grid, Vector3Int origin, Vector3Int step, int player, List<Vector3Int> line)
    {
        Vector3Int cur = origin + step;
        while (grid.GetStone(cur) == player)
        {
            line.Add(cur);
            cur += step;
        }
    }
}
