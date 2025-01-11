using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.Tilemaps;

/// <summary>
/// Contient toutes les méthodes relatives au moteur de combat
/// </summary>

public class BattleManager : MonoBehaviour
{
    
    public static BattleManager Instance;

    //Game states
    public enum BattleState {OUT, START, TURN, END, WON, LOST}
    public enum TurnState {OUT, START, ACTION_CHOICE, APPLY_ACTIONS, END}
    public enum PlayerActionChoiceState {OUT, START, CHARACTER_SELECTION, SWITCH_CHARACTER, SPELL_SELECTION, TARGET_SELECTION, VALIDATED_ACTION, OTHER_STATE, EXIT}

    public enum Machine{BATTLESTATE, PLAYERTURNSTATE, PLAYERACTIONCHOICESTATE}
    public enum Trigger {VALIDATE, CANCEL, FORWARD, EMPTY}
    public enum TeamTurn{OUT, ALLY, ENEMY}

    public BattleState battleState;
    public TurnState turnState;
    public PlayerActionChoiceState playerActionChoiceState;
    public TeamTurn teamTurn;

    public Instruction emptyInstruction;
    public BattleTurn emptyBattleTurn;
    
    private int battleId;
    public int nTurn = 1;
    public BattleTurn currentTurn;

    public List<BattleTurn> archivedTurns = new List<BattleTurn>();

    private bool inAnimation = false;
    private GameObject battleArchive;
    private 

    void Awake(){
        Instance = this;

        battleArchive = new GameObject("Current Battle Archive");
    }

    void Update(){
        if(!inAnimation){
            ChangeState(Machine.PLAYERACTIONCHOICESTATE, Trigger.EMPTY);
            ChangeState(Machine.PLAYERTURNSTATE, Trigger.EMPTY);
            ChangeState(Machine.BATTLESTATE, Trigger.EMPTY);
        }
    }

    public void LaunchBattle(List<Tuple<Vector2, ScriptableUnit, int>> ally_composition, List<Tuple<Vector2, ScriptableUnit, int>> enemy_composition){
        
        GridManager.Instance.GenerateGrids();
        UnitManager.Instance.SpawnAllies(ally_composition);
        UnitManager.Instance.SpawnEnemies(enemy_composition);

        StartBattle();
    }

    public void LaunchBattle(List<BaseUnit> ally_composition, List<Tuple<Vector2, ScriptableUnit, int>> enemy_composition){

        GridManager.Instance.GenerateGrids();
        UnitManager.Instance.SpawnAllies(ally_composition);
        UnitManager.Instance.SpawnEnemies(enemy_composition);

        StartBattle();
    }

    public void ChangeState(Machine machine, Trigger trigger){

        switch (machine)
        {
            case Machine.PLAYERACTIONCHOICESTATE:
                ChangePlayerActionChoiceState(trigger);
                break;
            case Machine.PLAYERTURNSTATE:
                ChangeTurnState(trigger);
                break;
            case Machine.BATTLESTATE:
                ChangeBattleState(trigger);
                break;
            default:
                break;
        }

        
    }

    private void ChangePlayerActionChoiceState(Trigger trigger){
        switch(playerActionChoiceState){

            case PlayerActionChoiceState.OUT:
                switch (trigger)
                {
                    case Trigger.FORWARD:
                        // Do stuff if needed
                        playerActionChoiceState = PlayerActionChoiceState.CHARACTER_SELECTION;
                        break;
                    default:
                        break;
                }
                break;

            case PlayerActionChoiceState.CHARACTER_SELECTION:
                switch (trigger){
                    case Trigger.VALIDATE:
                        playerActionChoiceState = PlayerActionChoiceState.SPELL_SELECTION;
                        break;
                    case Trigger.CANCEL:
                        CancelLastInstruction();
                        break;
                    default:
                        break;
                }
                break;

            case PlayerActionChoiceState.SPELL_SELECTION:
                switch (trigger){
                    case Trigger.VALIDATE:
                        playerActionChoiceState = PlayerActionChoiceState.TARGET_SELECTION;
                        break;
                    case Trigger.CANCEL:
                        playerActionChoiceState = PlayerActionChoiceState.CHARACTER_SELECTION;
                        break;
                    default:
                        break;
                }
                break;

            case PlayerActionChoiceState.TARGET_SELECTION:
                switch (trigger)
                {
                    case Trigger.VALIDATE:
                        playerActionChoiceState = PlayerActionChoiceState.VALIDATED_ACTION;
                        break;
                    case Trigger.CANCEL:
                        playerActionChoiceState = PlayerActionChoiceState.SPELL_SELECTION;
                        break;

                    default:
                        break;
                }
                break;

            case PlayerActionChoiceState.VALIDATED_ACTION:
                switch (trigger)
                {
                    case Trigger.EMPTY:
                        if(UnitManager.Instance.DidEveryCharacterGaveInstruction()){
                            playerActionChoiceState = PlayerActionChoiceState.EXIT;
                        }
                        else{
                            playerActionChoiceState = PlayerActionChoiceState.CHARACTER_SELECTION;
                        }
                        break;
                    default:
                        break;
                }
            break;
            
            case PlayerActionChoiceState.EXIT:
                switch (trigger)
                {
                    case Trigger.EMPTY:
                        // Do stuff
                        playerActionChoiceState = PlayerActionChoiceState.OUT;
                        ChangeState(Machine.PLAYERTURNSTATE, Trigger.FORWARD);
                        break;
                    default:
                        break;
                }

            break;
        }
    }

