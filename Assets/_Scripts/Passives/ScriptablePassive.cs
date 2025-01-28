using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Scriptable Passive", menuName = "ScriptablePassive")]
public class ScriptablePassive : ScriptableObject
{
    public string passive_name;
    public string fight_description;
    public Sprite artwork;
    public Passive passivePrefab;
    public bool lootable;
}
