import os

def patch_player_health():
    path = "Assets/Scripts/Player/PlayerHealth.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    target = """    public void ApplyAugmentBonus(int bonusHp)
    {
        augmentBonusHp = bonusHp;
        RefreshSocketHealthBonus(suppressHealthReset: true);
    }"""

    replacement = """    public void ApplyAugmentBonus(int bonusHp)
    {
        augmentBonusHp = bonusHp;
        RefreshSocketHealthBonus(suppressHealthReset: true);
    }

    /// <summary>
    /// Hy sinh máu thực sự khi chọn Lõi Nâng Cấp (Khế Ước Quỷ Dữ -15%, Bóng Ma Cuồng Nộ -25%).
    /// Áp dụng ngay lập tức cho cả Max HP và Current HP, bất kể ở đầu game hay giữa trận!
    /// Hiển thị popup số đỏ giật máu, chớp đỏ cơ thể, rung màn hình và cập nhật thanh máu.
    /// </summary>
    public void ApplySacrificeHealth(float percent, string augmentName)
    {
        if (isDead) return;

        // Tính lượng máu hy sinh dựa trên maxHealth trước khi trừ (tối thiểu 15 HP để đầu game 110 HP vẫn trừ rõ rệt)
        int sacrificeAmount = Mathf.Max(15, Mathf.RoundToInt(maxHealth * percent));

        // Trừ vào augmentBonusHp để giới hạn Max HP vĩnh viễn trong run
        augmentBonusHp -= sacrificeAmount;
        RefreshSocketHealthBonus(suppressHealthReset: true);

        // Trừ trực tiếp máu hiện tại (giữ lại tối thiểu 1 HP)
        currentHealth = Mathf.Clamp(currentHealth - sacrificeAmount, 1, maxHealth);
        PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
        PlayerPrefs.Save();

        // Hiển thị số trừ máu đỏ thẫm bay lên
        DamagePopup.CreateText(transform.position + Vector3.up * 1.2f, $"-{sacrificeAmount} HP [{augmentName.ToUpper()}]", new Color(1f, 0.15f, 0.15f));

        // Hiệu ứng chớp đỏ thân thể & Rung màn hình
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColorRoutine(new Color(1f, 0.1f, 0.1f, 0.95f)));
        }
        FindAnyObjectByType<CameraFollow>()?.Shake(0.35f, 0.25f);
        SoundManager.EnsureInstance()?.PlayPlayerHurt();

        // Cập nhật thanh máu UI ngay lập tức
        var hBar = FindAnyObjectByType<HealthBarUI>();
        if (hBar != null) hBar.SetHealth(currentHealth, maxHealth);

        Debug.Log($"<color=red>[HEALTH SACRIFICE]</color> Đã hy sinh -{sacrificeAmount} HP do {augmentName}! Sinh lực hiện tại: {currentHealth}/{maxHealth} HP");
    }"""

    if target in content:
        content = content.replace(target, replacement, 1)
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        print("PlayerHealth.cs patched successfully!")
    else:
        print("PlayerHealth.cs target NOT found!")

if __name__ == "__main__":
    patch_player_health()

