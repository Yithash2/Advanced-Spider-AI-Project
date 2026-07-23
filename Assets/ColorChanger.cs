using System;
using UnityEngine;
using System.Collections;

public class ColorChanger : MonoBehaviour
{
    private void OnValidate()
    {
        for (int i = 0; i < bodyPart.Length; i++)
        { 
            bodyPart[i].name = bodyPart[i].sprite.gameObject.name;

            if (bodyPart[i].name.Contains("Body"))
            {
                bodyIndex = i;
            }
        }
        
        originalColor = bodyPart[bodyIndex].sprite.color;
        legsWhoDeclaredBlush = new bool[8] {false, false,false,false,false,false,false,false};

        
    }

    [System.Serializable]
    private struct Part
    {
        [HideInInspector]
        public string name;
        public SpriteRenderer sprite;
        public Color baseColor;
    }
    
    [SerializeField] private Part[] bodyPart;
    
    public void ChangeColor(Color color)
    {
        foreach (var p in bodyPart)
        {
            p.sprite.color = color * p.baseColor;
        }
    }

    [ContextMenu("Reset")]
    public void ResetToOriginal()
    {
        foreach (var p in bodyPart)
        {
            p.sprite.color = p.baseColor;
        }
    }

    private void Start()
    {
        originalColor = bodyPart[bodyIndex].sprite.color;
    }

    #region  Blushing

    private bool blushing;
    [SerializeField] private Color blushColor;
    private Color originalColor;
    private int bodyIndex;
    private bool[] legsWhoDeclaredBlush;
    public void Blush(bool b, int legId)
    {
        if (b)
        {
            legsWhoDeclaredBlush[legId] = true;
            if (blushing) return;
            blushing = true;
            StartCoroutine(BlushAnimation());

        }
        else
        {
            if (!blushing) return;
            if (!legsWhoDeclaredBlush[legId]) return;
            legsWhoDeclaredBlush[legId] = false;

            bool r = true;
            foreach (bool b1 in legsWhoDeclaredBlush)
            {
                r = r && !b1;
            }

            if (r)
            {
                blushing = false;
                StartCoroutine(UnBlushAnimation());
            }
            
            
        }
    }
    
    private IEnumerator BlushAnimation()
    {
        Color currentColor = bodyPart[bodyIndex].sprite.color;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            bodyPart[bodyIndex].sprite.color = Color.Lerp(currentColor, blushColor, t / 0.2f);
            
            if(!blushing)
                yield break;
            
            yield return 0;
        }
    }
    
    private IEnumerator UnBlushAnimation()
    {
        Color currentColor = bodyPart[bodyIndex].sprite.color;
        for (float t = 0; t < 0.05f; t += Time.deltaTime)
        {
            bodyPart[bodyIndex].sprite.color = Color.Lerp(currentColor,originalColor, t / 0.05f);
            
            if(blushing)
                yield break;
            
            yield return 0;
        }
    }

    #endregion
    
    
}
