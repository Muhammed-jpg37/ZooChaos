using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuySystemManager : MonoBehaviour
{
    public static BuySystemManager instance { get; private set; }

    [System.Serializable]
    public class BuildingEntry
    {
        public BuildingType type;
        public GameObject prefab;
        public int width = 1;
        public int depth = 1;
        public int cost = 0;
    }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }

        CacheBuildingLookup();
    }

    [Header("Building Catalog")]
    [SerializeField] private List<BuildingEntry> buildingEntries = new List<BuildingEntry>();

    public BuildingType selectedBuilding { get; private set; } = BuildingType.None;
    public enum BuildingType { None, Road, Cage1, Cage2, Cage3, FoodStall, WaterFountain }

    private readonly Dictionary<BuildingType, BuildingEntry> buildingLookup = new Dictionary<BuildingType, BuildingEntry>();

    public void SelectBuilding(int buildingIndex) {
        if (!System.Enum.IsDefined(typeof(BuildingType), buildingIndex)) {
            Debug.LogWarning("Invalid building index selected: " + buildingIndex);
            selectedBuilding = BuildingType.None;
            return;
        }

        selectedBuilding = (BuildingType)buildingIndex;

        if (selectedBuilding != BuildingType.None && !buildingLookup.ContainsKey(selectedBuilding)) {
            Debug.LogWarning("No building data configured for type: " + selectedBuilding);
            selectedBuilding = BuildingType.None;
            return;
        }

        Debug.Log("Selected Building: " + selectedBuilding);

        if (BuildConstruction.instance != null) {
            BuildConstruction.instance.GetBuildingType(buildingIndex);
        }
    }

    public bool TryGetBuildingData(BuildingType type, out GameObject prefab, out int width, out int depth)
    {
        prefab = null;
        width = 0;
        depth = 0;

        if (!buildingLookup.TryGetValue(type, out BuildingEntry entry) || entry.prefab == null) {
            return false;
        }

        prefab = entry.prefab;
        width = Mathf.Max(1, entry.width);
        depth = Mathf.Max(1, entry.depth);
        return true;
    }

    public bool TryGetBuildingCost(BuildingType type, out int cost)
    {
        cost = 0;

        if (!buildingLookup.TryGetValue(type, out BuildingEntry entry) || entry == null)
        {
            return false;
        }

        cost = Mathf.Max(0, entry.cost);
        return true;
    }

    private void CacheBuildingLookup()
    {
        buildingLookup.Clear();

        for (int i = 0; i < buildingEntries.Count; i++) {
            BuildingEntry entry = buildingEntries[i];
            if (entry == null || entry.type == BuildingType.None) {
                continue;
            }

            buildingLookup[entry.type] = entry;
        }
    }
}
