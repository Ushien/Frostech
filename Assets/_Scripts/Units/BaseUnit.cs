using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class BaseUnit : MonoBehaviour
{
    public ScriptableUnit scriptableUnit;
    public ScriptableJob scriptableJob;
    public Tile OccupiedTile;
    public BaseSpell attack;
    public Passive emptyPassive;
    public Passive passive;
    public List<BaseSpell> availableSpells;
    public Team Team = Team.Enemy;
    public int level = 1;
    public int stunTime = 0;
    public bool stun;

    private bool instructionGiven = false;

    public BaseSpell heroSpell1;
    public BaseSpell heroSpell2;

    public string unit_name = "Name";
    public int finalPower = 1;
    public int totalHealth = 1;
    public int finalHealth = 1;
    [TextArea(5,10)]
    public string lore_description = "Lore Description";
    [TextArea(5,10)]
    public string fight_description = "Fight Description";
    public bool dead = false;

    public int armor = 0;

    public Dictionary<Action<int>, List<Modifier>> modifiers = new Dictionary<Action<int>, List<Modifier>>();
    public Modifier emptyModifier;
    private List<Tuple<Action<int>, int, int>> actionQueue = new List<Tuple<Action<int>, int, int>>();

    public void Setup(ScriptableUnit originUnit, int setup_level, Team team){
        scriptableUnit = originUnit;
        this.name = scriptableUnit.unit_name;
        
        this.GetComponent<SpriteRenderer>().sprite = scriptableUnit.sprite;
        unit_name = scriptableUnit.unit_name;

        Team = team;
        level = setup_level;

        finalPower = GetStatFromLevel(scriptableUnit.original_power, level);
        totalHealth = GetStatFromLevel(scriptableUnit.original_health, level);
        finalHealth = totalHealth;

        lore_description = scriptableUnit.lore_description;
        fight_description = scriptableUnit.fight_description;

        modifiers[Heal] = new List<Modifier>();
        modifiers[Damage] = new List<Modifier>();

        if(scriptableUnit.passive != null){
            passive = Instantiate(scriptableUnit.passive);
        }
        else{
            passive = Instantiate(emptyPassive);
        }

        passive.Setup(this);

        attack = SpellManager.Instance.SetupAttack(this);

        foreach (BaseSpell spell in scriptableUnit.spells)
        {
            availableSpells.Add(SpellManager.Instance.SetupSpell(spell, this));
        }
        if (scriptableUnit.scriptableJob != null){
            LoadJob(scriptableUnit.scriptableJob);
        }
    }

    public void LoadJob(ScriptableJob scriptableJob){
        foreach (BaseSpell spell in scriptableJob.spells)
        {
            availableSpells.Add(SpellManager.Instance.SetupSpell(spell, this));
        }
        if (scriptableJob.passive != null){
            Destroy(passive.gameObject);
            passive = Instantiate(scriptableJob.passive);
            passive.Setup(this);
        }
    }

    public void Update(){
        if(HasGivenInstruction()){
            this.gameObject.GetComponent<SpriteRenderer>().color = new Color32( 173, 173, 173, 200);
        }
        else{
            this.gameObject.GetComponent<SpriteRenderer>().color = new Color32( 255, 255, 255, 255);
        }
    }

    public void Attack(Tile targetTile){
        attack.Cast(targetTile);
    }

    public int GetStatFromLevel(int level_100_stat, int real_level){

        var level_1_stat = (float)level_100_stat/10;
        var growth_by_level = (level_100_stat-level_1_stat)/99;
        var real_level_stat = Tools.Ceiling(level_1_stat+growth_by_level*(real_level-1));

        return real_level_stat;
    }

    public void CastSpell(int index){
        availableSpells[index].Cast();
    }

    public void GiveInstruction(bool newInstructionGiven){
        instructionGiven = newInstructionGiven;
    }

    public bool HasGivenInstruction(){
        return instructionGiven;
    }

    public string GetName(){
        return unit_name;
    }

    public string GetFightDescription(){
        return fight_description;
    }

    public int GetFinalPower(){
        return finalPower;
    }

    public void SetFinalPower(int amount){
        finalPower = amount;
    }

    public void ModifyPower(float amount){
        SetFinalPower(Tools.Ceiling(amount * finalPower + finalPower));
        CheckFinalPower();
    }

    private void CheckFinalPower(){
        if(GetFinalPower() < 0){
            SetFinalPower(0);
        }
    }
    public int GetTotalHealth(){
        return totalHealth;
    }

    public int GetFinalHealth(){
        return finalHealth;
    }

    public int GetArmor(){
        return armor;
    }

    public void SetArmor(int amount){
        if(amount >= 0){
            armor = amount;
        }
    }

    public void ModifyArmor(int amount){
        armor += amount;
        CheckArmor();
    }

    public void CheckArmor(){
        if(armor < 0){
            SetArmor(0);
        }
    }

    public bool isArmored(){
        return armor > 0;
    }

    public void ConvertArmorIntoHP(int amount){
        if(GetArmor() >= amount){
            ModifyArmor(-amount);
            ModifyBothHP(amount);
        }
    }

    public int GetLevel(){
        return level;
    }

    public Team GetTeam(){
        return Team;
    }

    public Passive GetPassive(){
        return passive;
    }

    public List<BaseSpell> GetSpells(bool includingAttack = false){

        if(includingAttack){
            
            List<BaseSpell> completeAvailableSpells = new List<BaseSpell>();
            foreach(BaseSpell spell in availableSpells){
                completeAvailableSpells.Add(spell);
            }
            completeAvailableSpells.Add(GetAttack());

            return completeAvailableSpells;
        }

        else{
            return availableSpells;
        }

    }

    public BaseSpell GetRandomSpell(bool includingAttack){
        return GetSpells(includingAttack).OrderBy(t => UnityEngine.Random.value).First();
    }

    public BaseSpell GetAttack(){
        return attack;
    }

    public Tile GetTile(){
        return OccupiedTile;
    }

    public void ModifyBothHP(int amount){
        ModifyTotalHP(amount);
        ModifyHP(amount);
    }

    public void MultiplyBothHP(int amount){
        MultiplyTotalHP(amount);
        MultiplyHP(amount);
    }

    public void ModifyTotalHP(int amount){
        totalHealth += amount;
        CheckTotalHP();
    }

    public void MultiplyTotalHP(int amount){
        totalHealth *= amount;
        CheckTotalHP();
    }

    public void Damage(int amount){
        int finalDamage = amount - GetArmor();
        if(finalDamage > 0){
            if(GetArmor() > 0){
                ModifyArmor(-GetArmor());
                BattleManager.Instance.AddEvent(new DamageEvent(this, -GetArmor(), true));
            }
            ModifyHP(-finalDamage);
            BattleManager.Instance.AddEvent(new DamageEvent(this, finalDamage));
        }
        else{
            ModifyArmor(-amount);
            BattleManager.Instance.AddEvent(new DamageEvent(this, amount, true));
        }

        
    }

    public void Heal(int amount){
        int finalAmount = amount;
        foreach (Modifier _modifier in modifiers[Heal])
        {
            finalAmount = Tools.Ceiling(_modifier.GetNewAmount(finalAmount));
        }
        ModifyHP(+finalAmount);
    }

    private void ModifyHP(int amount){
        finalHealth += amount;
        CheckHP();
    }

    public void MultiplyHP(int amount){
        finalHealth *= amount;
        CheckHP();
    }

    public void SetHP(int HP, bool check = true){
        finalHealth = HP;
        if(check){
            CheckHP();
        }
    }

    public void SetTotalHP(int HP, bool check = true){
        totalHealth = HP;
        if(check){
            CheckTotalHP();
        }
    }

    public void CheckHP(){
        if(AreHPBeyondMax()){
            SetHP(GetTotalHealth(), false);
        }
        if(AreHPBelowZero()){
            SetHP(0, false);
            Kill();
        }
    }

    public void CheckTotalHP(){
        if(AreTotalHPBelowZero()){
            SetTotalHP(0, false);
        }
        CheckHP();
    }

    public bool AreHPBelowZero(){
        return GetFinalHealth() <= 0;
    }
    public bool AreHPBeyondMax(){
        return GetFinalHealth() >= GetTotalHealth();
    }

    public bool AreTotalHPBelowZero(){
        return GetTotalHealth() <= 0;
    }

    public void SetStunTime(int amount){
        stunTime = amount;
        CheckStun();
    }

    public void ModifyStunTime(int amount){
        stunTime += amount;
        CheckStun();
    }

    public void CheckStun(){
        if (stunTime > 0){
            stun = true;
        }
        else{
            stun = false;
        }
    }

    public void Cleanse(Status status){
        switch (status)
        {
            case Status.Stun:
                SetStunTime(0);
                break;

            default:
                break;
        }
    }

    public void Cleanse(List<Status> statusList){

        foreach (Status status in statusList)
        {
            Cleanse(status);
        }
    }

    public void Cleanse(){
        Cleanse(SpellManager.Instance.GetCleansableStatus());
    }

    public bool IsAvailable(){
        bool available = true;
        if(dead){
            available = false;
        }
        if(stun){
            available = false;
        }
        return available;
    }

    public void Kill(){
        dead = true;
        EventManager.Instance.UnitDied(this);
        UnitManager.Instance.Kill(this);
    }

    public void QueueAction(Action<int> action, int parameter, int howManyTurns){
        actionQueue.Add(new Tuple<Action<int>, int, int>(action, parameter, howManyTurns));
    }

    public void ReduceQueueTurns(){
        List<Tuple<Action<int>, int, int>> newQueue = new List<Tuple<Action<int>, int, int>>();

        foreach (Tuple<Action<int>, int, int> queuedAction in actionQueue)
        {
            newQueue.Add(new Tuple<Action<int>, int, int>(queuedAction.Item1, queuedAction.Item2, queuedAction.Item3 - 1));
        }

        actionQueue = newQueue;
    }

    public void ApplyQueueActions(){
        foreach (Tuple<Action<int>, int, int> queuedAction in actionQueue)
        {
            if(queuedAction.Item3 == 0){
                queuedAction.Item1(queuedAction.Item2);
            }
        }

        List<Tuple<Action<int>, int, int>> newQueue = new List<Tuple<Action<int>, int, int>>();

        foreach (Tuple<Action<int>, int, int> queuedAction in actionQueue)
        {
            if(queuedAction.Item3 != 0){
                newQueue.Add(new Tuple<Action<int>, int, int>(queuedAction.Item1, queuedAction.Item2, queuedAction.Item3));
            }
        }

        actionQueue = newQueue;
    }

    public void AddModifier(Modifier modifier, Action<int> function){
        modifiers[function].Add(modifier);
    }

    public void DeleteModifier(Modifier modifier, Action<int> function){
        modifiers[function].Remove(modifier);
    }

    private void ModifierEndTurn(){
        foreach (var action in modifiers)
        {
            foreach (Modifier _modifier in action.Value)
            {
                _modifier.ModifyTurns(-1);
                if(_modifier.IsEnded()){
                    //FIXME this les listes aiment pas beaucoup ça
                    action.Value.Remove(_modifier);
                }
            }
        }
    }

    public void ApplyEndturnEffects(){
        ModifyStunTime(-1);
        foreach (BaseSpell spell in GetSpells())
        {
            spell.ApplyEndTurnEffects(); 
        }
        GetAttack().ApplyEndTurnEffects();
        ModifierEndTurn();
        ReduceQueueTurns();
        ApplyQueueActions();
    }

    public List<string> GetInfo(){
        List<string> infos = new List<string>();
        /*
        infos.Add(unit_name);
        infos.Add(Team);
        infos.Add(level);
        infos.Add(finalPower);
        infos.Add(finalLife);
        infos.Add(fight_description);
        infos.Add(armor);
        infos.Add();
        infos.Add();
        infos.Add();
        */
        return infos;
    }
}