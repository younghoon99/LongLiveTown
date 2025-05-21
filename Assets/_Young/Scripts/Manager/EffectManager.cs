using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [Header("오브젝트 풀 설정")]
    public GameObject effectPrefab; // 이펙트 프리팹
    public GameObject arrowPrefab; // 화살 프리팹
    public int effectPoolSize = 5; // 이펙트 풀 크기
    public int arrowPoolSize = 10; // 화살 풀 크기

    private Queue<GameObject> effectPool = new Queue<GameObject>(); // 이펙트 오브젝트 풀
    private Queue<GameObject> arrowPool = new Queue<GameObject>(); // 화살 오브젝트 풀

    void Start()
    {
        // 이펙트 풀 초기화
        for (int i = 0; i < effectPoolSize; i++)
        {
            GameObject effect = Instantiate(effectPrefab, transform); // EffectManager 하위에 생성
            effect.SetActive(false);
            effectPool.Enqueue(effect);
        }

        // 화살 풀 초기화
        for (int i = 0; i < arrowPoolSize; i++)
        {
            GameObject arrow = Instantiate(arrowPrefab, transform); // EffectManager 하위에 생성
            arrow.SetActive(false);
            arrowPool.Enqueue(arrow);
        }
    }

    // 이펙트 가져오기
    public GameObject GetPooledEffect()
    {
        if (effectPool.Count > 0)
        {
            GameObject effect = effectPool.Dequeue();
            effect.transform.SetParent(transform); // 항상 EffectManager 하위에 유지
            return effect;
        }
        return null; // 풀에 사용 가능한 이펙트가 없으면 null 반환
    }

    // 화살 가져오기
    public GameObject GetPooledArrow()
    {
        if (arrowPool.Count > 0)
        {
            GameObject arrow = arrowPool.Dequeue();
            arrow.transform.SetParent(transform); // 항상 EffectManager 하위에 유지
            return arrow;
        }
        return null; // 풀에 사용 가능한 화살이 없으면 null 반환
    }

    // 이펙트 반환
    public void ReturnEffectToPool(GameObject effect)
    {
        effect.SetActive(false);
        effect.transform.SetParent(transform); // 반환 시 EffectManager 하위로 이동
        effectPool.Enqueue(effect);
    }

    // 화살 반환
    public void ReturnArrowToPool(GameObject arrow)
    {
        arrow.SetActive(false);
        arrow.transform.SetParent(transform); // 반환 시 EffectManager 하위로 이동
        arrowPool.Enqueue(arrow);
    }
}
