using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// Gestion de l'interface de jeu
/// </summary>

public class InterfaceManager : MonoBehaviour
{
    public static InterfaceManager Instance;

    // UI elements
    public GameObject informationPanel;
    public TextMeshProUGUI unitNamePanel;
    public TextMeshProUGUI unitPowerPanel;
    public TextMeshProUGUI unitHealthPanel;
    public TextMeshProUGUI unitArmorPanel;
    public TextMeshProUGUI unitLevelPanel;
    public TextMeshProUGUI unitPassiveNamePanel;
    public TextMeshProUGUI unitPassiveDescriptionPanel;
    public TextMeshProUGUI spellCooldownPanel;
    public GameObject spellSelector;
    public GameObject shade;
    public Material grayscaleShader;

    public GameObject tileSelector;

    // La Tile contenant la source du spell, lorsqu'un spell est lancé (lorsqu'on revient en arrière pendant la sélection de cible)
    private Tile sourceTile;

    private enum SpellChoice{CHARACTER, LEFT, RIGHT, UP, DOWN}
    private SpellChoice spellChoice;

    // Le spell pour lequel on va sélectionner une cible
    private BaseSpell selectedSpell;

    // La Tile contenant la cible du spell, lorsqu'un spell est lancé
    public Tile targetTile;

    private Dictionary<BattleManager.PlayerActionChoiceState, bool> activated_states;

    void Awake(){
        activated_states = new Dictionary<BattleManager.PlayerActionChoiceState, bool>();
        foreach (BattleManager.PlayerActionChoiceState state in System.Enum.GetValues(typeof(BattleManager.PlayerActionChoiceState)))
        {
            activated_states[state] = false;
        }
    }
    void Update()
    {   
        switch (BattleManager.Instance.GetPlayerActionChoiceState())
        {
            case BattleManager.PlayerActionChoiceState.CHARACTER_SELECTION:
                SourceSelectionDisplay();
                break;
            case BattleManager.PlayerActionChoiceState.SPELL_SELECTION:
                SpellSelectionDisplay();
                break;
            case BattleManager.PlayerActionChoiceState.TARGET_SELECTION:
                TargetSelectionDisplay();
                break;
            default:
                break;
        }
        
    }   

