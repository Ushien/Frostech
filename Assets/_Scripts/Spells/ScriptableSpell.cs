using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spell")]

public class ScriptableSpell : ScriptableObject
{
    public string spell_name;
    [TextArea(5,10)]
    public string lore_description;
    [TextArea(5,10)]
    public string fight_description;
    public int cooldown;
    public List<Properties> properties;

    public Sprite artwork;

    public GameObject spellScriptPrefab;

    public virtual void OnUse()
    {
        Debug.Log("Base Obj");
    }
}