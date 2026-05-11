using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 游戏状态管理：逃脱胜利、死亡重启。
/// 通过静态 Instance 访问，场景中需存在一个挂载此脚本的 GameObject。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏设置")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private GameEndMenuUI victoryMenuPrefab;
    [SerializeField] private GameEndMenuUI failureMenuPrefab;
    [SerializeField, Tooltip("Game Over 后延迟重启时间（秒）")]
    private float gameOverDelay = 3f;
    [SerializeField, Tooltip("逃脱成功后延迟返回主菜单时间（秒）")]
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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 玩家死亡时由 PlayerController 调用。
    /// </summary>
    public void OnPlayerDeath()
    {
        if (isGameOver || isEscaped) return;
        isGameOver = true;

        // 停止 BGM，播放 Game Over 音效
        AudioManager.Instance.StopBGM();
        AudioManager.Instance.StopAmbient();
        AudioManager.Instance.Play(SFX.GameOver);

        // 禁用玩家输入
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.Input.DisablePlayerMoveInput();

        if (!TryShowEndMenu(
                failureMenuPrefab,
                "Assets/Prefabs/UI/FailureMenu.prefab",
                () => SceneManager.LoadScene(gameSceneName),
                () => SceneManager.LoadScene(mainMenuSceneName)))
        {
            // 延迟重启
            StartCoroutine(RestartAfterDelay());
        }
    }

    /// <summary>
    /// 集齐钥匙后由 MainDoor 调用，触发逃脱。
    /// </summary>
    public void OnPlayerEscape()
    {
        if (isGameOver || isEscaped) return;
        isEscaped = true;

        // 停止探索 BGM，播放逃脱成功 BGM
        AudioManager.Instance.StopBGM();
        AudioManager.Instance.StopAmbient();
        AudioManager.Instance.PlayBGM(BGM.EscapeSuccess);

        // 禁用玩家输入
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.Input.DisablePlayerMoveInput();

        if (!TryShowEndMenu(
                victoryMenuPrefab,
                "Assets/Prefabs/UI/VictoryMenu.prefab",
                () => SceneManager.LoadScene(gameSceneName),
                () => SceneManager.LoadScene(mainMenuSceneName)))
        {
            // 延迟返回主菜单
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
