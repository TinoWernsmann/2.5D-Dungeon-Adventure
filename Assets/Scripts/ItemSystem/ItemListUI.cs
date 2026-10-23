using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ItemManager itemManager;
    [SerializeField] private Image _crosshair;

    [Header("Inventory")]
    [SerializeField]
    private Image[] itemIcons;

    [SerializeField]
    private TMP_Text[] itemAmountTexts;

    [Header("Selected Item")]
    [SerializeField]
    private Image selectedItemIcon;

    [Header("Equipped Ammo")]
    [Tooltip("Leer lassen = wird automatisch gesucht.")]
    [SerializeField]
    private PlayerCombat playerCombat;

    [Tooltip("Leuchtender Rand um die ausgerüstete Munition.")]
    [SerializeField]
    private Color equippedAmmoGlow = new(1f, 0.85f, 0.2f, 1f);

    private readonly Color selectedColor =
        Color.white;

    private readonly Color unselectedColor =
        new(
            0.65f,
            0.65f,
            0.65f,
            1f
        );

    private void Awake()
    {
        if (playerCombat == null)
        {
            playerCombat = FindAnyObjectByType<PlayerCombat>();
        }
    }

    private void OnEnable()
    {
        if (playerCombat != null)
        {
            playerCombat.SelectedAmmoChanged += Refresh;
        }

        if (itemManager == null)
        {
            return;
        }

        itemManager.InventoryChanged +=
            Refresh;

        itemManager.SelectedItemChanged +=
            HandleSelectedItemChanged;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (playerCombat != null)
        {
            playerCombat.SelectedAmmoChanged -= Refresh;
        }

        if (itemManager == null)
        {
            return;
        }

        itemManager.InventoryChanged -=
            Refresh;

        itemManager.SelectedItemChanged -=
            HandleSelectedItemChanged;
    }

    private void Refresh()
    {
        if (itemManager == null)
        {
            return;
        }

        RefreshInventorySlots();
        RefreshSelectedItem();
    }

    private void RefreshInventorySlots()
    {
        for (
            int index = 0;
            index < itemIcons.Length;
            index++)
        {
            RefreshInventorySlot(
                index
            );
        }
    }

    private void RefreshInventorySlot(
        int index)
    {
        ItemSO item =
            itemManager.GetItemAt(
                index
            );

        bool hasItem =
            item != null;

        itemIcons[index].sprite =
            hasItem
                ? item.InventoryIcon
                : null;

        // Icons nicht auf die Slot-Größe strecken (z.B. schmale Schlüssel).
        itemIcons[index].preserveAspect = true;

        itemIcons[index]
            .gameObject
            .SetActive(hasItem);

        itemIcons[index].color =
            index ==
            itemManager.SelectedSlot
                ? selectedColor
                : unselectedColor;

        RefreshEquippedGlow(
            index,
            item
        );

        RefreshAmountText(
            index,
            item
        );
    }

    private void RefreshEquippedGlow(
        int index,
        ItemSO item)
    {
        bool isEquippedAmmo =
            item != null &&
            playerCombat != null &&
            item == playerCombat.EquippedAmmo;

        if (!itemIcons[index].TryGetComponent(out Outline glow))
        {
            if (!isEquippedAmmo)
            {
                return;
            }

            glow = itemIcons[index].gameObject.AddComponent<Outline>();
            glow.effectDistance = new Vector2(3f, -3f);
        }

        glow.effectColor = equippedAmmoGlow;
        glow.enabled = isEquippedAmmo;
    }

    private void RefreshAmountText(
        int index,
        ItemSO item)
    {
        if (itemAmountTexts == null ||
            index >=
            itemAmountTexts.Length ||
            itemAmountTexts[index] ==
            null)
        {
            return;
        }

        TMP_Text amountText =
            itemAmountTexts[index];

        if (item == null ||
            !item.Stackable)
        {
            amountText.text =
                string.Empty;

            amountText.gameObject
                .SetActive(false);

            return;
        }

        int amount =
            itemManager.GetItemAmountAt(
                index
            );

        amountText.text =
            $"x{amount}";

        amountText.gameObject
            .SetActive(true);
    }

    private void RefreshSelectedItem()
    {
        SetSelectedItemIcon(
            itemManager.SelectedItem
        );
    }

    private void
        HandleSelectedItemChanged(
            ItemSO item)
    {
        SetSelectedItemIcon(item);
    }

    private void SetSelectedItemIcon(
        ItemSO item)
    {
        if (selectedItemIcon == null)
        {
            return;
        }

        Sprite icon =
            item != null
                ? item.InventoryIcon
                : null;

        selectedItemIcon.sprite =
            icon;

        selectedItemIcon.preserveAspect = true;

        selectedItemIcon
            .gameObject
            .SetActive(
                icon != null
            );

        if (_crosshair != null)
        {
            // Fadenkreuz nur für Fernkampfwaffen (z.B. Armbrust).
            _crosshair.gameObject.SetActive(item is WeaponSO weapon && weapon.UsesProjectile);
        }
    }
}