    private void ChangeTurnState(Trigger trigger){
        switch (turnState)
        {
            case TurnState.OUT:
                // Nothing happens unless it receives an FORWARD signal
                switch (trigger)
                {
                    case Trigger.FORWARD:
                        // Do stuff if needed
                        turnState = TurnState.START;
                        break;
                    default:
                        break;
                }
                break;

            case TurnState.START:
                // Do stuff
                // Start turn effects
                turnState = TurnState.ACTION_CHOICE;
                NextTurn();
                if(teamTurn == TeamTurn.ALLY){
                    ChangeState(Machine.PLAYERACTIONCHOICESTATE, Trigger.FORWARD);
                }
                break;

            case TurnState.ACTION_CHOICE:
                switch (trigger)
                {
                    case Trigger.FORWARD:
                        turnState = TurnState.APPLY_ACTIONS;
                        break;
                    
                    default:
                        if(teamTurn == TeamTurn.ENEMY){
                            currentTurn.SetInstructions(AIManager.Instance.GetAIOrders(ConvertTeamTurn(teamTurn)));
                            turnState = TurnState.APPLY_ACTIONS;
                        }
                        break;
                }
                break;

            case TurnState.APPLY_ACTIONS:
                ApplyInstructions();

                turnState = TurnState.END;
                break;

            case TurnState.END:
                // Do stuff
                // End turn effects
                turnState = TurnState.OUT;
                ChangeState(Machine.BATTLESTATE, Trigger.FORWARD);
                break;
            default:
                break;
        }
    }
    private void ChangeBattleState(Trigger trigger){
        switch (battleState){
            case BattleState.OUT:
                break;
            case BattleState.START:
                break;
            case BattleState.TURN:
                switch (trigger)
                {
                    case Trigger.FORWARD:
                        // Check if game is over
                        if(isGameOver() != BattleState.OUT){
                            battleState = BattleState.END;
                        }
                        else{
                            EndTurnEffects();
                            AnimateElements();
                            ArchiveTurn();
                            UnitManager.Instance.MakeUnitsActive();
                            SwitchCurrentTeam();
                            ChangeTurnState(Trigger.FORWARD);
                        }
                        break;
                    default:
                        break;
                }
                break;
            case BattleState.END:
                switch (trigger){
                    case Trigger.FORWARD:
                        battleState = isGameOver();
                        break;
                    default:
                        // Trigger qui intervient quand les trucs d'après batailles sont conclus
                        ChangeBattleState(Trigger.FORWARD);
                        break;
                }
                break;
            case BattleState.WON:
                Debug.Log("Cool il n'y a plus d'ennemi");
                break;
            case BattleState.LOST:
                Debug.Log("Cool il n'y a plus d'allié'");
                break;
            default:
                break;
        }
    }

    private BattleState isGameOver()
    {
        if(UnitManager.Instance.GetUnits(Team.Ally).Count == 0){
            return BattleState.LOST;
        }
        if(UnitManager.Instance.GetUnits(Team.Enemy).Count == 0){
            return BattleState.WON;
        }
        return BattleState.OUT;
    }

