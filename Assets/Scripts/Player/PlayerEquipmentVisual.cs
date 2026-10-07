// ========================================================
// VAELSTRIDER: RUINBOUND - In-Game Player Equipment Visual Layer
// Dynamically renders equipped multi-slot armor, helmets, cloaks,
// pants, boots, and gloves directly onto the player character in game.
// Synchronizes with 100% precision across every animation frame:
// IDLE, RUN, ATTACK1, ATTACK2, DASH, ROLL, and DEATH without any
// awkward static floating boxes.
// ========================================================
using UnityEngine;

public class PlayerEquipmentVisual : MonoBehaviour
{
    private SpriteRenderer baseSr;
    private Material equipmentMat;

    private int lastEquippedHelmetId = -1;
    private int lastEquippedArmorId = -1;
    private int lastEquippedPantsId = -1;
    private int lastEquippedGlovesId = -1;
    private int lastEquippedBootsId = -1;
    private int lastEquippedCloakId = -1;

    public static PlayerEquipmentVisual EnsureExists(GameObject playerObj)
    {
        if (playerObj == null) return null;
        var visual = playerObj.GetComponent<PlayerEquipmentVisual>();
        if (visual == null)
        {
            visual = playerObj.AddComponent<PlayerEquipmentVisual>();
        }
        return visual;
    }

    private void Awake()
    {
        baseSr = GetComponent<SpriteRenderer>();
        CleanupOldStaticLayers();
        InitEquipmentMaterial();
        RefreshAllEquipmentVisuals();
    }

    private void Start()
    {
        CleanupOldStaticLayers();
        RefreshAllEquipmentVisuals();
    }

    private void OnDestroy()
    {
        if (equipmentMat != null)
        {
            Destroy(equipmentMat);
        }
    }

    private void CleanupOldStaticLayers()
    {
        string[] oldLayerNames = new string[] {
            "Visual_Cloak", "Visual_Armor", "Visual_Pants", "Visual_Boots", "Visual_Gloves", "Visual_Helmet"
        };
        foreach (var name in oldLayerNames)
        {
            Transform t = transform.Find(name);
            if (t != null) Destroy(t.gameObject);
        }
    }

