using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contient toutes les méthodes relatives à l'intelligence artificielle du jeu
/// </summary>

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    void Awake(){
        Instance = this;
    }

    /*
    Renvoie une liste d'instructions aléatoires pour une équipe donnée
    */
    private List<Instruction> GetDummyAIOrders(Team team){
        List<Instruction> instructions = new List<Instruction>();

        foreach (BaseUnit unit in UnitManager.Instance.GetUnits(Team.Enemy)){
            instructions.Add(new Instruction(unit, unit.GetRandomSpell(includingAttack : true), UnitManager.Instance.GetRandomUnit(Team.Ally).GetTile()));
        }

        return instructions;
    }

    /*
    Renvoie une liste d'instructions pour une équipe donnée
    */
    public List<Instruction> GetAIOrders(Team team){
        return GetDummyAIOrders(team);
    }
}
