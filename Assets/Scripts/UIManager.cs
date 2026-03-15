using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Gestiona toda la UI del juego: HUD durante partida, pantalla de Game Over,
/// pantalla de inicio, y efectos visuales de UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD - Panel Principal")]
    public GameObject hudPanel;
    public TextMeshProUGUI coinCountText;
    public TextMeshProUGUI scoreText;
    public Image propulsionIndicator;
    public Color propulsionActiveColor = new Color(0.2f, 0.5f, 1f, 1f);
    public Color propulsionInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

    [Header("HUD - Moneda Animación")]
    public RectTransform coinIconRect;        // Ícono de moneda que se sacude al recoger
    public float coinShakeMagnitude = 8f;
    public float coinShakeDuration = 0.3f;

    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public Button playButton;

    [Header("Game Over")]
    public GameObject gameOverScreen;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI newHighScoreLabel;  // "¡Nuevo récord!" (se activa/desactiva)
    public Button restartButton;
    public Button mainMenuButton;
    public CanvasGroup gameOverCanvasGroup;    // Para fade-in

    [Header("Propulsión Cooldown")]
    public TextMeshProUGUI propulsionCooldownText;

    [Header("Advertencia de Caja")]
    public GameObject boxWarningPanel;         // Panel "¡ZONA DE DESTRUCCIÓN!" 
    public TextMeshProUGUI boxWarningText;
    public float warningDisplayTime = 2f;

    [Header("Animación Puntaje")]
    public float scorePopScale = 1.3f;
    public float scorePopDuration = 0.15f;

    private int displayedScore = 0;
    private int targetScore = 0;
    private Coroutine coinShakeCoroutine;
    private Coroutine scorePopCoroutine;
    private Coroutine warningCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Suscribirse a eventos del GameManager
        GameManager.Instance.OnCoinsChanged += UpdateCoinsUI;
        GameManager.Instance.OnScoreChanged += UpdateScoreUI;
        GameManager.Instance.OnGameOver += ShowGameOver;
        GameManager.Instance.OnGameStart += OnGameStart;
        GameManager.Instance.OnPropulsionToggled += UpdatePropulsionUI;

        // Botones
        if (playButton) playButton.onClick.AddListener(OnStartPressed);
        if (restartButton) restartButton.onClick.AddListener(OnRestartPressed);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenuPressed);

        // Estado inicial - solo mostrar menú principal si el juego no está en progreso
        if (!GameManager.Instance.IsPlaying)
        {
            ShowMainMenu();
        }
        else
        {
            OnGameStart(); // Si el juego ya está en progreso, activar HUD
        }
    }

    void Update()
    {
        if (propulsionCooldownText == null) return;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        if (player.PropulsionOnCooldown)
        {
            propulsionCooldownText.text  = $"{player.PropulsionCooldownRemaining:F1}s";
            propulsionCooldownText.color = Color.red;
        }
        else if (GameManager.Instance.IsPropulsionActive)
        {
            propulsionCooldownText.text  = "ACTIVE";
            propulsionCooldownText.color = Color.cyan;
        }
        else
        {
            propulsionCooldownText.text  = "READY";
            propulsionCooldownText.color = Color.green;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged -= UpdateCoinsUI;
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnGameOver -= ShowGameOver;
            GameManager.Instance.OnGameStart -= OnGameStart;
            GameManager.Instance.OnPropulsionToggled -= UpdatePropulsionUI;
        }
    }

    // ────────────────────────────────────────────────
    //  PANTALLAS
    // ────────────────────────────────────────────────

    void ShowMainMenu()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (hudPanel) hudPanel.SetActive(false);
        if (gameOverScreen) gameOverScreen.SetActive(false);
    }

    void OnGameStart()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);
        if (gameOverScreen) gameOverScreen.SetActive(false);
        UpdateCoinsUI(0);
        UpdateScoreUI(0);
        UpdatePropulsionUI(false);
    }

    void ShowGameOver()
    {
        StartCoroutine(ShowGameOverCoroutine());
    }

    IEnumerator ShowGameOverCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (hudPanel) hudPanel.SetActive(false);
        if (gameOverScreen) gameOverScreen.SetActive(true);
        if (finalScoreText) finalScoreText.text = $"Score: {GameManager.Instance.Score}";
        if (highScoreText) highScoreText.text = $"High Score: {GameManager.Instance.HighScore}";

        bool isNewRecord = GameManager.Instance.Score >= GameManager.Instance.HighScore
                           && GameManager.Instance.Score > 0;
        if (newHighScoreLabel) newHighScoreLabel.gameObject.SetActive(isNewRecord);

        // Reproducir sonido de nuevo record si aplica
        if (isNewRecord)
        {
            AudioManager.Instance?.PlayNewHighScore();
        }

        // Fade in
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 2f;
                gameOverCanvasGroup.alpha = Mathf.Clamp01(t);
                yield return null;
            }
        }
    }

    // ────────────────────────────────────────────────
    //  ACTUALIZACIÓN DE HUD
    // ────────────────────────────────────────────────

    void UpdateCoinsUI(int coins)
    {
        if (coinCountText) coinCountText.text = "Coins: " + coins.ToString();
        if (coinShakeCoroutine != null) StopCoroutine(coinShakeCoroutine);
        if (coinIconRect) coinShakeCoroutine = StartCoroutine(ShakeCoinIcon());
    }

    void UpdateScoreUI(int score)
    {
        targetScore = score;
        if (scoreText) scoreText.text = "Score: " + score.ToString("N0");
        if (scorePopCoroutine != null) StopCoroutine(scorePopCoroutine);
        if (scoreText) scorePopCoroutine = StartCoroutine(PopScore());
    }

    void UpdatePropulsionUI(bool active)
    {
        if (propulsionIndicator)
            propulsionIndicator.color = active ? propulsionActiveColor : propulsionInactiveColor;
    }

    // ────────────────────────────────────────────────
    //  ADVERTENCIA ZONA DE DESTRUCCIÓN
    // ────────────────────────────────────────────────

    public void ShowBoxWarning(int totalCoinsRequired)
    {
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(ShowWarningCoroutine(totalCoinsRequired));
    }

    IEnumerator ShowWarningCoroutine(int coinsRequired)
    {
        if (boxWarningPanel) boxWarningPanel.SetActive(true);
        if (boxWarningText)
            boxWarningText.text = $"⚠ ZONA DE DESTRUCCIÓN\n¡Necesitas ~{coinsRequired} monedas!\nActiva PROPULSIÓN (clic derecho / B)";

        yield return new WaitForSeconds(warningDisplayTime);

        if (boxWarningPanel) boxWarningPanel.SetActive(false);
    }

    // ────────────────────────────────────────────────
    //  ANIMACIONES DE UI
    // ────────────────────────────────────────────────

    IEnumerator ShakeCoinIcon()
    {
        Vector3 originalPos = coinIconRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < coinShakeDuration)
        {
            float t = elapsed / coinShakeDuration;
            float shake = Mathf.Sin(t * Mathf.PI * 8f) * coinShakeMagnitude * (1f - t);
            coinIconRect.anchoredPosition = originalPos + new Vector3(shake, shake * 0.5f, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        coinIconRect.anchoredPosition = originalPos;
    }

    IEnumerator PopScore()
    {
        RectTransform rt = scoreText.GetComponent<RectTransform>();
        Vector3 originalScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < scorePopDuration)
        {
            float t = elapsed / scorePopDuration;
            float scale = Mathf.Lerp(scorePopScale, 1f, t);
            rt.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.localScale = originalScale;
    }

    // ────────────────────────────────────────────────
    //  BOTONES
    // ────────────────────────────────────────────────

    void OnStartPressed()
    {
        GameManager.Instance.StartGame();
    }

    void OnRestartPressed()
    {
        GameManager.Instance.RestartGame();
    }

    void OnMainMenuPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
