using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGScroll : MonoBehaviour
{
    public string propertyName = "_PlayerX";
    public string propertyName1 = "_LerpVec";
    private int propID;
    private int propID1;
    public float viewFloat; 
    private Renderer rend; 
    private MaterialPropertyBlock mpb;
    private Vector2 LerpVec = new Vector2(0,0);
    private float LerpFloat = 0f;

    void Awake() 
    {
        rend = GetComponent<Renderer>();      
        mpb = new MaterialPropertyBlock();
        propID = Shader.PropertyToID(propertyName);
        propID1 = Shader.PropertyToID(propertyName1);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float value = GameManager.instance.player.transform.position.x;
        rend.GetPropertyBlock(mpb);
        mpb.SetFloat(propID, value);
        mpb.SetVector(propID1, LerpVec);
        rend.SetPropertyBlock(mpb);
        viewFloat = value;        
        
        
    }
    public IEnumerator StartTwilight()
    {
        bool Play = true;
        Vector2 TempVec = LerpVec;
        while(Play)
        {
           
            LerpVec = Vector2.Lerp(TempVec, new Vector2(1,0), LerpFloat);
            LerpFloat += 0.034f;
            yield return new WaitForSeconds(0.4f);
            if(LerpFloat >= 1)
            {
                Play = false;
                LerpVec = new Vector2 (1,0);
                LerpFloat = 0;
            }
        }
        yield return null;
    }
    public IEnumerator StartNight()
    {
        bool Play = true;
        //Vector2 TempVec = LerpVec;
        while(Play)
        {
           
            LerpVec = Vector2.Lerp(new Vector2(1,0), new Vector2(1,1), LerpFloat);
            LerpFloat += 0.034f;
            yield return new WaitForSeconds(0.4f);
            if(LerpFloat >= 1)
            {
                LerpFloat = 0;
                Play = false;
                LerpVec = new Vector2 (0,1);                
            }
        }        
        yield return null;
    }
    public IEnumerator StartMorning()
    {
        bool Play = true;
        //Vector2 TempVec = LerpVec;
        while(Play)
        {
            
            LerpVec = Vector2.Lerp(new Vector2(1,0), new Vector2(0,0), LerpFloat);
            LerpFloat += 0.067f;
            yield return new WaitForSeconds(0.4f);
            if(LerpFloat >= 1)
            {
                Play = false;
                LerpVec = new Vector2 (0,0);
                LerpFloat = 0;
            }
        }        
        yield return null;
    }
    public void Twillight()
    {
        
        StartCoroutine(StartTwilight());
    }
    public void Morning()
    {
        StartCoroutine(StartMorning());
    }
    public void Night()
    {
        StartCoroutine(StartNight());
    }
}
