using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD + result UI. Reads gameplay state only; never drives gameplay.
/// Health: event-driven. Cooldowns (Player + AI): read-only per-frame update of Image.fillAmount.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private DragonHealth playerHealth;
    [SerializeField] private DragonHealth aiHealth;
    [SerializeField] private DragonPlayerCombatController playerCombatController;
    [SerializeField] private DragonAIController aiController;

    [Header("Health Sliders (0..1)")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Slider aiHealthSlider;

    [Header("Player Cooldown Overlays (Filled / Radial 360: 1 = just started, 0 = ready)")]
    [SerializeField] private Image playerFireCooldownImage;
    [SerializeField] private Image playerTailCooldownImage;
    [SerializeField] private Image playerFlyCooldownImage;

    [Header("AI Cooldown Overlays (Filled / Radial 360: 1 = just started, 0 = ready)")]
    [SerializeField] private Image aiFireCooldownImage;
    [SerializeField] private Image aiTailCooldownImage;
    [SerializeField] private Image aiFlyCooldownImage;

    [Header("Player Cooldown Text (optional)")]
    [SerializeField] private TMP_Text fireCooldownText;
    [SerializeField] private TMP_Text tailCooldownText;
    [SerializeField] private TMP_Text flyCooldownText;
    [SerializeField] private string readyLabel = "READY";

    [Header("Winner UI")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Button restartButton;
    [SerializeField] private string playerWinsMessage = "PLAYER WINS";
    [SerializeField] private string aiWinsMessage = "AI WINS";
    [SerializeField] private string drawMessage = "DRAW";

    // Last displayed cooldown text state (avoids per-frame string work).
    // -1 = unset, 0 = READY, >0 = ceil(remaining * 10)
    private int lastFireText = -1;
    private int lastTailText = -1;
    private int lastFlyText = -1;

    private bool healthSubscribed;
    private bool battleSubscribed;

    private void Awake()
    {
        PrepareSlider(playerHealthSlider);
        PrepareSlider(aiHealthSlider);

        PrepareOverlay(playerFireCooldownImage);
        PrepareOverlay(playerTailCooldownImage);
        PrepareOverlay(playerFlyCooldownImage);

        PrepareOverlay(aiFireCooldownImage);
        PrepareOverlay(aiTailCooldownImage);
        PrepareOverlay(aiFlyCooldownImage);

        if (winnerPanel != null)
            winnerPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerHealth != null && aiHealth != null && !healthSubscribed)
        {
            playerHealth.HealthChanged += OnPlayerHealthChanged;
            aiHealth.HealthChanged += OnAIHealthChanged;
            healthSubscribed = true;
        }

        if (battleManager != null && !battleSubscribed)
        {
            battleManager.BattleEnded += OnBattleEnded;
            battleSubscribed = true;
        }

        if (restartButton != null && battleManager != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void Start()
    {
        // Initialize health once at startup (afterwards it is event-driven).
        SetSlider(playerHealthSlider, playerHealth != null ? playerHealth.HealthNormalized : 0f);
        SetSlider(aiHealthSlider, aiHealth != null ? aiHealth.HealthNormalized : 0f);
    }

    private void OnDisable()
    {
        if (healthSubscribed)
        {
            if (playerHealth != null) playerHealth.HealthChanged -= OnPlayerHealthChanged;
            if (aiHealth != null) aiHealth.HealthChanged -= OnAIHealthChanged;
            healthSubscribed = false;
        }

        if (battleSubscribed)
        {
            if (battleManager != null) battleManager.BattleEnded -= OnBattleEnded;
            battleSubscribed = false;
        }

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }

    private void Update()
    {
        if (playerCombatController != null)
        {
            UpdatePlayerCooldown(playerFireCooldownImage, fireCooldownText,
                playerCombatController.FireCooldownNormalized,
                playerCombatController.FireCooldownRemaining, ref lastFireText);

            UpdatePlayerCooldown(playerTailCooldownImage, tailCooldownText,
                playerCombatController.TailCooldownNormalized,
                playerCombatController.TailCooldownRemaining, ref lastTailText);

            UpdatePlayerCooldown(playerFlyCooldownImage, flyCooldownText,
                playerCombatController.FlyCooldownNormalized,
                playerCombatController.FlyCooldownRemaining, ref lastFlyText);
        }

        if (aiController != null)
        {
            SetFill(aiFireCooldownImage, aiController.FireCooldownNormalized);
            SetFill(aiTailCooldownImage, aiController.TailCooldownNormalized);
            SetFill(aiFlyCooldownImage, aiController.FlyCooldownNormalized);
        }
    }

    // ------------------------------------------------------------------
    // Health (event-driven)
    // ------------------------------------------------------------------

    private void OnPlayerHealthChanged(float current, float max)
    {
        SetSlider(playerHealthSlider, playerHealth.HealthNormalized);
    }

    private void OnAIHealthChanged(float current, float max)
    {
        SetSlider(aiHealthSlider, aiHealth.HealthNormalized);
    }

    // ------------------------------------------------------------------
    // Cooldowns (display only)
    // ------------------------------------------------------------------

    private void UpdatePlayerCooldown(Image overlay, TMP_Text text, float normalized, float remaining, ref int lastText)
    {
        SetFill(overlay, normalized);

        if (text == null)
            return;

        int state = remaining <= 0f ? 0 : Mathf.CeilToInt(remaining * 10f);

        if (state == lastText)
            return;

        lastText = state;

        if (state == 0)
            text.SetText(readyLabel);
        else
            text.SetText("{0:1}", remaining); // no string allocation
    }

    // ------------------------------------------------------------------
    // Winner UI / restart
    // ------------------------------------------------------------------

    private void OnBattleEnded(BattleResult result)
    {
        if (winnerText != null)
            winnerText.text = GetMessage(result);

        if (winnerPanel != null)
            winnerPanel.SetActive(true);
    }

    private void OnRestartClicked()
    {
        // No second restart system: just forward to BattleManager.
        if (battleManager != null)
            battleManager.RestartBattle();
    }

    private string GetMessage(BattleResult result)
    {
        switch (result)
        {
            case BattleResult.PlayerWins: return playerWinsMessage;
            case BattleResult.AIWins: return aiWinsMessage;
            case BattleResult.Draw: return drawMessage;
            default: return string.Empty;
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static void PrepareSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false; // display only
    }

    private static void PrepareOverlay(Image overlay)
    {
        if (overlay == null)
            return;

        // fillAmount is ignored unless the Image is set to Filled in the Inspector.
        if (overlay.type != Image.Type.Filled)
            Debug.LogWarning("Cooldown overlay must use Image Type = Filled (Radial 360).", overlay);

        overlay.fillAmount = 0f; // ready
    }

    private static void SetFill(Image overlay, float value)
    {
        if (overlay != null)
            overlay.fillAmount = Mathf.Clamp01(value);
    }

    private static void SetSlider(Slider slider, float value)
    {
        if (slider != null)
            slider.value = Mathf.Clamp01(value);
    }
}