    void SourceSelectionDisplay(){
        if(!activated_states[BattleManager.PlayerActionChoiceState.CHARACTER_SELECTION]){
            // Just changed from another state
            // Reset view
            ResetDisplay();

            // Activate the needed interface
            informationPanel.SetActive(true);
            tileSelector.SetActive(true);

            GridManager.Instance.GetMiddleTile(Team.Ally).Select();
            GridManager.Instance.SetSelectionMode(GridManager.Selection_mode.Single_selection);

            ActivateState(BattleManager.PlayerActionChoiceState.CHARACTER_SELECTION);

        }
        sourceTile = GridManager.Instance.GetMainSelection();
        tileSelector.transform.position = sourceTile.transform.position;

        GridManager.Instance.DisplayHighlights();

        if (Input.GetKeyDown(KeyCode.B)){
            if(sourceTile.GetUnit()!= null){
                if(sourceTile.GetUnit().GetTeam() == Team.Ally && !sourceTile.GetUnit().HasGivenInstruction()){
                    SourceSelectionTrigger(BattleManager.Trigger.VALIDATE);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.N)){
            SourceSelectionTrigger(BattleManager.Trigger.CANCEL);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow)){
            if(sourceTile.GetNextTile(Directions.UP) != null){
                sourceTile.GetNextTile(Directions.UP).Select();
                sourceTile.Unselect();
            }
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)){
            if(sourceTile.GetNextTile(Directions.DOWN) != null){
                sourceTile.GetNextTile(Directions.DOWN).Select();
                sourceTile.Unselect();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)){
            if(sourceTile.GetNextTile(Directions.LEFT) != null){
                sourceTile.GetNextTile(Directions.LEFT).Select();
                sourceTile.Unselect();
            }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)){
            if(sourceTile.GetNextTile(Directions.RIGHT) != null){
                sourceTile.GetNextTile(Directions.RIGHT).Select();
                sourceTile.Unselect();
            }
        }

        BaseUnit currentUnit = sourceTile.GetUnit();
        if(currentUnit != null){
            informationPanel.SetActive(true);
            DisplayUnit(currentUnit);
        }
        else{
            informationPanel.SetActive(false);
        }
    }
    void SourceSelectionTrigger(BattleManager.Trigger trigger){
        // On sort de la sélection de la source pour aller vers un autre état
        sourceTile.Unselect();
        BattleManager.Instance.ChangeState(BattleManager.Machine.PLAYERACTIONCHOICESTATE, trigger);
    }

    void SpellSelectionDisplay(){
        // TODO Ne pas afficher les 4 cases si le personnage n'a pas 4 spells
        BaseUnit sourceUnit = sourceTile.GetUnit();
        List<BaseSpell> currentSpells = sourceUnit.GetSpells();

        if(!activated_states[BattleManager.PlayerActionChoiceState.SPELL_SELECTION]){
            // Just changed from another state

            // Reset view
            ResetDisplay();

            // Activate the needed interface
            spellSelector.SetActive(true);
            shade.SetActive(true);
            informationPanel.SetActive(true);

            spellSelector.transform.position = sourceTile.transform.position;

            int currentSpellIndex = 0;

            foreach (var spell in currentSpells)
            {
                spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().sprite = spell.GetArtwork();
                spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().material = null;
                if(!spell.IsAvailable()){
                    Material material = Instantiate(grayscaleShader);
                    spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().material = material;
                    //Grey
                }
                currentSpellIndex += 1;
            }

            GridManager.Instance.SetSelectionMode(GridManager.Selection_mode.Single_selection);
            
            ActivateState(BattleManager.PlayerActionChoiceState.SPELL_SELECTION);
        }

        //Display highlight
        spellSelector.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(false);
        spellSelector.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(false);
        spellSelector.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(false);
        spellSelector.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(false);
        spellSelector.transform.GetChild(4).transform.GetChild(0).gameObject.SetActive(false);

        switch(spellChoice){
            case SpellChoice.CHARACTER:
                spellSelector.transform.GetChild(4).transform.GetChild(0).gameObject.SetActive(true);
                break;
            case SpellChoice.LEFT:
                spellSelector.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(true);
                break;
            case SpellChoice.RIGHT:
                spellSelector.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(true);
                break;
            case SpellChoice.UP:
                spellSelector.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(true);
                break;
            case SpellChoice.DOWN:
                spellSelector.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(true);
                break;
            default:
                break;
        }

        // Navigation au sein de la sélection
        switch(spellChoice){
            case SpellChoice.CHARACTER:
                selectedSpell = sourceUnit.GetAttack();
                if (Input.GetKeyDown(KeyCode.B)){
                    // Sélectionner attaque
                    SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.N)){
                    // Retour à la sélection de personnages
                    SpellSelectionTrigger(BattleManager.Trigger.CANCEL);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)){
                    // Aller en haut
                    spellChoice = SpellChoice.UP;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Aller en bas
                    spellChoice = SpellChoice.DOWN;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Aller à gauche
                    spellChoice = SpellChoice.LEFT;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)){
                    // Aller à droite
                    spellChoice = SpellChoice.RIGHT;
                    break;
                }
                break;
            case SpellChoice.LEFT:
                selectedSpell = currentSpells[0];
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell.IsAvailable()){
                    SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.N)){
                    // Retour au centre
                    spellChoice = SpellChoice.CHARACTER;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)){
                    // Aller en haut
                    spellChoice = SpellChoice.UP;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Aller en bas
                    spellChoice = SpellChoice.DOWN;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)){
                    // Aller à droite
                    spellChoice = SpellChoice.RIGHT;
                    break;
                }
                break;
            case SpellChoice.RIGHT:
                selectedSpell = currentSpells[1];
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell.IsAvailable()){
                    SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.N)){
                    // Retour au centre
                    spellChoice = SpellChoice.CHARACTER;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)){
                    // Aller en haut
                    spellChoice = SpellChoice.UP;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Aller en bas
                    spellChoice = SpellChoice.DOWN;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Aller à gauche
                    spellChoice = SpellChoice.LEFT;
                    break;
                }
                break;
            case SpellChoice.UP:
                selectedSpell = currentSpells[2];
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell.IsAvailable()){
                    SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.N)){
                    // Retour au centre
                    spellChoice = SpellChoice.CHARACTER;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)){
                    // Aller à droite
                    spellChoice = SpellChoice.RIGHT;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Aller en bas
                    spellChoice = SpellChoice.DOWN;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Aller à gauche
                    spellChoice = SpellChoice.LEFT;
                    break;
                }
                break;
            case SpellChoice.DOWN:
                selectedSpell = currentSpells[3];
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell.IsAvailable()){
                    SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.N)){
                    // Retour au centre
                    spellChoice = SpellChoice.CHARACTER;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)){
                    // Aller à droite
                    spellChoice = SpellChoice.RIGHT;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)){
                    // Aller en haut
                    spellChoice = SpellChoice.UP;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Aller à gauche
                    spellChoice = SpellChoice.LEFT;
                    break;
                }
                break;

            default:
                break;
        }

        // Affichage du spell dans l'information panel
        switch(spellChoice){
            case SpellChoice.CHARACTER:
                DisplayUnit(sourceUnit);
                break;
            case SpellChoice.LEFT:
                if(currentSpells.Count > 0){
                    DisplaySpell(currentSpells[0]);
                }
                break;
            case SpellChoice.RIGHT:
                if(currentSpells.Count > 1){
                    DisplaySpell(currentSpells[1]);
                }
                break;
            case SpellChoice.UP:
                if(currentSpells.Count > 2){
                    DisplaySpell(currentSpells[2]);
                }
                break;
            case SpellChoice.DOWN:
                if(currentSpells.Count > 3){
                    DisplaySpell(currentSpells[3]);
                }
                break;
            default:
                break;
        }
    }
    
    void SpellSelectionTrigger(BattleManager.Trigger trigger){
        // On sort de la sélection des spells pour aller vers un autre état
        BattleManager.Instance.ChangeState(BattleManager.Machine.PLAYERACTIONCHOICESTATE, trigger);
    }
    
    void TargetSelectionDisplay(){
        if(!activated_states[BattleManager.PlayerActionChoiceState.TARGET_SELECTION]){
            // Just changed from another state

            // Reset view
            ResetDisplay();

            // Activate the needed interface
            informationPanel.SetActive(true);
            tileSelector.SetActive(true);

            // TODO Définir par défaut l'emplacement de la targetTile, l'aléatoire c'est nul
            targetTile = GridManager.Instance.GetRandomTile(Team.Enemy);
            
            ActivateState(BattleManager.PlayerActionChoiceState.TARGET_SELECTION);
        }
        // Display selector on the target tile
        tileSelector.transform.position = targetTile.transform.position;
        GridManager.Instance.DisplayHighlights();

        // Highlight the selected tiles depending on the range of the spell

        // Change the tile or the state depending on the input (same algorithm than the source selection !)

        targetTile.Unselect();

        if (Input.GetKeyDown(KeyCode.B)){
            if(targetTile.GetUnit()!= null){
                if(GridManager.Instance.IsSelected(targetTile)){
                    TargetSelectionTrigger(BattleManager.Trigger.VALIDATE);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.N)){
            TargetSelectionTrigger(BattleManager.Trigger.CANCEL);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow)){
            if(targetTile.GetNextTile(Directions.UP) != null){
                targetTile = targetTile.GetNextTile(Directions.UP);
            }
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)){
            if(targetTile.GetNextTile(Directions.DOWN) != null){
                targetTile = targetTile.GetNextTile(Directions.DOWN);
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)){
            if(targetTile.GetNextTile(Directions.LEFT) != null){
                targetTile = targetTile.GetNextTile(Directions.LEFT);
            }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)){
            if(targetTile.GetNextTile(Directions.RIGHT) != null){
                targetTile = targetTile.GetNextTile(Directions.RIGHT);
            }
        }

        targetTile.Select();
        GridManager.Instance.SetSelectionMode(selectedSpell.GetRange());

        //Abstraire ce code
        BaseUnit currentUnit = targetTile.GetUnit();
        if(currentUnit != null){
            informationPanel.SetActive(true);
            DisplayUnit(currentUnit);
        }
        else{
            informationPanel.SetActive(false);
        }

        // Write into a variable the instruction if validated

        // Go back to the same spell selection if cancel
    }
    
    void TargetSelectionTrigger(BattleManager.Trigger trigger){
        // On sort de la sélection de la cible pour aller vers un autre état
        if(trigger == BattleManager.Trigger.VALIDATE){
            // Ajouter l'instruction dans la liste d'instructions
            Instruction instruction = BattleManager.Instance.CreateInstruction(sourceTile.GetUnit(), selectedSpell, targetTile);
            BattleManager.Instance.AssignInstruction(instruction);
        }
        BattleManager.Instance.ChangeState(BattleManager.Machine.PLAYERACTIONCHOICESTATE, trigger);
    }

    private void DisplaySpell(BaseSpell spell){
        unitPassiveNamePanel.text = spell.GetName();
        unitPassiveDescriptionPanel.text = spell.GetFightDescription();
        spellCooldownPanel.text = spell.GetCooldown().ToString() + " / " + spell.GetBaseCooldown().ToString();
    }

    private void DisplayUnit(BaseUnit unit){
        unitNamePanel.text = unit.GetName();
        unitPowerPanel.text = "Puissance : " + unit.GetFinalPower().ToString();
        unitHealthPanel.text = "PV : " + unit.GetFinalHealth().ToString() + "/" + unit.GetTotalHealth().ToString();
        if(unit.GetArmor() > 0){
            unitArmorPanel.text = "Armure : " + unit.GetArmor().ToString();
        }
        else{
            unitArmorPanel.text = "";
        }
        unitLevelPanel.text = "Niveau : " + unit.GetLevel().ToString();
        unitPassiveNamePanel.text = unit.GetPassive().GetName();
        unitPassiveDescriptionPanel.text = unit.GetPassive().GetFightDescription();
        spellCooldownPanel.text = "";
    }
    
    void ResetDisplay(){
            spellSelector.SetActive(false);
            shade.SetActive(false);
            informationPanel.SetActive(false);
            tileSelector.SetActive(false);
    }

    void ActivateState(BattleManager.PlayerActionChoiceState stateToActivate){
        Dictionary<BattleManager.PlayerActionChoiceState, bool> new_states = new Dictionary<BattleManager.PlayerActionChoiceState, bool>(activated_states);
        foreach (var state in activated_states)
        {
            if(state.Key == stateToActivate){
                new_states[state.Key] = true;
            }
            else{
                new_states[state.Key] = false;
            }
        }
        activated_states = new_states;
    }

    void Navigate(Directions direction){
        //
    }
}


public enum Directions {RIGHT, LEFT, UP, DOWN}
