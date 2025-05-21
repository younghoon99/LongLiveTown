using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject escMenuUI; // ESC 메뉴 UI 패널
    [SerializeField] private MonoBehaviour[] behavioursToDisable; // 일시 정지 시 비활성화할 스크립트들
    [SerializeField] private GameObject SoundMenuUI; // 소리설정 UI 패널 등 추가 UI
    [SerializeField] private GameObject SaveLoadMenuUI; // 저장/로드 UI 패널
    private bool isPaused = false; // 현재 일시 정지 상태
    // UI 스택 구조
    private List<GameObject> uiStack = new List<GameObject>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (uiStack.Count > 0)
            {
                GameObject topUI = uiStack[uiStack.Count - 1];
                if (topUI == SoundMenuUI)
                {
                    // 소리설정 UI 닫기 전에 복원 함수 호출!
                    this.GetComponent<SoundMenu>().RestoreVolumeIfNotSaved();
                    SoundMenuUI.SetActive(false);
                    uiStack.RemoveAt(uiStack.Count - 1);
                    SetPause(true);
                }
                if(topUI == SaveLoadMenuUI)//메뉴구현하면 추가하면됨 
                {
                    SaveLoadMenuUI.SetActive(false);
                    uiStack.RemoveAt(uiStack.Count - 1);
                    SetPause(true);
                }
                else if (topUI == escMenuUI)
                {
                    // 메뉴 UI 닫기
                    SetPause(false);
                }
            }
            else
            {
                // 아무 UI도 없으면 메뉴 오픈
                SetPause(true);
            }
        }
        if(Time.timeScale == 0)    // 효과음 끄기/켜기
            GameManager.instance.sfxAudioSource.mute = true;
        else 
            GameManager.instance.sfxAudioSource.mute = false;
        
    }
    // ESC 메뉴 관련 버튼
    private void SetPause(bool pause)
    {
        isPaused = pause;
        escMenuUI.SetActive(pause);

        if (pause)
        {
            if (!uiStack.Contains(escMenuUI))
                uiStack.Add(escMenuUI);
            Time.timeScale = 0;
        }
        else
        {
            uiStack.Remove(escMenuUI);
            Time.timeScale = 1;
        }

        foreach (var b in behavioursToDisable)
            b.enabled = !pause;
    }
    // 소리설정 버튼 클릭 시
    public void OpenSoundMenu()
    {
        // 메뉴 UI 닫기
        SetPause(false);
        // 소리설정 UI 켜기
        SoundMenuUI.SetActive(true);
        if (!uiStack.Contains(SoundMenuUI))
            uiStack.Add(SoundMenuUI);
        Time.timeScale = 0;
    }
    public void OpenSaveLoadMenu()
    {
        // 메뉴 UI 닫기
        SetPause(false);
        // 저장/로드 UI 켜기
        SaveLoadMenuUI.SetActive(true);
        if (!uiStack.Contains(SaveLoadMenuUI))
            uiStack.Add(SaveLoadMenuUI);
        Time.timeScale = 0;
    }
    public void ClickExit()
    {
        Debug.Log("게임 종료");
        Application.Quit(); // 게임 종료
    }
    public void SceneReload()
{
    SceneManager.LoadScene(1);
    Time.timeScale = 1;
}
    //게임으로 돌아가기
    public void ClickReturn()
    {
        SetPause(false);
    }
}
