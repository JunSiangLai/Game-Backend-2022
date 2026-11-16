using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class Inventory : MonoBehaviour
{
    public Dropdown dropDown;
    public Toggle toggle;
    public GameObject itemPrefab;
    public GameObject removeButton;
    public Item itemSelected;
    public GameObject itemContent;
    private GameObject content;

    private void Awake()
    {
        content = gameObject.transform.GetChild(0).gameObject;
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += InventoryUpdate;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += InventoryUpdate;
    }

    public static void ReqUpdate()
    {
        Inventory inventory = GameObject.Find("Inventory").GetComponent<Inventory>();
        byte[] content;
        string message = "";
        if (inventory.toggle.isOn)
            message = "true,";
        else
            message = "false,";
        message += inventory.dropDown.options[inventory.dropDown.value].text;
        content = Encoding.Default.GetBytes(message);
        PhotonNetwork.RaiseEvent((byte)6, (content), RaiseEventOptions.Default, SendOptions.SendReliable);
    }

    private void InventoryUpdate(EventData obj)
    {
        if (obj.Code != 6)
            return;

        // Delete All Children
        foreach (Transform child in content.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        //obj.Parameters.TryGetValue((byte)245, out object json);
        UserArray users = JsonUtility.FromJson<UserArray>("{\"userArray\":" + obj.CustomData + "}");

        foreach (User user in users.userArray)
        {
            Debug.Log(obj.CustomData);
            GameObject item = GameObject.Instantiate(itemPrefab, content.transform);
            item.transform.GetChild(3).GetComponent<Item>().owner = user.username;
            item.transform.GetChild(3).GetComponent<Item>().name = user.item_name;
            item.transform.GetChild(3).GetComponent<Item>().count = user.count;
            item.transform.GetChild(3).GetComponent<Item>().ID = user.ID;
            item.transform.GetChild(3).GetComponent<Item>().inventory = this;
            if (user.is_facebook == "0")
                item.transform.GetChild(3).GetComponent<Item>().facebook = false;
            else
                item.transform.GetChild(3).GetComponent<Item>().facebook = true;
            item.transform.GetChild(3).gameObject.SetActive(true);
        }

        if (itemSelected != null)
            removeButton.SetActive(true);
        else
            removeButton.SetActive(false);
    }

    public void InventoryCustomUpdate(string json)
    {
        UserArray users = JsonUtility.FromJson<UserArray>("{\"userArray\":" + json + "}");
        // Delete All Children
        foreach (Transform child in content.transform)
        {
            if (child.transform.GetChild(3).GetComponent<Item>().ID == users.userArray[0].ID)
            {
                child.transform.GetChild(3).GetComponent<Item>().count = users.userArray[0].count;
                child.transform.GetChild(3).GetComponent<Item>().Reset();
                return;
            }
        }

        GameObject item = GameObject.Instantiate(itemPrefab, content.transform);
        item.transform.GetChild(3).GetComponent<Item>().owner = users.userArray[0].username;
        item.transform.GetChild(3).GetComponent<Item>().name = users.userArray[0].item_name;
        item.transform.GetChild(3).GetComponent<Item>().count = users.userArray[0].count;
        item.transform.GetChild(3).GetComponent<Item>().ID = users.userArray[0].ID;
        item.transform.GetChild(3).GetComponent<Item>().inventory = this;
        if (users.userArray[0].is_facebook == "0")
            item.transform.GetChild(3).GetComponent<Item>().facebook = false;
        else
            item.transform.GetChild(3).GetComponent<Item>().facebook = true;
        item.transform.GetChild(3).gameObject.SetActive(true);
    }
    public void InventoryCustomDelete(string ID)
    {
        foreach (Transform child in content.transform)
        {
            if (child.transform.GetChild(3).GetComponent<Item>().ID == ID)
            {
                if (itemSelected.gameObject.transform == child.transform.GetChild(3))
                {
                    removeButton.SetActive(false);
                    ItemContentReset();
                }
                GameObject.Destroy(child.gameObject);
                break;
            }
        }
    }

    public void ItemContentUpdate()
    {
        itemContent.gameObject.SetActive(true);
        itemContent.transform.GetChild(1).GetComponent<Text>().text = itemSelected.name;
        itemContent.transform.GetChild(2).GetComponent<Text>().text = itemSelected.count.ToString();
        itemContent.transform.GetChild(3).GetComponent<Text>().text = itemSelected.owner;
        if (itemSelected.facebook)
            itemContent.transform.GetChild(4).gameObject.SetActive(true);
        else
            itemContent.transform.GetChild(4).gameObject.SetActive(false);
    }

    public void ItemContentReset()
    {
        itemContent.gameObject.SetActive(false);
    }

    public void ContentReset()
    {
        foreach (Transform child in content.transform)
        {
            child.GetComponent<UnityEngine.UI.Image>().enabled = false;
        }
    }
}

[System.Serializable]
public class User
{
    public string username;

    public string item_name;

    public uint count;

    public string ID;

    public string is_facebook;

}

[System.Serializable]
public class UserArray
{
    public User[] userArray;

}

