using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    public GameObject introUI;
    public void Start()
    {
        introUI.SetActive(false); // 시작 시 비활성화
        StartCoroutine(ShowIntroUIAfterDelay()); // 3초 후 활성화 코루틴 시작
    }
    public void OnclickSeceneChange()
    {
        SceneManager.LoadScene(1);
    }
    private IEnumerator ShowIntroUIAfterDelay()
    {
        yield return new WaitForSeconds(3f); // 3초 대기
        introUI.SetActive(true); // 3초 후 활성화
    }

}
