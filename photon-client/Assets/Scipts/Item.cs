using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public string owner { get; set; }
    public string name { get; set; }
    public uint count { get; set; }
    public string ID { get; set; }
    public bool facebook { get; set; }

    public Inventory inventory;

    private void OnEnable()
    {
        transform.parent.GetChild(1).GetComponent<Text>().text = name;
        transform.parent.GetChild(2).GetComponent<Text>().text = count.ToString();
    }

    public void Reset()
    {
        transform.parent.GetChild(1).GetComponent<Text>().text = name;
        transform.parent.GetChild(2).GetComponent<Text>().text = count.ToString();
    }

    public void ItemSelected()
    {
        inventory.ContentReset();
        inventory.itemSelected = this;

        inventory.removeButton.SetActive(true);
        inventory.ItemContentUpdate();
    }
}