    private void InitEquipmentMaterial()
    {
        if (baseSr == null) baseSr = GetComponent<SpriteRenderer>();
        if (baseSr == null) return;

        string curChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        if (curChar != CharacterManager.CHAR_VAELSTRIDER)
        {
            baseSr.material = new Material(Shader.Find("Sprites/Default"));
            return;
        }

        Shader shader = Shader.Find("Custom/CharacterEquipmentShader");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            equipmentMat = new Material(shader);
            baseSr.material = equipmentMat;
        }
    }

    public void RefreshAllEquipmentVisuals()
    {
        string curChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        HeroCostumeManager.ApplyCostumeVisual(gameObject, curChar);

        if (curChar != CharacterManager.CHAR_VAELSTRIDER)
        {
            if (baseSr == null) baseSr = GetComponent<SpriteRenderer>();
            if (baseSr != null && (baseSr.material == null || baseSr.material.name.Contains("Custom")))
            {
                baseSr.material = new Material(Shader.Find("Sprites/Default"));
            }
            return;
        }

        if (equipmentMat == null) InitEquipmentMaterial();
        if (equipmentMat == null) return;

        ArmorShopUI.InitDatabase();

        int helmetId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Helmet);
        int armorId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Armor);
        int pantsId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Pants);
        int glovesId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Gloves);
        int bootsId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Boots);
        int cloakId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Cloak);

        lastEquippedHelmetId = helmetId;
        lastEquippedArmorId = armorId;
        lastEquippedPantsId = pantsId;
        lastEquippedGlovesId = glovesId;
        lastEquippedBootsId = bootsId;
        lastEquippedCloakId = cloakId;

        // 1. Armor / Outer Tunic Colors
        Color armorPri = GetItemPrimaryColor(armorId > 0 ? armorId : 1, new Color(0.77f, 0.14f, 0.19f, 1f));
        Color armorSec = GetItemSecondaryColor(armorId > 0 ? armorId : 1, new Color(0.54f, 0.12f, 0.17f, 1f));
        equipmentMat.SetColor("_ArmorColor", armorPri);
        equipmentMat.SetColor("_ArmorSecColor", armorSec);

        // 2. Cloak / Scarf Color
        if (cloakId > 0)
        {
            Color cloakColor = GetItemPrimaryColor(cloakId, new Color(0.85f, 0.20f, 0.20f, 1f));
            equipmentMat.SetColor("_CloakColor", cloakColor);
        }
        else
        {
            equipmentMat.SetColor("_CloakColor", new Color(0.85f, 0.20f, 0.20f, 1f));
        }

        // 3. Pants / Undergarment Color
        if (pantsId > 0)
        {
            Color pantsColor = GetItemPrimaryColor(pantsId, new Color(0.24f, 0.24f, 0.24f, 1f));
            equipmentMat.SetColor("_PantsColor", pantsColor);
        }
        else
        {
            equipmentMat.SetColor("_PantsColor", new Color(0.24f, 0.24f, 0.24f, 1f));
        }

        // 4. Gloves Color
        if (glovesId > 0)
        {
            Color glovesColor = GetItemPrimaryColor(glovesId, new Color(0.85f, 0.85f, 0.85f, 1f));
            equipmentMat.SetColor("_GlovesColor", glovesColor);
        }
        else
        {
            equipmentMat.SetColor("_GlovesColor", new Color(0.85f, 0.85f, 0.85f, 1f));
        }

        // 5. Boots Color
        if (bootsId > 0)
        {
            Color bootsColor = GetItemPrimaryColor(bootsId, new Color(0.36f, 0.17f, 0.15f, 1f));
            equipmentMat.SetColor("_BootsColor", bootsColor);
        }
        else
        {
            equipmentMat.SetColor("_BootsColor", new Color(0.36f, 0.17f, 0.15f, 1f));
        }

        // 6. Helmet / Headgear Tint Color
        if (helmetId > 0)
        {
            Color helmColor = GetItemPrimaryColor(helmetId, new Color(0.54f, 0.28f, 0.21f, 1f));
            equipmentMat.SetColor("_HelmetColor", helmColor);
        }
        else
        {
            equipmentMat.SetColor("_HelmetColor", new Color(0.54f, 0.28f, 0.21f, 1f));
        }

        // 7. Highest Tier Enchantment Aura Glow
        ArmorShopUI.ArmorTier maxTier = GetHighestEquippedTier(helmetId, armorId, pantsId, glovesId, bootsId, cloakId);
        ApplyTierAuraGlow(maxTier);
    }

    private void ApplyTierAuraGlow(ArmorShopUI.ArmorTier tier)
    {
        if (equipmentMat == null) return;

        switch (tier)
        {
            case ArmorShopUI.ArmorTier.Mythic:
                equipmentMat.SetColor("_TierGlowColor", new Color(1f, 0.2f, 0.35f, 1f));
                equipmentMat.SetFloat("_TierGlowIntensity", 0.45f);
                break;
            case ArmorShopUI.ArmorTier.Legendary:
                equipmentMat.SetColor("_TierGlowColor", new Color(1f, 0.85f, 0.25f, 1f));
                equipmentMat.SetFloat("_TierGlowIntensity", 0.35f);
                break;
            case ArmorShopUI.ArmorTier.Epic:
                equipmentMat.SetColor("_TierGlowColor", new Color(0.75f, 0.35f, 1f, 1f));
                equipmentMat.SetFloat("_TierGlowIntensity", 0.25f);
                break;
            case ArmorShopUI.ArmorTier.Rare:
                equipmentMat.SetColor("_TierGlowColor", new Color(0.2f, 0.7f, 1f, 1f));
                equipmentMat.SetFloat("_TierGlowIntensity", 0.15f);
                break;
            default:
                equipmentMat.SetColor("_TierGlowColor", Color.black);
                equipmentMat.SetFloat("_TierGlowIntensity", 0f);
                break;
        }
    }

    private ArmorShopUI.ArmorTier GetHighestEquippedTier(params int[] itemIds)
    {
        ArmorShopUI.ArmorTier maxTier = ArmorShopUI.ArmorTier.Common;
        foreach (int id in itemIds)
        {
            if (id <= 0) continue;
            ArmorShopUI.ArmorTier t = ArmorShopUI.ArmorTier.Common;
            if (id >= 51) t = ArmorShopUI.ArmorTier.Mythic;
            else if (id >= 41) t = ArmorShopUI.ArmorTier.Legendary;
            else if (id >= 27) t = ArmorShopUI.ArmorTier.Epic;
            else if (id >= 13) t = ArmorShopUI.ArmorTier.Rare;

            if (t > maxTier) maxTier = t;
        }
        return maxTier;
    }

    private void Update()
    {
        CheckEquipmentChange();
    }

    private void CheckEquipmentChange()
    {
        if (ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Helmet) != lastEquippedHelmetId ||
            ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Armor) != lastEquippedArmorId ||
            ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Pants) != lastEquippedPantsId ||
            ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Gloves) != lastEquippedGlovesId ||
            ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Boots) != lastEquippedBootsId ||
            ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Cloak) != lastEquippedCloakId)
        {
            RefreshAllEquipmentVisuals();
        }
    }

    public static Color GetItemPrimaryColor(int id, Color fallback)
    {
        switch (id)
        {
            case 1: return new Color(0.92f, 0.78f, 0.62f); // Ao Len Bac Au
            case 2: return new Color(0.90f, 0.65f, 0.55f); // Giay Len Do
            case 3: return new Color(0.85f, 0.55f, 0.45f); // Tui Deo Cheo
            case 4: return new Color(0.82f, 0.75f, 0.45f); // Giay Len Vang Be
            case 5: return new Color(0.85f, 0.25f, 0.22f); // Giay Len Do Noen
            case 6: return new Color(0.70f, 0.55f, 0.40f); // Bit Tai Mua Dong
            case 7: return new Color(0.25f, 0.50f, 0.85f); // Ao Len Kim Cuong Xanh
            case 8: return new Color(0.95f, 0.58f, 0.25f); // Giay Len Cam Soc
            case 9: return new Color(0.75f, 0.48f, 0.30f); // Khan Choang Caro
            case 10: return new Color(0.85f, 0.35f, 0.25f); // Ao Len Do
            case 11: return new Color(0.92f, 0.72f, 0.48f); // Mu Len Kem
            case 12: return new Color(0.85f, 0.30f, 0.25f); // Gang Tay Do
            case 13: return new Color(0.82f, 0.32f, 0.25f); // Mu Len Phi Cong Do
            case 14: return new Color(0.92f, 0.75f, 0.55f); // Ao Len Kem
            case 15: return new Color(0.92f, 0.75f, 0.55f);
            case 16: return new Color(0.78f, 0.50f, 0.32f);
            case 17: return new Color(0.75f, 0.45f, 0.28f);
            case 18: return new Color(0.90f, 0.75f, 0.55f);
            case 19: return new Color(0.95f, 0.45f, 0.15f); // Khan Choang Cam
            case 20: return new Color(0.72f, 0.52f, 0.38f); // Ao Len Soc Be Nau
            case 21: return new Color(0.90f, 0.45f, 0.35f);
            case 22: return new Color(0.88f, 0.32f, 0.28f); // Mu Len Do Dat
            case 23: return new Color(0.85f, 0.78f, 0.45f);
            case 24: return new Color(0.95f, 0.50f, 0.22f);
            case 25: return new Color(0.35f, 0.60f, 0.88f); // Ao Xanh Duong
            case 26: return new Color(0.95f, 0.72f, 0.45f);
            case 27: return new Color(0.48f, 0.62f, 0.25f); // Mu Len Xanh Reu
            case 28: return new Color(0.85f, 0.55f, 0.35f);
            case 29: return new Color(0.82f, 0.35f, 0.28f);
            case 30: return new Color(0.82f, 0.45f, 0.32f);
            case 31: return new Color(0.95f, 0.90f, 0.78f); // Giay Trinh Tuyet
            case 32: return new Color(0.35f, 0.38f, 0.45f); // Ao Den Than
            case 33: return new Color(0.92f, 0.70f, 0.42f);
            case 34: return new Color(0.65f, 0.68f, 0.72f); // Ao Xam
            case 35: return new Color(0.78f, 0.18f, 0.22f); // Gang Tay Do Tham
            case 36: return new Color(0.95f, 0.48f, 0.18f); // Ao Khoac Cam
            case 37: return new Color(0.55f, 0.65f, 0.32f); // Ao Xanh La
            case 38: return new Color(0.55f, 0.58f, 0.65f);
            case 39: return new Color(0.65f, 0.35f, 0.82f); // Khan Choang Tim
            case 40: return new Color(0.90f, 0.75f, 0.55f);
            case 41: return new Color(0.88f, 0.55f, 0.32f);
            case 42: return new Color(0.85f, 0.28f, 0.25f); // Mu Do
            case 43: return new Color(0.30f, 0.52f, 0.82f); // Ao Xanh
            case 44: return new Color(0.28f, 0.48f, 0.78f); // Quan Jeans Xanh
            case 45: return new Color(0.58f, 0.60f, 0.65f); // Quan Xam
            case 46: return new Color(0.92f, 0.80f, 0.65f);
            case 47: return new Color(0.55f, 0.22f, 0.15f);
            case 48: return new Color(0.35f, 0.50f, 0.68f);
            case 49: return new Color(0.95f, 0.82f, 0.62f);
            case 50: return new Color(0.98f, 0.75f, 0.25f); // Ao Vang Hoang Gia
            case 51: return new Color(0.72f, 0.42f, 0.88f); // Ao Tim Huyen Bi
            case 52: return new Color(0.98f, 0.55f, 0.22f); // Gang Tay Lua
            case 53: return new Color(0.72f, 0.75f, 0.78f);
            case 54: return new Color(0.62f, 0.72f, 0.35f);
            case 55: return new Color(0.98f, 0.85f, 0.68f); // Ao Than Thanh
            case 56: return new Color(0.68f, 0.42f, 0.28f); // Quan Nau
            default: return fallback;
        }
    }

    public static Color GetItemSecondaryColor(int id, Color fallback)
    {
        switch (id)
        {
            case 1: return new Color(0.63f, 0.13f, 0.13f);
            case 2: return new Color(0.75f, 0.25f, 0.25f);
            case 3: return new Color(0.75f, 0.25f, 0.25f);
            case 4: return new Color(0.94f, 0.88f, 0.69f);
            case 5: return new Color(0.56f, 0.06f, 0.13f);
            case 6: return new Color(0.38f, 0.19f, 0.13f);
            case 7: return new Color(0.13f, 0.31f, 0.50f);
            case 8: return new Color(0.94f, 0.82f, 0.63f);
            case 9: return new Color(0.82f, 0.69f, 0.50f);
            case 10: return new Color(0.94f, 0.82f, 0.63f);
            case 11: return new Color(0.94f, 0.82f, 0.63f);
            case 12: return new Color(0.69f, 0.25f, 0.19f);
            case 13: return new Color(0.94f, 0.82f, 0.63f);
            case 14: return new Color(0.94f, 0.82f, 0.56f);
            case 15: return new Color(0.94f, 0.82f, 0.56f);
            case 16: return new Color(0.63f, 0.19f, 0.13f);
            case 17: return new Color(0.44f, 0.13f, 0.06f);
            case 18: return new Color(0.56f, 0.38f, 0.25f);
            case 19: return new Color(0.56f, 0.19f, 0.06f);
            case 20: return new Color(0.44f, 0.31f, 0.19f);
            case 21: return new Color(0.44f, 0.13f, 0.06f);
            case 22: return new Color(0.82f, 0.63f, 0.31f);
            case 23: return new Color(0.38f, 0.50f, 0.13f);
            case 24: return new Color(0.63f, 0.19f, 0.13f);
            case 25: return new Color(0.50f, 0.69f, 0.82f);
            case 26: return new Color(0.88f, 0.82f, 0.63f);
            case 27: return new Color(0.25f, 0.31f, 0.13f);
            case 28: return new Color(0.94f, 0.82f, 0.63f);
            case 29: return new Color(0.44f, 0.13f, 0.13f);
            case 30: return new Color(0.69f, 0.19f, 0.19f);
            case 31: return new Color(0.88f, 0.82f, 0.63f);
            case 32: return new Color(0.25f, 0.38f, 0.50f);
            case 33: return new Color(0.63f, 0.44f, 0.25f);
            case 34: return new Color(0.50f, 0.50f, 0.50f);
            case 35: return new Color(0.75f, 0.19f, 0.19f);
            case 36: return new Color(0.88f, 0.50f, 0.19f);
            case 37: return new Color(0.82f, 0.82f, 0.56f);
            case 38: return new Color(0.56f, 0.56f, 0.56f);
            case 39: return new Color(0.31f, 0.13f, 0.44f);
            case 40: return new Color(0.63f, 0.44f, 0.25f);
            case 41: return new Color(0.25f, 0.06f, 0.06f);
            case 42: return new Color(0.50f, 0.13f, 0.13f);
            case 43: return new Color(0.06f, 0.19f, 0.38f);
            case 44: return new Color(0.19f, 0.31f, 0.50f);
            case 45: return new Color(0.44f, 0.44f, 0.50f);
            case 46: return new Color(0.94f, 0.88f, 0.75f);
            case 47: return new Color(0.25f, 0.19f, 0.06f);
            case 48: return new Color(0.19f, 0.25f, 0.31f);
            case 49: return new Color(0.82f, 0.63f, 0.44f);
            case 50: return new Color(0.82f, 0.56f, 0.13f);
            case 51: return new Color(0.31f, 0.13f, 0.44f);
            case 52: return new Color(0.88f, 0.50f, 0.19f);
            case 53: return new Color(0.38f, 0.38f, 0.44f);
            case 54: return new Color(0.94f, 0.82f, 0.63f);
            case 55: return new Color(0.82f, 0.69f, 0.50f);
            case 56: return new Color(0.63f, 0.44f, 0.25f);
            default: return fallback;
        }
    }
}
