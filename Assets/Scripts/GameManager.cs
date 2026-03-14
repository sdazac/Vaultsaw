using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool IsPlaying { get; private set; } = false;
    public bool IsPropulsionActive { get; private set; } = false;

    [Header("Score")]
    public int Score { get; private set; } = 0;
    public int HighScore { get; private set; } = 0;
    public int Coins { get; private set; } = 0;

    // Events
    public System.Action<int> OnCoinsChanged;
    public System.Action<int> OnScoreChanged;
    public System.Action OnGameOver;
    public System.Action OnGameStart;
    public System.Action<bool> OnPropulsionToggled;

    private const string HIGH_SCORE_KEY = "HighScore";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public void StartGame()
    {
        IsPlaying = true;
        Score = 0;
        Coins = 0;
        IsPropulsionActive = false;
        OnGameStart?.Invoke();
        OnScoreChanged?.Invoke(Score);
        OnCoinsChanged?.Invoke(Coins);
    }

    public void TriggerGameOver()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, HighScore);
            PlayerPrefs.Save();
        }
        OnGameOver?.Invoke();
    }

    public void AddCoin(int amount = 1)
    {
        Coins += amount;
        Score += amount * 10;
        OnCoinsChanged?.Invoke(Coins);
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>Gasta monedas al golpear una caja en modo propulsión. Retorna true si se pudo gastar.</summary>
    public bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void SetPropulsion(bool active)
    {
        IsPropulsionActive = active;
        OnPropulsionToggled?.Invoke(active);
    }

    public void AddDistanceScore(float delta)
    {
        int points = Mathf.FloorToInt(delta * 0.1f);
        if (points > 0)
        {
            Score += points;
            OnScoreChanged?.Invoke(Score);
        }
    }

    public void RestartGame()
    {
        StartGame(); // Resetear estado del juego
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
