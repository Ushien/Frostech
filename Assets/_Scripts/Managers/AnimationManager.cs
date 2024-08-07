using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// Contient toutes les méthodes relatives à l'animation
/// </summary>

public class AnimationManager : MonoBehaviour
{
    public static AnimationManager Instance;
    public TextMeshProUGUI damageText;
    public GameObject DamageSection;

    void Awake(){
        Instance = this;
    }

    public async Task Animate(List<BattleEvent> battleEvents){

        for (var i = 0; i < battleEvents.Count; i++)
        {
           await Animate(battleEvents[i]);
           await Task.Delay(500);
        }
        BattleManager.Instance.SetInAnimation(false);
    }

    private async Task Animate(CastEvent castEvent){
        for (float distance = 0.0f; distance <= 0.4f; distance += 0.02f)
        {
            castEvent.GetSourceUnit().gameObject.transform.Translate(new Vector3(0, 0.02f, 0));
            await Task.Yield();
        }
        for (float distance = 0.4f; distance >= 0.0f; distance -= 0.02f)
        {
            castEvent.GetSourceUnit().gameObject.transform.Translate(new Vector3(0, -0.02f, 0));
            await Task.Yield();
        }
    }

    private async Task Animate(DamageEvent damageEvent){
        TextMeshProUGUI damageDisplay = Instantiate(damageText);
        damageDisplay.transform.SetParent(DamageSection.transform);
        damageDisplay.text = "-" + damageEvent.GetAmount().ToString();
        damageDisplay.transform.position = damageEvent.GetTargetUnit().transform.position;
        damageDisplay.transform.localScale = new Vector3(1, 1, 1);
        damageDisplay.gameObject.SetActive(true);
        for (float distance = 0.0f; distance <= 0.5f; distance += 0.005f)
        {
            damageDisplay.gameObject.transform.Translate(new Vector3(0, 0.005f, 0));
            await Task.Yield();
        }
        damageDisplay.gameObject.SetActive(false);
        Destroy(damageDisplay.gameObject);
    }

    private async Task Animate(BattleEvent battleEvent){
        if (battleEvent is CastEvent){
            await Animate((CastEvent)battleEvent);
        }
        if (battleEvent is DamageEvent){
            await Animate((DamageEvent)battleEvent);
        }
    }
}
