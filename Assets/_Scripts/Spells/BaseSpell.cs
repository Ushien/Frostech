using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Représente une attaque ou une technique
/// </summary>
public class BaseSpell : MonoBehaviour
{

        #region Fields

        #region Fields de setup
    public ScriptableSpell scriptableSpell;
    public Modifier modifier;

        #endregion
        #region Références à d'autres objets
    public BaseUnit owner;
        #endregion

        #region Caractérstiques
    public string spell_name = "Name";
    private bool isATechnique = true;
    [TextArea(5,10)]
    public string lore_description = "Lore Description";
    [TextArea(5,10)]
    public string fight_description = "Fight Description";
    // Cooldown total du sort.
    public int base_cooldown = 0;
    // Cooldown actuel du sort. Lorsque celui-ci est égal au cooldown total, le sort peut être lancé. Quand le sort est lancé, celui-ci passe à zéro. Il augmente ensuite de 1 par tour.
    public int cooldown = 0;
    public Sprite artwork = null;
    // Indique la portée du sort (horizontale, verticale, toutes les unités, etc...)
    public GridManager.Selection_mode range;
    // Indique si un sort ne peut être lancé que sur une équipe en particulier
    public GridManager.Team_restriction team_restriction;

    /// Ratio associés au sort, doivent être définis au sein de la fonction Awake propre au sort.
    public float ratio1 = 1f;
    public float ratio2 = 1f;
    public float ratio3 = 1f;
        #endregion

        #region Fields relatifs au moteur de jeu

    public List<Modifier> modifiers = new List<Modifier>();
        #endregion
        #endregion

    ////////////////////////////////////////////////////////

        #region Méthodes d'initialisation

    /// <summary>
    /// Initialise le sort
    /// </summary>
    /// <param name="ownerUnit">Unité possédant le sort</param>
    public void Setup(BaseUnit ownerUnit){
        this.name = scriptableSpell.spell_name;

        owner = ownerUnit;
        modifier = ownerUnit.emptyModifier;
        
        spell_name = scriptableSpell.spell_name;
        lore_description = scriptableSpell.lore_description;
        fight_description = scriptableSpell.fight_description;
        base_cooldown = scriptableSpell.cooldown;
        cooldown = base_cooldown;
        artwork = scriptableSpell.artwork;
        range = scriptableSpell.range;
        team_restriction = scriptableSpell.team_restriction;
    }
        #endregion
        #region Actions du sort
    /// <summary>
    /// Lance le sort. Méthode à overrider pour intégrer l'effet du sort que l'on souhaite implémenter.
    /// </summary>
    virtual public void Cast(){
        Debug.Log("Méthode overridée");
    }

    /// <summary>
    /// Lance le sort. Méthode à overrider pour intégrer l'effet du sort que l'on souhaite implémenter.
    /// </summary>
    virtual public void Cast(Tile targetTile){
        Debug.Log("Méthode overridée");
    }

    /// <summary>
    /// Méthode à appeler dans l'implémentation du sort, qui s'occupe de toutes les vérifications communes à chaque sort.
    /// </summary>
    /// <param name="targetTile"></param>
    /// <param name="spellFunction"></param>
    virtual public void CastSpell(Tile targetTile, Action<Tile> spellFunction){
        BaseUnit targetUnit = null;
        if (targetTile != null){
            targetUnit = targetTile.GetUnit();
        }

        if(targetUnit != null && IsAvailable() && GetOwner().IsAvailable()){

            if (IsATechnique()){
                EventManager.Instance.BeforeTechCast(this, targetTile);
            }

            if(targetTile.GetUnit() != null){
                Debug.Log(GetOwner().GetName() + " lance " + GetName() + " sur " + targetTile.GetUnit().GetName());
            }
            else{
                Debug.Log(GetOwner().GetName() + " lance " + GetName() + " sur " + targetTile.name);
            }
            BattleManager.Instance.AddEvent(new CastEvent(GetOwner(), this, targetTile));
            //await AnimationManager.Instance.Jump(GetOwner().gameObject);

            SetCooldown(0);

            spellFunction(targetTile);

            if (IsATechnique()){
                EventManager.Instance.AfterTechCast(this, targetTile);
            }

        }
    }
        #endregion

        #region Manipulation des caractéristiques du sort

    /// <summary>
    /// Renvoie l'unité en possession du sort
    /// </summary>
    /// <returns></returns>
    public BaseUnit GetOwner(){
        return owner;
    }

    /// <summary>
    /// Renvoie la portée du sort
    /// </summary>
    /// <returns></returns>
    public GridManager.Selection_mode GetRange(){
        return range;
    }

