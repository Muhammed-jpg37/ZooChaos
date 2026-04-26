using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CategoryManager : MonoBehaviour
{
    public GameObject[] categories;
    public void ShowCategory(int index)
    {
        for (int i = 0; i < categories.Length; i++)
        {
            categories[i].SetActive(i == index);
        }
        DisableOtherCategories(index);
    }
    public void DisableOtherCategories(int index)
    {
        for (int i = 0; i < categories.Length; i++)
        {
            if (i != index)
            {
                categories[i].SetActive(false);
            }
        }
    }
}
