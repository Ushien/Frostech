using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake(){
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        GridManager.Instance.GenerateGrid();
        UnitManager.Instance.SpawnEnemy();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
