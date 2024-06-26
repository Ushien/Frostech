using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inspiration : Passive
{
    void Awake()
    {
        passiveName = "Inspiration";
        fight_description = "Avant de lancer une technique sur un allié, retire ses états négatifs.";
    }
    // Lorsque le passif est setup, l'active
    override public void Activate()
    {
        EventManager.BeforeCast+= _Inspiration;
    }
    // Lorsque le passif disparaît, le désactive
    void OnDisable()
    {
        EventManager.BeforeCast -= _Inspiration;
    }
    void _Inspiration(BaseSpell spell, Tile targetTile){
        if(spell.GetOwner() == GetOwner() && targetTile != GetOwner().GetTile() && targetTile.GetUnit().GetTeam() == GetOwner().GetTeam()){
            targetTile.GetUnit().Cleanse();
        }
    }
}
