using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

/// <summary>
/// 背包数据控制.
/// </summary>
public class InventoryPanelModel : MonoBehaviour {

    public List<InventoryItem> GetJsonList(string fileName)
    {
        List<InventoryItem> tempList = new List<InventoryItem>();

        //读取Json文件，得到长的Json字符串.
        string jsonStr = Resources.Load<TextAsset>("Inventory/JsonData/" + fileName).text;
        //将长的Json字符串转化为JsonData.
        JsonData jsonData = JsonMapper.ToObject(jsonStr);
        //遍历JsonData并转化得到单独的Json字符串.
        for (int i = 0; i < jsonData.Count; i++)
        {
            //将单独的Json字符串转化为对象.
            InventoryItem ii = JsonMapper.ToObject<InventoryItem>(jsonData[i].ToJson());
            tempList.Add(ii);
        }

        return tempList;
    }
}
