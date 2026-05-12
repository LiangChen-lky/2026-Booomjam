using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game state, including opening text, escape victory, death, and scene transitions.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private GameEndMenuUI victoryMenuPrefab;
    [SerializeField] private GameEndMenuUI failureMenuPrefab;

    [Header("Black Screen Text")]
    [SerializeField] private bool showOpeningText = true;
    [SerializeField, TextArea(3, 8)] private string openingText = "";
    [SerializeField, TextArea(3, 8)] private string victoryText = "";
    [SerializeField, TextArea(3, 8)] private string failureText = "";
    [SerializeField] private string continueHint = "按任意键继续";

    [SerializeField, Tooltip("Delay before restarting after game over if no end menu can be shown.")]
    private float gameOverDelay = 3f;
    [SerializeField, Tooltip("Delay before returning to the main menu after escape if no end menu can be shown.")]
    private float escapeDelay = 5f;

    private bool isGameOver = false;
    private bool isEscaped = false;
    private GameEndMenuUI activeEndMenu;

    public bool IsGameOver => isGameOver;
    public bool IsEscaped => isEscaped;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (showOpeningText)
        {
            ShowOpeningText();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OnPlayerDeath()
    {
        if (isGameOver || isEscaped)
        {
            return;
        }

        isGameOver = true;

        AudioManager.Instance.StopBGM();
        AudioManager.Instance.StopAmbient();
        AudioManager.Instance.Play(SFX.GameOver);

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.Input.DisablePlayerMoveInput();
        }

        ShowEndingText(failureText, ShowFailureMenuOrRestart);
    }

    public void OnPlayerEscape()
    {
        if (isGameOver || isEscaped)
        {
            return;
        }

        isEscaped = true;

        AudioManager.Instance.StopBGM();
        AudioManager.Instance.StopAmbient();
        AudioManager.Instance.PlayBGM(BGM.EscapeSuccess);

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.Input.DisablePlayerMoveInput();
        }

        ShowEndingText(victoryText, ShowVictoryMenuOrReturn);
    }

    private void ShowOpeningText()
    {
        Time.timeScale = 0f;

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.Input.DisablePlayerMoveInput();
        }

        BlackScreenTextUI.Show(openingText, continueHint, () =>
        {
            Time.timeScale = 1f;

            var currentPlayer = FindObjectOfType<PlayerController>();
            if (currentPlayer != null && !isGameOver && !isEscaped)
            {
                currentPlayer.Input.EnablePlayerMoveInput();
            }
        });
    }

    private void ShowEndingText(string text, Action onContinue)
    {
        Time.timeScale = 0f;
        BlackScreenTextUI.Show(text, continueHint, onContinue);
    }

    private void ShowFailureMenuOrRestart()
    {
        if (!TryShowEndMenu(
                failureMenuPrefab,
                "Assets/Prefabs/UI/FailureMenu.prefab",
                () => SceneManager.LoadScene(gameSceneName),
                () => SceneManager.LoadScene(mainMenuSceneName)))
        {
            Time.timeScale = 1f;
            StartCoroutine(RestartAfterDelay());
        }
    }

    private void ShowVictoryMenuOrReturn()
    {
        if (!TryShowEndMenu(
                victoryMenuPrefab,
                "Assets/Prefabs/UI/VictoryMenu.prefab",
                () => SceneManager.LoadScene(gameSceneName),
                () => SceneManager.LoadScene(mainMenuSceneName)))
        {
            Time.timeScale = 1f;
            StartCoroutine(ReturnToMenuAfterDelay());
        }
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSeconds(escapeDelay);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private bool TryShowEndMenu(
        GameEndMenuUI menuPrefab,
        string prefabPath,
        UnityEngine.Events.UnityAction onRetry,
        UnityEngine.Events.UnityAction onMainMenu)
    {
        if (activeEndMenu != null)
        {
            return true;
        }

        if (menuPrefab == null)
        {
#if UNITY_EDITOR
            menuPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameEndMenuUI>(prefabPath);
#endif
        }

        if (menuPrefab != null)
        {
            activeEndMenu = Instantiate(menuPrefab.gameObject).GetComponent<GameEndMenuUI>();
        }
        else
        {
            var menuObject = new GameObject(prefabPath.Contains("Victory") ? "[VictoryMenu]" : "[FailureMenu]");
            activeEndMenu = menuObject.AddComponent<GameEndMenuUI>();
        }

        if (activeEndMenu == null)
        {
            return false;
        }

        activeEndMenu.Configure(onRetry, onMainMenu);
        activeEndMenu.Show();
        return true;
    }
}
