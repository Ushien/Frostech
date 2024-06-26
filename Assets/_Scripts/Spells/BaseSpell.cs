using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BaseSpell : MonoBehaviour
{
    public ScriptableSpell scriptableSpell;
    public BaseUnit owner;

    public Modifier modifier;

    public string spell_name = "Name";
    private bool isATechnique = true;
    [TextArea(5,10)]
    public string lore_description = "Lore Description";
    [TextArea(5,10)]
    public string fight_description = "Fight Description";
    public int cooldown = 0;
    public int base_cooldown = 0;
    public Sprite artwork = null;
    public GridManager.Selection_mode range;
    public GridManager.Team_restriction team_restriction;

    public float ratio1 = 1f;
    public float ratio2 = 1f;
    public float ratio3 = 1f;

    public List<Modifier> modifiers = new List<Modifier>();

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

    virtual public void Cast(){
        Debug.Log("Méthode overridée");
    }

    virtual public void Cast(Tile targetTile){
        Debug.Log("Méthode overridée");
    }

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

    virtual public List<float> GetRatio(){
        return new List<float>{
            ratio1,
            ratio2,
            ratio3
        };

    }

    public string GetName(){
        return spell_name;
    }

    public void ModifyCooldown(int amount){
        cooldown += amount;
        CheckCooldown();
    }
    
    public void CheckCooldown(){
        if(cooldown > base_cooldown){
            cooldown = base_cooldown;
        }
        if(cooldown < 0){
            cooldown = 0;
        }
    }

    public void SetCooldown(int amount){
        cooldown = amount;
    }

    public int GetCooldown(){
        return cooldown;
    }

    public int GetBaseCooldown(){
        return base_cooldown;
    }

    public bool IsAvailable(){
        bool availability = true;
        if(GetCooldown() < GetBaseCooldown()){
            availability = false;
        }
        return availability;
    }
    public bool IsATechnique(){
        return isATechnique;
    }

    public void SetIsATechnique(bool value){
        isATechnique = value;
    }

    public BaseUnit GetOwner(){
        return owner;
    }

    public GridManager.Selection_mode GetRange(){
        return range;
    }

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

    public int GetFinalDamages(float _ratio){
        int finalAmount = Tools.Ceiling(_ratio * GetOwner().GetFinalPower());
        
        foreach (Modifier _modifier in modifiers)
        {
            finalAmount = Tools.Ceiling(_modifier.GetNewAmount(finalAmount));
        }

        return finalAmount;
    }

    public float ApplyPower(float ratio){
        return ratio * GetOwner().GetFinalPower();
    }

    public string DisplayPercents(float percentRatio){
        return (percentRatio * 100).ToString();
    }

    public void AddModifier(Modifier modifier){
        modifiers.Add(modifier);
    }

    public void DeleteModifier(Modifier modifier){
        modifiers.Remove(modifier);
    }

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

    public void ApplyEndTurnEffects(){
        ModifyCooldown(+1);
        ModifierEndTurn();
    }
}
