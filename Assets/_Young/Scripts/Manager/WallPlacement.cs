using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class WallType
{
    public string name; // 벽 이름 (예: "Wall1")
    public GameObject wallPrefab; // 벽 프리팹
    public GameObject hologramPrefab; // 홀로그램 프리팹
    public int needWood;  // 필요한 나무
    public int needStone; // 필요한 돌
    public int needGold;  // 필요한 골드
}

public class WallPlacement : MonoBehaviour
{
    public Tilemap tilemap; // 기준 타일맵 (예: Ground)
    public List<WallType> wallTypes = new List<WallType>(); // Inspector에서 설정
    public GameObject epanel; // 안내 패널

    public bool isWall = true;

    private GameObject currentHologramObject;
    private SpriteRenderer currentHologramRenderer;
    private WallType currentWallType;

    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.3f;

    // 벽 설치 함수 예시 (더블클릭 등에서 호출)
    private void PlaceWall(Vector3Int cellPosition)
    {
        if (currentWallType == null)
        {
            Debug.LogWarning("설치할 벽이 선택되지 않았습니다.");
            return;
        }

        // 자원 체크
        if (GameManager.instance.woodCount >= currentWallType.needWood &&
            GameManager.instance.stoneCount >= currentWallType.needStone &&
            GameManager.instance.goldCount >= currentWallType.needGold)
        {
            // 자원 차감
            GameManager.instance.woodCount -= currentWallType.needWood;
            GameManager.instance.stoneCount -= currentWallType.needStone;
            GameManager.instance.goldCount -= currentWallType.needGold;
            // UI 갱신
            GameManager.instance.UpdateResourceUI();

            // 벽 설치
            Vector3 worldPos = tilemap.CellToWorld(cellPosition) + tilemap.cellSize / 2;
            Instantiate(currentWallType.wallPrefab, worldPos, Quaternion.identity);
        }
        else
        {
            Debug.Log("재화가 부족합니다!");
            // UI 경고 등 추가 가능
        }
    }
    private bool isActive = false;
    private bool isHammerSelected = false;
    [SerializeField] private float Yposition = -3.0f;
    private bool firstTile = false;

    void Start()
    {
        epanel.SetActive(false);
    }

    public void OnHammerSelected()
    {
        Debug.Log("망치 선택됨");
        if (epanel != null)
        {
            epanel.SetActive(true);
        }
        else
        {
            Debug.LogError("epanel이 할당되지 않았습니다!");
        }

        isHammerSelected = true;
    }

    public void OnHammerDeselected()
    {
       
        if (epanel != null)
        {
            epanel.SetActive(false);
        }
        else
        {
            Debug.LogError("epanel이 할당되지 않았습니다!");
        }

        isHammerSelected = false;

        // 현재 홀로그램도 비활성화
        if (currentHologramObject != null)
        {
            currentHologramObject.SetActive(false);
        }
        isActive = false;
        firstTile = false;
    }

    public void SelectWallType(string wallName)
    {
        firstTile = true;
       

        // 이전 홀로그램 제거
        if (currentHologramObject != null)
        {
            Destroy(currentHologramObject);
        }

        currentWallType = wallTypes.Find(w => w.name == wallName);

        if (currentWallType == null)
        {
            Debug.LogWarning($"'{wallName}' 이름의 벽이 없습니다.");
            return;
        }

        currentHologramObject = Instantiate(currentWallType.hologramPrefab);
        currentHologramRenderer = currentHologramObject.GetComponent<SpriteRenderer>();

        if (currentHologramRenderer == null)
        {
            Debug.LogWarning("홀로그램 프리팹에 SpriteRenderer가 없습니다.");
        }

        currentHologramObject.SetActive(true);
        isActive = true;
    }
    public void OnEnterButton()
    {
        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        if (player != null)
            player.isAttack = false;
        isWall = false;
    }
    public void OnExitButton()
    {
        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        if (player != null)
            player.isAttack = true;
        isWall = true;
    }


    void Update()
    {
        // 망치 선택 해제 시 상태 초기화
        if (!isHammerSelected)
        {
            isActive = false;
            firstTile = false;
            if (currentHologramObject != null)
            {
                currentHologramObject.SetActive(false);
            }
        }

        if (!isActive || currentHologramObject == null || currentHologramRenderer == null)
            return;

        // 플레이어의 Y값을 고정값으로 사용
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        float fixedY = Yposition;
        if (playerObj != null)
        {
            fixedY = playerObj.transform.position.y;
        }

        // 마우스 위치를 타일맵 셀 위치로 변환 (X만 사용)
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = tilemap.WorldToCell(mousePosition);

        // 홀로그램 위치: X는 마우스, Y는 플레이어 Y로 고정
        Vector3 holoWorldPos = tilemap.CellToWorld(cellPosition);
        currentHologramObject.transform.position = new Vector3(holoWorldPos.x, fixedY - 0.5f, holoWorldPos.z);

        SetHologramTransparency(0.5f);

        // 좌클릭 시 벽 생성
        if (Input.GetMouseButtonDown(0) && isWall)
        {
            Vector3Int wallCellPos = tilemap.WorldToCell(new Vector3(currentHologramObject.transform.position.x, fixedY - 0.5f, 0));
            PlaceWall(wallCellPos);

            // 벽 생성 후 모든 플레이어에 대해 IgnoreCollision 갱신
            Player[] players = FindObjectsOfType<Player>();
            foreach (Player player in players)
            {
                player.IgnoreCollisionsWithEnemiesAndNpcs();
            }
            Npc[] npcs = FindObjectsOfType<Npc>();
            foreach (Npc npc in npcs)
            {
                npc.IgnoreCollisionsWithEnemies();
            }
        }
    }

    private void SetHologramTransparency(float alpha)
    {
        if (currentHologramRenderer != null)
        {
            Color color = currentHologramRenderer.color;
            color.a = alpha;
            currentHologramRenderer.color = color;
        }
    }
}