    private void SwitchCurrentTeam()
    {
        if(teamTurn == TeamTurn.ALLY){
            teamTurn = TeamTurn.ENEMY;
        }
        else{
            teamTurn = TeamTurn.ALLY;
        }
    }

    public void DebugSetState(){
        teamTurn = TeamTurn.ALLY;

        battleState = BattleState.TURN;
        turnState = TurnState.START;
        playerActionChoiceState = PlayerActionChoiceState.OUT;
    }

    public string GetCurrentStatesSummary(){
        string currentStates = "";
        currentStates = currentStates + "BattleState: " + battleState + "\n";
        currentStates = currentStates + "TurnState: " + turnState + "\n";
        currentStates = currentStates + "PlayerActionChoiceState: " + playerActionChoiceState + "\n";
        return currentStates;

    }
    public BattleState GetBattleState(){
        return battleState;
    }

    public TurnState GetTurnState(){
        return turnState;
    }

    public PlayerActionChoiceState GetPlayerActionChoiceState(){
        return playerActionChoiceState;
    }
    private void StartBattle(){
        teamTurn = TeamTurn.ALLY;
        battleState = BattleState.START;
        turnState = TurnState.OUT;
        playerActionChoiceState = PlayerActionChoiceState.OUT;
    }

    public void EndTurnEffects(){
        UnitManager.Instance.ApplyEndTurnEffects(ConvertTeamTurn(teamTurn));
    }

    private void ArchiveTurn(){
        currentTurn.ArchiveTurn();
        archivedTurns.Add(currentTurn);
    }

    public Team ConvertTeamTurn(TeamTurn teamTurn){
        switch (teamTurn)
        {
            case TeamTurn.ALLY:
                return Team.Ally;
                
            case TeamTurn.ENEMY:
                return Team.Enemy;

            default:
                return Team.Both;
        }
    }

    private void NextTurn(){
        nTurn ++;
        currentTurn = Instantiate(emptyBattleTurn);
        currentTurn.transform.SetParent(battleArchive.transform);
        currentTurn.name = "Turn " + nTurn;
        currentTurn.Setup(nTurn);
    }

    public Instruction CreateInstruction(BaseUnit source_unit, BaseSpell spell_to_cast, Tile target_tile, bool hyper = false){
        Instruction new_instruction = Instantiate(emptyInstruction);
        new_instruction.transform.SetParent(currentTurn.transform);
        new_instruction.Setup(source_unit, spell_to_cast, target_tile, hyper : hyper);
        new_instruction.name = new_instruction.GetSummary();
        return new_instruction;
    }

    public void AssignInstruction(Instruction instruction){
        AddInstruction(instruction);
        instruction.GetSourceUnit().GiveInstruction(true);
    }

    public void CancelLastInstruction(){
        currentTurn.RemoveInstruction(currentTurn.GetInstructions().Count - 1);
    }

    private void AddInstruction(Instruction instruction){
        currentTurn.AddInstruction(instruction);
    }

    private void ApplyInstructions(){
        foreach (Instruction instruction in currentTurn.GetInstructions())
        {
            ApplyInstruction(instruction);    
        }
    }

    private void ApplyInstruction(Instruction instruction){
        if(instruction.IsOverloaded()){
            instruction.GetSpell().HyperCast(instruction.GetTargetTile());
        }
        else{
            instruction.GetSpell().Cast(instruction.GetTargetTile());
        }

    }

    public int CountInstructions(){
        return currentTurn.GetInstructions().Count;
    }

    public bool AreInstructionsFull(){
        int registeredInstructionCount = 0;
        foreach (BaseUnit unit in UnitManager.Instance.GetUnits(Team.Ally))
        {
            foreach (Instruction instruction in currentTurn.GetInstructions()){
                if(instruction.GetSourceUnit() == unit){
                    registeredInstructionCount ++;
                }
            }
        }
        return registeredInstructionCount == UnitManager.Instance.CountUnits(Team.Ally);
    }

    public void AddEvent(BattleEvent _event){
        currentTurn.AddEvent(_event);
    }

    public void SetInAnimation(bool value){
        inAnimation = value;
    }

    public bool IsInAnimation(){
        return inAnimation;
    }

    public GameObject GetArchive(){
        return battleArchive;
    }

    public void AnimateElements(){
        //SetInAnimation(true);
        //var task = AnimationManager.Instance.Animate(currentTurn.GetBattleEvents());
    }
}