    /// <summary>
    /// Renvoie la description en combat du sort, avec les nombres convertis en fonction de la puissance du personnage.
    /// </summary>
    /// <returns></returns>
    public string GetFightDescription(){
        string _fight_description = fight_description.Clone().ToString();
        _fight_description = _fight_description.Replace("%%1", GetFinalDamages(GetRatio()[0]).ToString());
        _fight_description = _fight_description.Replace("%%2", GetFinalDamages(GetRatio()[1]).ToString());
        _fight_description = _fight_description.Replace("%%3", GetFinalDamages(GetRatio()[2]).ToString());

        _fight_description = _fight_description.Replace("__1", DisplayPercents(GetRatio()[0]));
        _fight_description = _fight_description.Replace("__2", DisplayPercents(GetRatio()[1]));
        _fight_description = _fight_description.Replace("__3", DisplayPercents(GetRatio()[2]));
        return _fight_description;
    }
    /// <summary>
    /// Renvoie les différents ratios associés au sort
    /// </summary>
    /// <returns></returns>
    virtual public List<float> GetRatio(){
        return new List<float>{
            ratio1,
            ratio2,
            ratio3
        };

    }

        #endregion

        #region Gestion des cooldowns

    /// <summary>
    /// Renvoie le nom du sort
    /// </summary>
    /// <returns></returns>
    public string GetName(){
        return spell_name;
    }

    /// <summary>
    /// Augmente le cooldown actuel d'un nombre donné
    /// </summary>
    /// <param name="amount"></param>
    public void ModifyCooldown(int amount){
        cooldown += amount;
        CheckCooldown();
    }
    
    /// <summary>
    /// Vérifie si les cooldowns sont légaux et les ajuste au besoin
    /// </summary>
    public void CheckCooldown(){
        if(cooldown > base_cooldown){
            cooldown = base_cooldown;
        }
        if(cooldown < 0){
            cooldown = 0;
        }
    }

    /// <summary>
    /// Fixe le cooldown actuel du sort à un nombre donné
    /// </summary>
    /// <param name="amount"></param>
    public void SetCooldown(int amount){
        cooldown = amount;
    }

    /// <summary>
    /// Renvoie le cooldown actuel du sort
    /// </summary>
    /// <returns></returns>
    public int GetCooldown(){
        return cooldown;
    }

    /// <summary>
    /// Renvoie le cooldown total du sort
    /// </summary>
    /// <returns></returns>
    public int GetBaseCooldown(){
        return base_cooldown;
    }
        #endregion

        #region Methodes relatives au moteur de jeu
    /// <summary>
    /// Indique si le sort est prêt à être utilisé ou pas, en respectant les différentes contraintes définies
    /// </summary>
    /// <returns></returns>
    public bool IsAvailable(){
        bool availability = true;
        if(GetCooldown() < GetBaseCooldown()){
            availability = false;
        }
        return availability;
    }

    /// <summary>
    /// Indique si le sort est une technique ou pas
    /// </summary>
    /// <returns></returns>
    public bool IsATechnique(){
        return isATechnique;
    }

    /// <summary>
    /// Définit si le sort est une technique ou non
    /// </summary>
    /// <param name="value"></param>
    public void SetIsATechnique(bool value){
        isATechnique = value;
    }

    /// <summary>
    /// Applique tous les effets de fin de tour liés au sort
    /// </summary>
    public void ApplyEndTurnEffects(){
        ModifyCooldown(+1);
        ModifierEndTurn();
    }
        #endregion

        #region Gestion de types et conversions
    /// <summary>
    /// Convertit un ratio donné avec la puissance de l'unité et renvoie la quantité finale de dégats
    /// </summary>
    /// <param name="_ratio"></param>
    /// <returns></returns>
    public int GetFinalDamages(float _ratio){
        int finalAmount = Tools.Ceiling(_ratio * GetOwner().GetFinalPower());
        
        foreach (Modifier _modifier in modifiers)
        {
            finalAmount = Tools.Ceiling(_modifier.GetNewAmount(finalAmount));
        }

        return finalAmount;
    }

    /// <summary>
    /// Dépréciée
    /// </summary>
    /// <param name="ratio"></param>
    /// <returns></returns>
    public float ApplyPower(float ratio){
        return ratio * GetOwner().GetFinalPower();
    }

    /// <summary>
    /// Convertit un ratio du décimal vers les pourcentages. (0.2 -> 20%)
    /// </summary>
    /// <param name="percentRatio"></param>
    /// <returns></returns>
    public string DisplayPercents(float percentRatio){
        return (percentRatio * 100).ToString();
    }
        #endregion

        #region Gestion des modificateurs

    /// <summary>
    /// Ajoute un modificateur sur le sort
    /// </summary>
    /// <param name="modifier"></param>
    public void AddModifier(Modifier modifier){
        modifiers.Add(modifier);
    }

    /// <summary>
    /// Supprime un modificateur sur le sort
    /// </summary>
    /// <param name="modifier"></param>
    public void DeleteModifier(Modifier modifier){
        modifiers.Remove(modifier);
    }


    /// <summary>
    ///  Diminue la durée restante des modificateurs de 1 et les supprime si celle-ci vaut 0
    /// </summary>
    private void ModifierEndTurn(){
        if(modifiers.Count > 0){
        }
        foreach (Modifier modifier in modifiers)
        {
            modifier.ModifyTurns(-1);
            if(modifier.IsEnded()){
                //FIXME les listes aiment pas beaucoup ça
                modifiers.Remove(modifier);
            }
        }
    }
        #endregion
}
