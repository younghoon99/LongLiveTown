//GameManager 스크립트 파일

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   //UI 사용을 위해서 추가
using UnityEngine.SceneManagement;   //Scene 전환을 위해서 추가

public class GameManager_ : MonoBehaviour
{
    public int totalPoint;  //총 점수
    public int stagePoint;  //스테이지 점수
    public int stageIndex;  //스테이지 번호
    public int health;  //플레이어 체력
    public PlayerMove player;
    public GameObject[] Stages;

    public Image[] UIhealth;
    public Text UIPoint;
    public Text UIStage;
    public GameObject UIRestartBtn;

    void Update()
    {
        UIPoint.text = (totalPoint + stagePoint).ToString();

    }

    public void NextStage()
    {
        //다음 스테이지로 이동
        if (stageIndex < Stages.Length - 1)
        {
            Stages[stageIndex].SetActive(false);    //현재 스테이지 비활성화
            stageIndex++;
            Stages[stageIndex].SetActive(true); //다음 스테이지 활성화
            PlayerReposition();

            UIStage.text = "STAGE " + (stageIndex + 1);
        }
        else
        {  //게임 클리어시
            //멈추기
            Time.timeScale = 0;

            //재시작 버튼 UI
            UIRestartBtn.SetActive(true);
            Text btnText = UIRestartBtn.GetComponentInChildren<Text>();   //버튼 텍스트는 자식 오브젝트이므로 InChildren을 붙여야함
            btnText.text = "Clear!";
            UIRestartBtn.SetActive(true);
        }

        //점수 계산
        totalPoint += stagePoint;
        stagePoint = 0;
    }

    public void HealthDown()
    {
        if (health > 1)
        {
            health--;
            UIhealth[health].color = new Color(1, 0, 0, 0.4f);  //체력 UI 색상 변화
        }
        else
        {
            //체력 UI 끄기
            UIhealth[0].color = new Color(1, 0, 0, 0.4f);

            //플레이어 사망 이펙트
///            player.OnDie();

            //재시작 버튼 UI
            UIRestartBtn.SetActive(true);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        //낙사할 경우
        if (collision.gameObject.tag == "Player")
        {
            //플레이어 위치 되돌리기
            if (health > 1)
            {
                PlayerReposition();
            }

            //체력 감소
            HealthDown();
        }
    }

    void PlayerReposition()
    {   //플레이어 위치 되돌리기 함수
        player.transform.position = new Vector3(0, 0, -1);  //플레이어 위치 이동
///        player.VelocityZero();  //플레이어 낙하 속도 0으로 만들기
    }

    public void Restart()
    {
        Time.timeScale = 1; //재시작시 시간 복구
        SceneManager.LoadScene(0);
    }
}