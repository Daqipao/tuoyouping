using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包内Item物品控制器.
/// </summary>
public class InventoryItemController : MonoBehaviour {

    private Transform _transform;
    public Transform Transform
    {
        get { return _transform; }
    }

    private Image _image;
    public Image Image
    {
        get { return _image; }
    }

    private Text _text;
    public Text Text
    {
        get { return _text; }
    }

    private void Awake()
    {
        _transform = gameObject.GetComponent<Transform>();

        _image = gameObject.GetComponent<Image>();
        _text = _transform.Find("Inventory_Item_Num").GetComponent<Text>();
    }

    /// <summary>
    /// 初始化Item数据.
    /// </summary>
    /// <param name="name">Item名字</param>
    /// <param name="num">Item数量</param>
    public void InitItem(string name, int number)
    {
        _image.sprite = Resources.Load<Sprite>("Inventory/InventoryImage/" + name);
        _text.text = number.ToString();
    }
}
