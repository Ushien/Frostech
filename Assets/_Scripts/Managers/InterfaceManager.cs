using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// Gestion de l'interface de jeu
/// </summary>

public class InterfaceManager : MonoBehaviour
{
    #region Fields
    public static InterfaceManager Instance;

    // UI elements

    [SerializeField]
    private GameObject UIobject;
    public GameObject infosPanel;
    public GameObject informationPanel;
    public GameObject passivePanel;
    public GameObject spellPanel;
    public GameObject alliesLifeBar;
    public GameObject ennemiesLifeBar;
    public TextMeshProUGUI unitNamePanel;
    public TextMeshProUGUI unitPowerPanel;
    public TextMeshProUGUI unitHealthPanel;
    public TextMeshProUGUI unitArmorPanel;
    public TextMeshProUGUI unitLevelPanel;
    public RectTransform infosPanelLine;
    public TextMeshProUGUI unitPassiveNamePanel;
    public TextMeshProUGUI unitPassiveDescriptionPanel;
    public TextMeshProUGUI spellNamePanel;
    public TextMeshProUGUI spellCooldownPanel;
    public TextMeshProUGUI spellDescriptionPanel;
    public RectTransform spellPanelLine;
    public RectTransform spellSelectorLine;
    public Image spellPanelIcon;
    public GameObject spellSelector;
    public GameObject shade;
    public GameObject lifeBarPrefab;
    public Material grayscaleShader;
    public Sprite emptySpellSelectorSquare;
   

    private GameObject tileSelector;
    private Vector3 tileSelector_targetPos;
    private Vector3 tileSelector_currentPos;

    public Vector3 lifeBarOffset;

    [SerializeField]
    private float selectorSpeed;

    [SerializeField]
    private Material tileOutliner;
    public Camera mainCamera; // Utile pour convertir des position in game à des positions en pixels sur l'écran

    public float tileSize = 250f; // A bit ugly but still good for now

    // La Tile contenant la source du spell, lorsqu'un spell est lancé (lorsqu'on revient en arrière pendant la sélection de cible)
    private Tile sourceTile;

    private enum SpellChoice{CHARACTER, LEFT, RIGHT, UP, DOWN}
    private SpellChoice spellChoice;

    [SerializeField]
    private bool overloaded = false;

    // Le spell pour lequel on va sélectionner une cible
    private BaseSpell selectedSpell;

    // La Tile contenant la cible du spell, lorsqu'un spell est lancé
    public Tile targetTile;

    private Dictionary<BattleManager.PlayerActionChoiceState, bool> activated_states;

    #endregion

    void Awake(){
        Instance = this;
        activated_states = new Dictionary<BattleManager.PlayerActionChoiceState, bool>();
        foreach (BattleManager.PlayerActionChoiceState state in System.Enum.GetValues(typeof(BattleManager.PlayerActionChoiceState)))
        {
            activated_states[state] = false;
        }

        // On crée le tileSelector qui va naviguer pour la sélection des cases

        tileSelector = Instantiate(GridManager.Instance.GetTilePrefab()).gameObject;
        Material material = Instantiate(tileOutliner);

        tileSelector.transform.GetComponent<UnityEngine.SpriteRenderer>().material = material;
        tileSelector.transform.GetComponent<UnityEngine.SpriteRenderer>().sortingOrder = 2;
        tileSelector.transform.parent = UIobject.transform;
        tileSelector.name = "Tile Selector";
        tileSelector.transform.DetachChildren();

        tileSelector_targetPos = tileSelector.transform.position;
        tileSelector_currentPos = tileSelector.transform.position;


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
            infosPanel.SetActive(true);
            tileSelector.SetActive(true);

            GridManager.Instance.GetMiddleTile(Team.Ally).Select();
            GridManager.Instance.SetSelectionMode(GridManager.Selection_mode.Single_selection);

            ActivateState(BattleManager.PlayerActionChoiceState.CHARACTER_SELECTION);

        }
        sourceTile = GridManager.Instance.GetMainSelection();

