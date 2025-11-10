using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class RightClickOption : MonoBehaviour, IPointerClickHandler
{
    public GameObject optionPrefab;
    public GameObject optionMenuPrefab;
    public List<string> options;
    public List<UnityEvent> optionEvents;

    public static GameObject currentOptionMenu;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {

            if (currentOptionMenu != null)
                Destroy(currentOptionMenu);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0;
            GameObject optionMenu = Instantiate(optionMenuPrefab, worldPos, Quaternion.identity, transform.root);
            currentOptionMenu = optionMenu;
            for (int i = 0; i < options.Count; i++)
            {
                GameObject option = Instantiate(optionPrefab, optionMenu.transform);
                option.GetComponentInChildren<TMP_Text>().text = options[i];
                int index = i; // Capture index for the closure
                option.GetComponent<Button>().onClick.AddListener(() =>
                {
                    optionEvents[index]?.Invoke();
                    Destroy(optionMenu);
                });
            }
        }
        else
        {
            // Left click
            if (currentOptionMenu != null)
                Destroy(currentOptionMenu);
        }
    }

    public void AddFriendFromRank()
    {
        RankRow row = GetComponent<RankRow>();
        _ = GameManager.Instance.Client.SendFriendRequest(row.profileId);
    }

    public void Block()
    {

    }

    public void Info()
    {

    }

    private void OnDisable()
    {
        Destroy(currentOptionMenu);
    }
}
