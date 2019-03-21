using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包界面控制.
/// </summary>
public class InventoryPanelView : MonoBehaviour {

    private Transform _transform;
    public Transform Transform
    {
        get { return _transform; }
    }

    private Transform _gridTransform;
    public Transform GridTransform
    {
        get { return _gridTransform; }
    }

    private GameObject _slotPrefab;
    public GameObject SlotPrefab
    {
        get { return _slotPrefab; }
    }

    private GameObject _itemPrefab;
    public GameObject ItemPrefab
    {
        get { return _itemPrefab; }
    }

    private void Awake()
    {
        _transform = gameObject.GetComponent<Transform>();
        _gridTransform = transform.Find("BG/Grid").GetComponent<Transform>();

        _slotPrefab = Resources.Load<GameObject>("Inventory/Inventory_Slot");
        _itemPrefab = Resources.Load<GameObject>("Inventory/Inventory_Item");
    }
}