        // Change la position du sélector de case à l'aide d'un joli lerp
        tileSelector_targetPos = sourceTile.transform.position;
        tileSelector_currentPos = Vector3.Lerp(tileSelector_currentPos, tileSelector_targetPos, Time.deltaTime*selectorSpeed);
        tileSelector.transform.position = tileSelector_currentPos;

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
            infosPanel.SetActive(true);
            DisplayUnit(currentUnit);
            //DrawPanelLine(infosPanelLine, sourceTile);
        }
        else{
            infosPanel.SetActive(false);
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
        BaseSpell[] currentSpells = sourceUnit.GetSpells();

        if(!activated_states[BattleManager.PlayerActionChoiceState.SPELL_SELECTION]){
            // Just changed from another state

            // Reset view
            ResetDisplay();

            // Activate the needed interface
            spellSelector.SetActive(true);
            shade.SetActive(true);
            infosPanel.SetActive(true);
            spellPanel.SetActive(true);
            spellPanelLine.gameObject.SetActive(false);

            //spellSelector.transform.position = sourceTile.transform.position;
            DrawPanelLine(spellSelectorLine, sourceTile);

            int currentSpellIndex = 0;

            foreach (BaseSpell spell in currentSpells)
            {
                if(spell != null){
                    spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().sprite = spell.GetArtwork();
                    spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().material = null;
                    if(!spell.IsAvailable()){
                        Material material = Instantiate(grayscaleShader);
                        spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().material = material;
                        //Grey
                    }              
                }
                else{
                    spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().sprite = emptySpellSelectorSquare;
                    spellSelector.transform.GetChild(currentSpellIndex).GetComponent<UnityEngine.UI.Image>().material = null;
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
                AnimateSpellChoice(4);
                break;
            case SpellChoice.LEFT:
                spellSelector.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(true);
                AnimateSpellChoice(0);
                break;
            case SpellChoice.RIGHT:
                spellSelector.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(true);
                AnimateSpellChoice(1);
                break;
            case SpellChoice.UP:
                spellSelector.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(true);
                AnimateSpellChoice(2);
                break;
            case SpellChoice.DOWN:
                spellSelector.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(true);
                AnimateSpellChoice(3);
                break;
            default:
                break;
        }

        // Navigation au sein de la sélection
        switch(spellChoice){
            case SpellChoice.CHARACTER:
                overloaded = false;
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
                //TODO Ce code se répète 4 fois, il y a moyen de refactor
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell != null){
                    if(selectedSpell.IsAvailable()){
                        SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    }
                    break;
                }
                if (Input.GetKeyDown(KeyCode.N)){
                    // Retour au centre
                    spellChoice = SpellChoice.CHARACTER;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)){
                    // Aller en haut
                    spellChoice = SpellChoice.UP;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Aller en bas
                    spellChoice = SpellChoice.DOWN;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)){
                    // Aller à droite
                    spellChoice = SpellChoice.RIGHT;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Passer en mode surcharge / Revenir au mode non surchargé
                    spellChoice = SpellChoice.LEFT;
                    overloaded = !overloaded;
                    break;
                }
                break;
            case SpellChoice.RIGHT:
                selectedSpell = currentSpells[1];
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell != null){
                    if(selectedSpell.IsAvailable()){
                        SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    }
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
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Aller en bas
                    spellChoice = SpellChoice.DOWN;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Aller à gauche
                    spellChoice = SpellChoice.LEFT;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)){
                    // Passer en mode surcharge / Revenir au mode non surchargé
                    spellChoice = SpellChoice.RIGHT;
                    overloaded = !overloaded;
                    break;
                }
                break;
            case SpellChoice.UP:
                selectedSpell = currentSpells[2];
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell != null){
                    if(selectedSpell.IsAvailable()){
                        SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    }
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
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Aller en bas
                    spellChoice = SpellChoice.DOWN;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Aller à gauche
                    spellChoice = SpellChoice.LEFT;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)){
                    // Passer en mode surcharge / Revenir au mode non surchargé
                    spellChoice = SpellChoice.UP;
                    overloaded = !overloaded;
                    break;
                }
                break;
            case SpellChoice.DOWN:
                selectedSpell = currentSpells[3];
                if (Input.GetKeyDown(KeyCode.B) && selectedSpell != null){
                    if(selectedSpell.IsAvailable()){
                        SpellSelectionTrigger(BattleManager.Trigger.VALIDATE);
                    }
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
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)){
                    // Aller en haut
                    spellChoice = SpellChoice.UP;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)){
                    // Aller à gauche
                    spellChoice = SpellChoice.LEFT;
                    overloaded = false;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)){
                    // Passer en mode surcharge / Revenir au mode non surchargé
                    spellChoice = SpellChoice.DOWN;
                    overloaded = !overloaded;
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
                DisplaySpell(null, hyper : overloaded);
                break;
            case SpellChoice.LEFT:
                DisplaySpell(currentSpells[0], hyper : overloaded);
                break;
            case SpellChoice.RIGHT:
                DisplaySpell(currentSpells[1], hyper : overloaded);
                break;
            case SpellChoice.UP:
                DisplaySpell(currentSpells[2], hyper : overloaded);
                break;
            case SpellChoice.DOWN:
                DisplaySpell(currentSpells[3], hyper : overloaded);
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
            infosPanel.SetActive(true);
            tileSelector.SetActive(true);
            spellSelector.SetActive(true);
            shade.SetActive(true);
            spellPanel.SetActive(true);
            spellPanelLine.gameObject.SetActive(true);

            // TODO Définir par défaut l'emplacement de la targetTile, l'aléatoire c'est nul
            targetTile = GridManager.Instance.GetRandomTile(Team.Enemy);
            
            ActivateState(BattleManager.PlayerActionChoiceState.TARGET_SELECTION);
        }

        // Change la position du sélector de case à l'aide d'un joli lerp
        targetTile.transform.position = targetTile.transform.position;
        tileSelector_currentPos = Vector3.Lerp(tileSelector_currentPos, tileSelector_targetPos, Time.deltaTime*selectorSpeed);
        tileSelector.transform.position = tileSelector_currentPos;
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
        DrawPanelLine(spellPanelLine, targetTile);
        GridManager.Instance.SetSelectionMode(selectedSpell.GetRange());

        //Abstraire ce code
        BaseUnit currentUnit = targetTile.GetUnit();

        // N'aidait pas à la lisibilité d'après moi
        // (en gros, le personnage ennemi était toujours sélectionné, je préfère qu'on ait toujours les infos sur le lanceur)
        //if(currentUnit != null){
            //infosPanel.SetActive(true);
            //DisplayUnit(currentUnit);
            //DrawPanelLine(infosPanelLine, targetTile);
        //}
        //else{
            //infosPanel.SetActive(false);
        //}

        // Write into a variable the instruction if validated

        // Go back to the same spell selection if cancel
    }
    
    void TargetSelectionTrigger(BattleManager.Trigger trigger){
        // On sort de la sélection de la cible pour aller vers un autre état
        if(trigger == BattleManager.Trigger.VALIDATE){
            // Ajouter l'instruction dans la liste d'instructions
            if(selectedSpell == null){
                Debug.Log("Pas normal ça");
            }
            ResetDisplay();
            Instruction instruction = BattleManager.Instance.CreateInstruction(sourceTile.GetUnit(), selectedSpell, targetTile, hyper : overloaded);
            BattleManager.Instance.AssignInstruction(instruction);
        }
        BattleManager.Instance.ChangeState(BattleManager.Machine.PLAYERACTIONCHOICESTATE, trigger);
    }

    private void DisplaySpell(BaseSpell spell, bool hyper = false){
        if(spell != null){
            spellPanel.SetActive(true);
            spellNamePanel.text = spell.GetName();
            spellDescriptionPanel.text = spell.GetFightDescription(hyper);
            spellCooldownPanel.text = spell.GetCooldown().ToString() + " / " + spell.GetBaseCooldown().ToString();  
            spellPanelIcon.sprite = spell.GetArtwork();
        }
        else{
            spellPanel.SetActive(false);
            spellNamePanel.text="";
            spellDescriptionPanel.text = "";
            spellCooldownPanel.text = "";
        }
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
            infosPanel.SetActive(false);
            tileSelector.SetActive(false);
            spellPanel.SetActive(false);
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

    private void DrawPanelLine(RectTransform PanelLine, Tile tile){
        Vector3 targetPosition = mainCamera.WorldToScreenPoint(tile.transform.position);
        if(PanelLine == spellSelectorLine)
            PanelLine.sizeDelta = new Vector2(targetPosition.x - tileSize,  Screen.height - spellSelector.GetComponent<RectTransform>().rect.height - targetPosition.y); // a bit ugly but still good      
        //if(PanelLine == infosPanelLine)
        //    PanelLine.sizeDelta = new Vector2(targetPosition.x - tileSize, Screen.height - infosPanel.GetComponent<RectTransform>().rect.height - targetPosition.y); // a bit ugly but still good
            //PanelLine.sizeDelta = new Vector2(targetPosition.x - tileSize, targetPosition.y - infosPanel.GetComponent<RectTransform>().rect.height); // a bit ugly but still good      
        if (PanelLine == spellPanelLine)
            PanelLine.sizeDelta = new Vector2(Screen.width-targetPosition.x - tileSize, targetPosition.y - spellPanel.GetComponent<RectTransform>().rect.height); // Hard coded, needs some update
    } 
    
    public GameObject SetupLifebar(string unitName, Vector3 barPosition, int totalHealth, int armor, Team team){
        
        // GameObject instanciation
        GameObject lifeBarPanel = Instantiate(lifeBarPrefab);
        lifeBarPanel.transform.parent = (team == Team.Ally) ? alliesLifeBar.transform : ennemiesLifeBar.transform;
        lifeBarPanel.transform.localScale = new Vector3(1, 1, 1);
        lifeBarPanel.transform.position = barPosition + lifeBarOffset;
        lifeBarPanel.name = $"{unitName}_LifeBar";

        // Child components access and modification, very ugly
        TextMeshProUGUI HP = lifeBarPanel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI AR = lifeBarPanel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
        HP.text = $"{totalHealth} HP";
        AR. text = $"{armor} AR";

        //Armor initialization
        Transform armorBar = lifeBarPanel.transform.GetChild(4);
        armorBar.localScale = new Vector3((float)armor/totalHealth, 1, 1);

        return lifeBarPanel;
    }
    
    public void UpdateLifebar(BaseUnit unit){
        
        // Child components access and modification, very ugly
        TextMeshProUGUI HP = unit.lifeBar.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI AR = unit.lifeBar.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
        HP.text = $"{unit.finalHealth} HP";
        AR. text = $"{unit.armor} AR";

        //Armor initialization
        Transform lifeBar  = unit.lifeBar.transform.GetChild(3);
        Transform armorBar = unit.lifeBar.transform.GetChild(4);
        lifeBar.localScale = new Vector3((float)unit.finalHealth/unit.totalHealth, 1, 1);
        armorBar.localScale = new Vector3((float)unit.armor/unit.totalHealth, 1, 1);
    }

    public void KillLifeBar(GameObject lifeBarPanel){
        Destroy(lifeBarPanel);
    }

    private void AnimateSpellChoice(int index){
        for (int i=0; i<5; i++){
            if (i == index)
                continue;
            spellSelector.transform.GetChild(3).gameObject.GetComponent<Animator>().Play("Empty");
        }
        if (index==0)
            spellSelector.transform.GetChild(0).GetComponent<Animator>().Play("LeftSelected");
        if (index==1)
            spellSelector.transform.GetChild(1).GetComponent<Animator>().Play("RightSelected");
        if (index==2)
            spellSelector.transform.GetChild(2).GetComponent<Animator>().Play("TopSelected");
        if (index==3)
            spellSelector.transform.GetChild(3).GetComponent<Animator>().Play("DownSelected");
        if (index==4)
            spellSelector.transform.GetChild(4).GetComponent<Animator>().Play("AttackSelected");
    }
}


public enum Directions {RIGHT, LEFT, UP, DOWN}
