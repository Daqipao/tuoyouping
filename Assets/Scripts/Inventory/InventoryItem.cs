using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包物品数据实体类.
/// </summary>
public class InventoryItem {

    private string _name;
    public string Name
    {
        get { return _name; }
    }

    private int _number;
    public int Number
    {
        get { return _number; }
    }

    public InventoryItem() { }
    public InventoryItem(string name, int number)
    {
        _name = name;
        _number = number;
    }
}
