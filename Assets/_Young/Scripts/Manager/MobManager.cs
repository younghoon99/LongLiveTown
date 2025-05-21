using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MobManager : MonoBehaviour
{
    public static MobManager instance;

    [Header("사운드 설정")]
    public AudioClip waveStartSound;
    public AudioClip clearSound;

    [Header("프리팹 설정")]
    public GameObject[] mobPrefabs; // 몬스터 프리팹들
    public Transform[] spawnPoints; // 몬스터 스폰 위치
    

    [Header("게임 설정")]
    public float spawnInterval = 2f; // 몬스터 스폰 간격
    public int[] mobCountsPerStage = new int[10]; // 각 스테이지별 몬스터 수
    public float waveDelay = 30f; // 웨이브 간 대기 시간 및 스테이지 클리어 후 대기시간 통합
    public float firstWaveDelay = 15f; // 게임 시작 후 첫 웨이브 전 대기시간

    public int currentStage = 0; // 현재 스테이지 (0부터 시작)
    private int spawnedCount = 0; // 현재 스폰된 몬스터 수
    private int destroyedCount = 0; // 처치된 몬스터 수
    public TextMeshProUGUI stageText; 
    public TextMeshProUGUI waveTimerText; // 웨이브까지 남은 시간 표시용 텍스트

    public bool IsStageClearing { get; private set; } = false;

    // 웨이브 대기 타이머 및 상태
    private float waveTimer = 0f;
    private bool isWaitingForWave = false;

    public List<GameObject> activeMobs = new List<GameObject>();
    private List<GameObject> mobPool = new List<GameObject>();

    private AudioSource audioSource;
    public UIManager uiManager;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        // 오디오 소스 초기화
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // 오브젝트 풀 미리 생성 (최대 30개까지)
        for (int i = 0; i < 30; i++)
        {
            GameObject mob = Instantiate(GetRandomMobPrefab());
            // Rigidbody2D의 Body Type을 강제로 Dynamic으로 설정
            Rigidbody2D rb = mob.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
            mob.SetActive(false);
            mobPool.Add(mob);
            mob.transform.SetParent(transform);
        }

        StartCoroutine(StartStage());

       
    }
    private void Update() {
        stageText.text = $"Stage {currentStage + 1}";
        // 웨이브 대기 중이면 남은 시간 표시
        if (isWaitingForWave && waveTimerText != null)
        {
            waveTimerText.text = $"다음 웨이브까지: {Mathf.CeilToInt(waveTimer)}초";
        }
        else if (waveTimerText != null)
        {
            waveTimerText.text = "";
        }
    }

    // 스테이지 시작
    private IEnumerator StartStage()
    {
        if (currentStage >= mobCountsPerStage.Length) yield break;

        float waitTime = (currentStage == 0) ? firstWaveDelay : waveDelay;
        if (waitTime > 0f)
        {
            waveTimer = waitTime;
            isWaitingForWave = true;
            while (waveTimer > 0f)
            {
                yield return null;
                waveTimer -= Time.deltaTime;
            }
            isWaitingForWave = false;
            if (waveTimerText != null) waveTimerText.text = "";
        }

        // 스테이지 텍스트 & 사운드
        yield return StartCoroutine(PlayWaveStartSoundWithText($"Stage {currentStage + 1}"));

        // 몬스터 스폰 코루틴 시작
        StartCoroutine(SpawnMobs());
    }

    // 몬스터 스폰
    private IEnumerator SpawnMobs()
    {
        int totalToSpawn = mobCountsPerStage[currentStage];
        spawnedCount = 0;
        destroyedCount = 0;

        while (spawnedCount < totalToSpawn)
        {
            yield return new WaitForSeconds(spawnInterval);

            GameObject mob = GetPooledMob();
            if (mob == null) continue;

            // 랜덤 위치 배치
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            mob.transform.position = spawnPoint.position;

            // Rigidbody2D를 Dynamic으로 설정
            Rigidbody2D rb = mob.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }

            // 활성화
            mob.SetActive(true);

            // 꺼져있을 수 있는 컴포넌트 다시 켜주기
            Collider2D collider = mob.GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true;

            MonoBehaviour enemyScript = mob.GetComponent<Enemy>();
            if (enemyScript != null) enemyScript.enabled = true;

            activeMobs.Add(mob);
            spawnedCount++;
        }
    }

    // 몬스터가 죽었을 때 호출할 함수 (외부에서 호출)
    public void OnMobDestroyed(GameObject mob)
    {
        if (activeMobs.Contains(mob))
        {
            activeMobs.Remove(mob);
            mob.SetActive(false); // 오브젝트 풀에 다시 넣기
            destroyedCount++;

            // 아이템 드롭
            GameManager.instance.resourcePoolScipt.ActivateResource("Gold", 2, mob.transform.position);
            //SpawnItem(mob.transform.position);

            // 모든 몬스터가 처치되면 다음 스테이지
            if (destroyedCount >= mobCountsPerStage[currentStage])
            {
                StartCoroutine(HandleStageClear());
            }
        }
    }

    // 스테이지 클리어 처리
    public IEnumerator HandleStageClear()
    {
        IsStageClearing = true;

        // 마지막 스테이지(10스테이지) 클리어 시
        if (currentStage == mobCountsPerStage.Length - 1)
        {
            if (waveTimerText != null)
                waveTimerText.text = "축하합니다! 모든 적을 처치했습니다. 엔딩이 곧 시작됩니다.";
            yield return new WaitForSeconds(5f);
            if (waveTimerText != null)
                waveTimerText.text = "";
            IsStageClearing = false;
            currentStage++; // 엔딩 매니저에서 자동으로 엔딩 연출
            yield break;
        }

        // 1. 대기 시간(waveDelay) 카운트다운 즉시 시작
        float waitTime = waveDelay;
        waveTimer = waitTime;
        isWaitingForWave = true;
        if (waveTimerText != null)
            waveTimerText.text = $"다음 웨이브까지: {Mathf.CeilToInt(waveTimer)}초";

        // 2. 연출(사운드/텍스트 등)은 병렬로 실행
        StartCoroutine(PlayClearSoundWithText("Clear!"));

        // 3. 대기 루프 - 텍스트 실시간 갱신
        while (waveTimer > 0f)
        {
            yield return null;
            waveTimer -= Time.deltaTime;
            if (waveTimerText != null)
                waveTimerText.text = $"다음 웨이브까지: {Mathf.CeilToInt(waveTimer)}초";
        }
        isWaitingForWave = false;
        if (waveTimerText != null)
            waveTimerText.text = "";

        IsStageClearing = false;

        // 플레이어 체력 초기화
        var playerHealth = GameObject.FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        // NPC들의 체력 초기화
        var npcHealths = FindObjectsOfType<NpcHealth>();
        foreach (var npcHealth in npcHealths)
        {
            npcHealth.ResetHealth();
        }

        currentStage++;

        if (currentStage < mobCountsPerStage.Length)
        {
            StartCoroutine(StartStage());
        }
        else
        {
            Debug.Log("모든 스테이지 클리어!");
        }
    }

    // 랜덤 몬스터 프리팹 반환
    private GameObject GetRandomMobPrefab()
    {
        return mobPrefabs[Random.Range(0, mobPrefabs.Length)];
    }

    // 오브젝트 풀에서 비활성화된 몬스터 중 랜덤으로 하나 꺼내오기
    private GameObject GetPooledMob()
    {
        List<GameObject> inactiveMobs = new List<GameObject>();

        // 현재 풀에 있는 비활성화된 몬스터들만 리스트로 모으기
        foreach (var mob in mobPool)
        {
            if (!mob.activeInHierarchy)
            {
                inactiveMobs.Add(mob);
            }
        }

        // 비활성화된 몬스터가 있으면 랜덤으로 하나 선택해서 반환
        if (inactiveMobs.Count > 0)
        {
            GameObject mob = inactiveMobs[Random.Range(0, inactiveMobs.Count)];
            // 몹이 다시 활성화될 때 EnemyHealth의 체력을 초기화
            var health = mob.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ResetHealth();
            }
            return mob;
        }

        // 비활성화된 몬스터가 없으면 새로 생성
        GameObject newMob = Instantiate(GetRandomMobPrefab());
        newMob.SetActive(false);
        mobPool.Add(newMob);
        return newMob;
    }

    // 웨이브 시작 텍스트 및 사운드
    private IEnumerator PlayWaveStartSoundWithText(string waveText)
    {
        if (waveStartSound != null)
        {
            audioSource.clip = waveStartSound;
            audioSource.Play();

            // 사운드 재생 시간을 3초로 제한
            yield return new WaitForSeconds(2f);
            audioSource.Stop();
        }

        if (uiManager != null)
        {
            uiManager.ShowWaveText(waveText);
        }

        yield return new WaitForSeconds(3f);
    }

    // 클리어 텍스트 및 사운드
    private IEnumerator PlayClearSoundWithText(string clearText)
    {
        float totalWait = 0f;

        // 사운드 연출(3초)
        if (clearSound != null)
        {
            audioSource.clip = clearSound;
            audioSource.Play();
            yield return new WaitForSeconds(3f);
            audioSource.Stop();
            totalWait += 3f;
        }

        // 텍스트 연출(3초)
        if (uiManager != null)
        {
            uiManager.ShowClearText(clearText);
        }
        yield return new WaitForSeconds(3f);
        totalWait += 3f;

        // 연출 시간(6초)보다 waveDelay가 더 크면 추가 대기
        if (waveDelay > totalWait)
        {
            yield return new WaitForSeconds(waveDelay - totalWait);
        }
        // waveDelay가 연출 시간 이하라면, 추가 대기 없이 바로 진행
    }

    
}
