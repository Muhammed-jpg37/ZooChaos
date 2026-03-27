using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ObjectsDatabseSO : ScriptableObject
{
    public List<ObjectData> objectsData;


    public ObjectData GetObjectByID(int id)
    {
        foreach (ObjectData obj in objectsData)
        {
            if (obj.ID == id)
            {
                return obj;
            }
        }

        return new(); // This cannot happen
    }

}
public enum BuildTypes
{
    None,
    House,
    Yemekhane,

    Forest,
    Well,
    IronMine,
   
}

[System.Serializable]
public class ObjectData
{
    [field: SerializeField]
    public string Name { get; private set; }

    [field: SerializeField]
    public int ID { get; private set; }

     [field: SerializeField]
    public BuildTypes thisBuildType { get; private set; }

    [field: SerializeField]
    [TextArea(3, 10)]
    public string description;

    [field: SerializeField]
    public Vector2Int Size { get; private set; } = Vector2Int.one;

    [field: SerializeField]
    public GameObject Prefab { get; private set; }

    [field: SerializeField]
    public List<BuildRequirement> resourceRequirements { get; private set; }

      [field: SerializeField]
    public List<BuildTypes> buildDependencies { get; private set; }

    [field: SerializeField]
    public List<BuildBenefits> benefits { get; private set; }

  
}

[System.Serializable]
public class BuildRequirement
{
    public int amount;
}


[System.Serializable]
public class BuildBenefits
{
    public enum BenefitType
    {
        Housing,
        Forest,
        FoodPlace,
        
    }


    public string benefit;
    public Sprite benefitIcon;
    public BenefitType benefitType;
    public int benefitAmount;
}