using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    public static GameManager Instance;
    
    public enum BattleState {OUTSIDE, START, PLAYERTURN, ENEMYTURN, END, WON, LOST}
    public enum TurnState {OUTSIDE, START, MIDDLE, END}

    public int nTurn = 0;
    public BattleState battleState;
    public TurnState turnState;

    void Awake(){
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        GridManager.Instance.GenerateGrid();
        UnitManager.Instance.SpawnEnemy(1, 1);
        UnitManager.Instance.SpawnEnemy(1, 3);
        StartBattle();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void StartBattle(){
        battleState = BattleState.START;

        //Start the first turn
        NextTurn();

    }

    private void StartTurn(){
        turnState = TurnState.START;
    }

    [ContextMenu("Tour suivant")]
    public void NextTurn(){
        nTurn ++;
        StartTurn();
    }
}
