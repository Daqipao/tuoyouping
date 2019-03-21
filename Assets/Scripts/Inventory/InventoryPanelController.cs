using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包模块总控制器.
/// </summary>
public class InventoryPanelController : MonoBehaviour {

    int slotCount = 36;
    List<GameObject> slotList;

    //持有V和M对象.
    private InventoryPanelModel _inventoryPanelModel;
    private InventoryPanelView _inventoryPanelView;

    private void Awake()
    {
        _inventoryPanelModel = gameObject.GetComponent<InventoryPanelModel>();
        _inventoryPanelView = gameObject.GetComponent<InventoryPanelView>();

        slotList = new List<GameObject>();
    }

    /// <summary>
    /// 生成所有物品槽.
    /// </summary>
    void CreateAllSlot()
    {
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = GameObject.Instantiate<GameObject>(_inventoryPanelView.SlotPrefab, _inventoryPanelView.GridTransform);
            slotList.Add(slot);
        }
    }

    /// <summary>
    /// 生成所有物品.
    /// </summary>
    void CreateAllItem()
    {
        List<InventoryItem> itemList = _inventoryPanelModel.GetJsonList("InventoryJsonData");
        for (int i = 0; i < itemList.Count; i++)
        {
            GameObject.Instantiate<GameObject>(_inventoryPanelView.ItemPrefab, slotList[i].GetComponent<Transform>());
        }
    }
}
