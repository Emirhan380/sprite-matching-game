using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public Image topImage;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI finalText;

    [Header("Buttons (exactly 9)")]
    public Button[] buttons; 

    [Tooltip("Optional. Leave empty to auto-detect the visible image for each button.")]
    public Image[] buttonImages; 

    [Header("End UI")]
    public Button tryAgainButton;

    [Header("Game Rules")]
    public float timePerRound = 30f;
    public float resultHideSeconds = 1.2f;
    public int totalRounds = 12;
    public int winAtLeast = 8;

    [Header("Sprites (need >= 9 UNIQUE)")]
    public Sprite[] sprites;

    [Header("Sound")]
    public AudioSource sfxSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    public AudioClip winSfx;
    public AudioClip loseSfx;

    [Header("Pause")]
    public GameObject pausePanel;   
    public Button pauseButton;      
    public Button resumeButton;     
    private bool isPaused = false;

    [Header("Highscore")]
    public TextMeshProUGUI highscoreText;   
    private int highscore = 0;
    private const string HS_KEY = "HIGHSCORE";




    
    private int score;
    private int roundsPlayed;         
    private float timeLeft;
    private bool gameEnded;
    private bool roundLocked;

    
    private readonly List<Sprite> uniquePool = new List<Sprite>();
    private readonly List<Sprite> currentRoundSprites = new List<Sprite>(9);
    private readonly string[] buttonIDs = new string[9]; 
    private string targetID; 

    private Coroutine hideResultCo;

    void Awake()
    {
        
        if (buttons != null && buttons.Length > 0)
        {
            if (buttonImages == null || buttonImages.Length != buttons.Length)
            {
                buttonImages = new Image[buttons.Length];
                for (int i = 0; i < buttons.Length; i++)
                    buttonImages[i] = GetBestImageForButton(buttons[i]);
            }
        }
    }

    void Start()
    {
        

        
        if (topImage == null) { Debug.LogError("TOP IMAGE not assigned in Inspector."); enabled = false; return; }
        if (scoreText == null) { Debug.LogError("SCORE TEXT not assigned in Inspector."); enabled = false; return; }
        if (timerText == null) { Debug.LogError("TIMER TEXT not assigned in Inspector."); enabled = false; return; }
        if (resultText == null) { Debug.LogError("RESULT TEXT not assigned in Inspector."); enabled = false; return; }
        if (finalText == null) { Debug.LogError("FINAL TEXT not assigned in Inspector."); enabled = false; return; }

        if (buttons == null || buttons.Length != 9) { Debug.LogError("Buttons must be EXACTLY 9."); enabled = false; return; }
        if (buttonImages == null || buttonImages.Length != 9) { Debug.LogError("buttonImages must be 9 (or leave empty so auto-detect works)."); enabled = false; return; }
        if (sprites == null) { Debug.LogError("Sprites array not assigned."); enabled = false; return; }

        BuildUniquePool();
        if (uniquePool.Count < 9)
        {
            Debug.LogError($"Need >= 9 UNIQUE sprites. You currently have {uniquePool.Count}. Remove duplicates in sprites[].");
            enabled = false;
            return;
        }

        
        for (int i = 0; i < 9; i++)
        {
            int idx = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnPick(idx));
        }

        
        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(RestartGame);
            tryAgainButton.gameObject.SetActive(false);
        }
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            Debug.LogWarning("Nemas AudioSource na GameManager (Add Component -> AudioSource).");
        
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);

        PlayerPrefs.DeleteKey(HS_KEY);
        PlayerPrefs.Save();
        highscore = 0;

        highscore = PlayerPrefs.GetInt(HS_KEY, 0);

        
        RestartGame();
    }
    void UpdateHighscoreUI()
    {
        if (highscoreText != null)
            highscoreText.text = $"Highscore: {highscore}/{totalRounds}";
    }

    public void PauseGame()
    {
        if (gameEnded || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;                 
        SetButtonsInteractable(false);        

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }
    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;                 
        SetButtonsInteractable(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    void Update()
    {
        if (gameEnded || roundLocked) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;

        timerText.text = "Time: " + Mathf.CeilToInt(timeLeft);

        if (timeLeft <= 0f)
        {
            roundLocked = true;
            SetButtonsInteractable(false);
            ShowResult("TIME UP!", Color.yellow);
            NextRound();
        }
    }

    void BuildUniquePool()
    {
        uniquePool.Clear();
        var seen = new HashSet<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            var s = sprites[i];
            if (s == null) continue;
            if (seen.Add(s)) uniquePool.Add(s);
        }
    }

    public void RestartGame()
    {
        gameEnded = false;
        roundLocked = false;
        score = 0;
        roundsPlayed = 0;
        timeLeft = timePerRound;
        targetID = null;

        finalText.text = "";
        finalText.gameObject.SetActive(true);
        if (highscoreText != null)
            highscoreText.gameObject.SetActive(false);

        if (tryAgainButton != null)
            tryAgainButton.gameObject.SetActive(false);

        ShowResult("", Color.white);
        SetButtonsInteractable(true);




        StartRound();

    }

    void StartRound()
    {
        if (gameEnded) return;

        roundLocked = false;
        SetButtonsInteractable(true);

        timeLeft = timePerRound;

        
        if (roundsPlayed >= totalRounds)
        {
            EndGame();
            return;
        }

        roundsPlayed++;
        UpdateUI();

        SetupRound_NoDuplicates();
        PickTargetFromRound();

       
        int correctIndex = -1;
        for (int i = 0; i < 9; i++)
        {
            if (buttonIDs[i] == targetID) { correctIndex = i; break; }
        }

        if (correctIndex >= 0)
        {
            topImage.sprite = currentRoundSprites[correctIndex];
            topImage.color = Color.white;
        }
        else
        {
            Debug.LogError("Internal error: targetID not found in this round.");
        }
    }

    void SetupRound_NoDuplicates()
    {
        currentRoundSprites.Clear();

        
        List<int> idxs = new List<int>(uniquePool.Count);
        for (int i = 0; i < uniquePool.Count; i++) idxs.Add(i);

        for (int i = idxs.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (idxs[i], idxs[j]) = (idxs[j], idxs[i]);
        }

        for (int k = 0; k < 9; k++)
            currentRoundSprites.Add(uniquePool[idxs[k]]);

        
        for (int i = 0; i < 9; i++)
        {
            var s = currentRoundSprites[i];
            buttonImages[i].sprite = s;
            buttonImages[i].color = Color.black;

            
            buttonIDs[i] = s.name + "#" + s.GetInstanceID();
        }
    }

    void PickTargetFromRound()
    {
        int correctIndex = Random.Range(0, 9);
        targetID = buttonIDs[correctIndex];
    }

    void OnPick(int index)
    {
        if (gameEnded || roundLocked) return;
        if (index < 0 || index >= 9) return;

        roundLocked = true;
        SetButtonsInteractable(false);

        bool correct = (buttonIDs[index] == targetID);

        if (correct)
        {
            score++;
            buttonImages[index].color = Color.white;
            ShowResult("TOCNO!", Color.green);

            
            if (sfxSource != null && correctSfx != null)
                sfxSource.PlayOneShot(correctSfx, sfxVolume);
        }
        else
        {
            ShowResult("GRESNO!", Color.red);

            
            if (sfxSource != null && wrongSfx != null)
                sfxSource.PlayOneShot(wrongSfx, sfxVolume);
        }

        NextRound();
    }


    void NextRound()
    {
        UpdateUI();

        if (roundsPlayed >= totalRounds)
        {
            EndGame();
            return;
        }

        StartRound();
    }

    void EndGame()
    {
        gameEnded = true;
        roundLocked = true;
        SetButtonsInteractable(false);

        bool won = (score >= winAtLeast);
        string outcome = won ? "GAME WON" : "GAME OVER";

        finalText.text = $"{outcome}\nScore: {score}/{totalRounds}";
        finalText.color = won ? Color.green : Color.yellow;

        
        if (sfxSource != null)
        {
            if (won && winSfx != null)
                sfxSource.PlayOneShot(winSfx, sfxVolume);
            else if (!won && loseSfx != null)
                sfxSource.PlayOneShot(loseSfx, sfxVolume);
        }

        
        if (tryAgainButton != null)
            tryAgainButton.gameObject.SetActive(true);

        
        bool hadHighscoreBefore = PlayerPrefs.HasKey(HS_KEY);

        
        if (score > highscore)
        {
            highscore = score;
            PlayerPrefs.SetInt(HS_KEY, highscore);
            PlayerPrefs.Save();
        }

        
        if (highscoreText != null && hadHighscoreBefore)
        {
            highscoreText.gameObject.SetActive(true);
            highscoreText.text = $"Highscore: {highscore}/{totalRounds}";
        }
        else if (highscoreText != null)
        {
            highscoreText.gameObject.SetActive(false);
        }
    }



void UpdateUI()
    {
        scoreText.text = $"Score: {score}/{totalRounds}   Round: {roundsPlayed}/{totalRounds}";
    }

    void SetButtonsInteractable(bool on)
    {
        for (int i = 0; i < 9; i++)
            buttons[i].interactable = on;
    }

    void ShowResult(string msg, Color c)
    {
        resultText.text = msg;
        resultText.color = c;

        if (hideResultCo != null) StopCoroutine(hideResultCo);
        if (!string.IsNullOrEmpty(msg))
            hideResultCo = StartCoroutine(HideResultAfter(resultHideSeconds));
    }

    IEnumerator HideResultAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        resultText.text = "";
    }

    
    Image GetBestImageForButton(Button b)
    {
        if (b == null) return null;

        Image bg = b.GetComponent<Image>();
        Image[] imgs = b.GetComponentsInChildren<Image>(true);

        if (imgs != null)
        {
            for (int i = 0; i < imgs.Length; i++)
            {
                if (imgs[i] != null && imgs[i] != bg)
                    return imgs[i];
            }
        }
        return bg;
    }
}
