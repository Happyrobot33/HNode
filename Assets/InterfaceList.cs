using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InterfaceList : MonoBehaviour
{
    public string interfaceName; //cant think of a better way to do this right now
    public TMP_Dropdown dropdown;
    public Button addButton;
    private Type @interface;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Startup()
    {
        // Initialize the interface type
        if (string.IsNullOrEmpty(interfaceName))
        {
            Debug.LogError("Interface name is not set.");
            return;
        }

        @interface = Type.GetType(interfaceName);
        if (@interface == null)
        {
            Debug.LogError($"Type '{interfaceName}' could not be found.");
            return;
        }
    }

    public void Initialize<T>(List<T> activeTypes, List<T> allTypes, Action<int> deleteAction, Action<int, int> swapAction, Action<Type> addAction) where T : class
    {
        //check types
        if (typeof(T) != @interface)
        {
            //just exit, this isnt what we want
            return;
        }

        //clear out children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        var possibleUserInterfaces = activeTypes.OfType<IUserInterface<T>>().ToList();

        //make new objects for each
        for (int i = 0; i < possibleUserInterfaces.Count; i++)
        {
            //create a new gameobject
            GameObject newObject = new GameObject(possibleUserInterfaces[i].GetType().Name);
            newObject.transform.SetParent(transform, false);

            //get the rect transform
            RectTransform rectTransform = newObject.AddComponent<RectTransform>();

            //add image
            newObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1);

            //setup vertical layout
            var layoutGroup = newObject.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childScaleHeight = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            //set padding
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            //margin
            layoutGroup.spacing = 5f;

            //recalculate children
            layoutGroup.CalculateLayoutInputVertical();

            //add text
            var tex = Util.AddText(rectTransform, possibleUserInterfaces[i].GetType().Name);
            tex.rectTransform.SetHeight(50f);

            //construct the user interface
            possibleUserInterfaces[i].ConstructUserInterface(rectTransform);

            //needed because I gets hoisted once, this makes a hoist per iteration
            int copy = i;

            //setup a horizontal layout group
            GameObject layoutObject = new GameObject("HorizontalLayout");
            RectTransform layoutRect = layoutObject.AddComponent<RectTransform>();
            layoutObject.transform.SetParent(rectTransform, false);
            layoutObject.AddComponent<HorizontalLayoutGroup>();
            layoutRect.SetHeight(40f);

            //add a button
            var delbut = Util.AddButton(layoutRect, "Delete");
            delbut.onClick.AddListener(() =>
            {
                deleteAction?.Invoke(copy);
            });
            //center the text in it
            delbut.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var upbut = Util.AddButton(layoutRect, "Move Up");
            upbut.onClick.AddListener(() =>
            {
                if (copy > 0)
                {
                    swapAction?.Invoke(copy, copy - 1);
                }
            });
            upbut.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var downbut = Util.AddButton(layoutRect, "Move Down");
            downbut.onClick.AddListener(() =>
            {
                if (copy < possibleUserInterfaces.Count - 1)
                {
                    swapAction?.Invoke(copy, copy + 1);
                }
            });
            downbut.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        //clear dropdown
        dropdown.ClearOptions();

        //add options to dropdown
        foreach (var type in allTypes)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(type.GetType().Name));
        }

        dropdown.RefreshShownValue();

        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(() =>
        {
            Debug.Log($"Adding new interface of type: {@interface.Name}");
            //get the selected index
            int index = dropdown.value;

            //check if the index is valid
            if (index < 0 || index >= allTypes.Count)
            {
                Debug.LogError("Selected index is out of range.");
                return;
            }

            //call the add action with the type
            addAction?.Invoke(allTypes[index].GetType());
        });
    }

    public void UpdateInterface<T>(IEnumerable<IUserInterface<T>> possibleUserInterfaces) where T : class
    {
        //check types
        if (typeof(T) != @interface)
        {
            //just exit, this isnt what we want
            return;
        }

        //update each user interface
        foreach (var userInterface in possibleUserInterfaces)
        {
            userInterface.UpdateUserInterface();
        }
    }
}
