using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sacrements : BaseSpell
{
    float ratio1 = 1.1f;
    float ratio2 = 1.4f;
    override public void Cast(Tile targetTile = null){
        base.CastSpell(targetTile, _Sacrements);
    }

    private void _Sacrements(Tile targetTile){
        float finalAmount1 = ratio1 * owner.finalPower;
        float finalAmount2 = ratio2 * owner.finalPower;
       
        if(targetTile.GetUnit().isArmored()){
            SpellManager.Instance.InflictDamage(finalAmount2, targetTile.GetUnit());
        }
        else{
            SpellManager.Instance.InflictDamage(finalAmount1, targetTile.GetUnit());
        }
    }
}