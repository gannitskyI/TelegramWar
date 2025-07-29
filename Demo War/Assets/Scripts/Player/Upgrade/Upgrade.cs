
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Upgrade
{
    public string id;
    public string name;
    public string description;
    public UpgradeType type;
    public float value;
    public int maxLevel;
    public int currentLevel;

    public Upgrade(string id, string name, string description, UpgradeType type, float value, int maxLevel = 5)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.type = type;
        this.value = value;
        this.maxLevel = maxLevel;
        this.currentLevel = 0;
    }

    public bool CanUpgrade => currentLevel < maxLevel;

    public float GetCurrentValue => value * (currentLevel + 1);

    public string GetDisplayText()
    {
        string levelText = maxLevel > 1 ? $" (Lv.{currentLevel + 1})" : "";
        return $"{name}{levelText}\n{description}";
    }
}