using UnityEngine;

// 15x15x15 보드 데이터 관리 (순수 로직 클래스, MonoBehaviour 아님)
// 0=빈칸, 1=플레이어1, 2=플레이어2
public class GomokuGrid
{
    public const int Size = 15;

    private readonly int[,,] board = new int[Size, Size, Size];
    private readonly int[,] columnHeight = new int[Size, Size];

    public bool IsInBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

    public bool CanPlace(int x, int y) => IsInBounds(x, y) && columnHeight[x, y] < Size;

    // (x,y) 기둥의 가장 낮은 빈 층에 player를 배치하고, 실제로 놓인 좌표를 반환
    public bool TryPlace(int x, int y, int player, out Vector3Int placed)
    {
        placed = default;
        if (!CanPlace(x, y)) return false;

        int z = columnHeight[x, y];
        board[x, y, z] = player;
        columnHeight[x, y] = z + 1;
        placed = new Vector3Int(x, y, z);
        return true;
    }

    public int GetStone(int x, int y, int z)
    {
        if (!IsInBounds(x, y) || z < 0 || z >= Size) return 0;
        return board[x, y, z];
    }

    public int GetStone(Vector3Int pos) => GetStone(pos.x, pos.y, pos.z);

    public int GetColumnHeight(int x, int y) => IsInBounds(x, y) ? columnHeight[x, y] : Size;

    public bool IsFull()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                if (columnHeight[x, y] < Size) return false;
        return true;
    }

    public void Reset()
    {
        System.Array.Clear(board, 0, board.Length);
        System.Array.Clear(columnHeight, 0, columnHeight.Length);
    }
}
