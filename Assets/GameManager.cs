using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public List<spider_script>[] Spiders { get;private set; }
    public int NumberOfSpiders { get; private set; }
    

    [field:SerializeField]
    public float EnemyMapRefreshTime {get; private set;}
    
    [field:SerializeField]public bool BadPC { get; private set; }

    public static GameManager Instance {get; private set;}

    void Awake(){
        if(Instance!=null){
            Debug.LogWarning("There was more then one GameManager,\n so we decided to delete the last one");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        var values = Enum.GetValues(typeof(Species));
        Spiders = new List<spider_script>[values.Length];
        
        for (int i = 0; i < values.Length; i++)
        {
            Spiders[i] = new List<spider_script>();
        }
    }

    public void RemoveSpider(spider_script spider){
        Spiders[(int)spider.specie].Remove(spider);
        NumberOfSpiders--;
    }

    public void AddSpider(spider_script spider)
    {
        Spiders[(int)spider.specie].Add(spider);
        NumberOfSpiders++;
    }
    
}
