using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridUpgradeButton : MonoBehaviour
{
    [SerializeField] private GridScript gridScript;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text costText;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (gridScript == null)
        {
            gridScript = FindObjectOfType<GridScript>();
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }

        RefreshCostLabel();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    private void Update()
    {
        RefreshCostLabel();
    }

    private void HandleClicked()
    {
        if (gridScript == null)
        {
            gridScript = FindObjectOfType<GridScript>();
        }

        if (gridScript == null)
        {
            Debug.LogWarning("GridScript instance is missing.");
            return;
        }

        if (!gridScript.TryPurchaseGridUpgrade())
        {
            Debug.Log("Grid upgrade purchase failed.");
        }
    }

    private void RefreshCostLabel()
    {
        if (costText == null)
        {
            return;
        }

        if (gridScript == null)
        {
            costText.text = "Upgrade";
            return;
        }

        costText.text = $"Upgrade ${gridScript.CurrentGridUpgradeCost}";
    }
}
