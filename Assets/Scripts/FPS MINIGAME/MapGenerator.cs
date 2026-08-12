using UnityEngine;
using Photon.Pun;

namespace FPSMinigame
{
public class MapGenerator : MonoBehaviourPunCallbacks
{
    [Header("엄폐물 설정")]
    public GameObject coverPrefab;
    public int coverCount = 5;

    [Header("맵 범위 설정")]
    public float mapWidth = 18f;
    public float mapDepth = 18f;
    public float centerExcludeRadius = 3f;

    [Header("엄폐물 크기")]
    public Vector3 minSize = new Vector3(2, 1.5f, 1f);
    public Vector3 maxSize = new Vector3(5, 2.5f, 2f);

    // 고정 스폰 보호 벽 위치 (겹침 체크용)
    private Vector3 spawnWall1Pos = new Vector3(0, 1f, -10f);
    private Vector3 spawnWall2Pos = new Vector3(0, 1f, 10f);
    private Vector3 spawnWallSize = new Vector3(12, 2f, 1f);

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int seed = Random.Range(0, 99999);
            photonView.RPC("GenerateMap", RpcTarget.All, seed);
        }
    }

    [PunRPC]
    void GenerateMap(int seed)
    {
        // 고정 스폰 보호 벽 생성
        SpawnCover(spawnWall1Pos, spawnWallSize, 0f);
        SpawnCover(spawnWall2Pos, spawnWallSize, 0f);

        Random.InitState(seed);

        // 랜덤 엄폐물 생성
        int placed = 0;
        int maxAttempts = 100;
        int attempts = 0;

        while (placed < coverCount && attempts < maxAttempts)
        {
            Vector3 pos = GetRandomPosition();
            Vector3 size = new Vector3(
                Random.Range(minSize.x, maxSize.x),
                Random.Range(minSize.y, maxSize.y),
                Random.Range(minSize.z, maxSize.z)
            );
            float rotY = Random.Range(0, 4) * 90f;

            // 고정 벽이랑 겹치는지 체크
            if (!IsOverlappingSpawnWall(pos, size))
            {
                SpawnCover(pos, size, rotY);
                Vector3 mirrorPos = new Vector3(-pos.x, pos.y, -pos.z);
                SpawnCover(mirrorPos, size, rotY);
                placed++;
            }

            attempts++;
        }

        // 중앙 엄폐물
        Vector3 centerSize = new Vector3(
            Random.Range(2f, 4f),
            Random.Range(1.5f, 2.5f),
            Random.Range(2f, 4f)
        );
        SpawnCover(new Vector3(0, centerSize.y / 2f, 0), centerSize, 0f);
    }

    bool IsOverlappingSpawnWall(Vector3 pos, Vector3 size)
    {
        // 고정 벽 1, 2와 겹치는지 체크
        float minDist = 4f; // 최소 거리
        if (Vector3.Distance(pos, spawnWall1Pos) < minDist) return true;
        if (Vector3.Distance(pos, spawnWall2Pos) < minDist) return true;
        return false;
    }

    Vector3 GetRandomPosition()
    {
        float x = Random.Range(2f, mapWidth);
        float z = Random.Range(-mapDepth, mapDepth);
        return new Vector3(x, 1f, z);
    }

    void SpawnCover(Vector3 position, Vector3 size, float rotY)
    {
        GameObject cover = Instantiate(coverPrefab, position, Quaternion.Euler(0, rotY, 0));
        cover.transform.localScale = size;
        cover.name = "Cover";
    }
}
}