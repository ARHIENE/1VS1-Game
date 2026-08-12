using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PKUIManager : MonoBehaviour
{
    public static PKUIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI kickCountText;
    public TextMeshProUGUI roleText;
    public TextMeshProUGUI timerText;

    [Header("Kicker Guide")]
    public GameObject      kickerGuidePanel;
    public TextMeshProUGUI kickerGuideText;

    [Header("GK Guide")]
    public GameObject      gkGuidePanel;
    public TextMeshProUGUI gkGuideText;

    [Header("Result")]
    public GameObject      resultPanel;
    public TextMeshProUGUI resultMainText;
    public TextMeshProUGUI resultSubText;

    [Header("Sudden Death")]
    public GameObject      suddenDeathPanel;

    [Header("Match Result")]
    public GameObject      matchResultPanel;
    public TextMeshProUGUI matchResultText;
    public TextMeshProUGUI finalScoreText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (resultPanel)       resultPanel.SetActive(false);
        if (matchResultPanel)  matchResultPanel.SetActive(false);
        if (suddenDeathPanel)  suddenDeathPanel.SetActive(false);
    }

    public void UpdateTurnUI(bool isLocalKicker, bool isTeamATurn,
        int sA, int sB, int kA, int kB, bool sd, int sdPair)
    {
        if (turnText)
            turnText.text = sd
                ? $"서든데스 {sdPair + 1}R — {(isTeamATurn ? "A" : "B")}팀 킥"
                : $"{(isTeamATurn ? "A" : "B")}팀 킥  ({(isTeamATurn ? kA : kB) + 1} / {5})";

        if (scoreText)     scoreText.text    = $"A  {sA} : {sB}  B";
        if (kickCountText) kickCountText.text = $"A {kA}번  /  B {kB}번";
        if (roleText)      roleText.text      = isLocalKicker ? "키커" : "골키퍼";

        if (kickerGuidePanel) kickerGuidePanel.SetActive(isLocalKicker);
        if (kickerGuideText && isLocalKicker)
            kickerGuideText.text = "공 위에서 드래그 → 방향 설정\n스페이스바 → 파워 타이밍으로 발사";

        if (gkGuidePanel) gkGuidePanel.SetActive(!isLocalKicker);
        if (gkGuideText && !isLocalKicker)
            gkGuideText.text = "골대 안을 클릭해서 막을 위치 선택";

        if (resultPanel) resultPanel.SetActive(false);
    }

    public void ShowResult(bool isGoal, string reason, int sA, int sB)
    {
        if (resultPanel) resultPanel.SetActive(true);
        if (resultMainText)
        {
            resultMainText.text  = isGoal ? "GOAL!" : reason;
            resultMainText.color = isGoal ? Color.yellow : Color.white;
        }
        if (resultSubText)  resultSubText.text = reason;
        if (scoreText)      scoreText.text     = $"A  {sA} : {sB}  B";
    }

    public void ShowSuddenDeath()
    {
        if (suddenDeathPanel)
        {
            suddenDeathPanel.SetActive(true);
            Invoke(nameof(HideSD), 2.5f);
        }
    }
    void HideSD() { if (suddenDeathPanel) suddenDeathPanel.SetActive(false); }

    public void ShowMatchResult(bool localWon, int sA, int sB, string reason)
    {
        if (matchResultPanel) matchResultPanel.SetActive(true);
        if (matchResultText)
        {
            matchResultText.text  = localWon ? "승리!" : "패배...";
            matchResultText.color = localWon ? Color.yellow : Color.gray;
        }
        if (finalScoreText)
            finalScoreText.text = $"최종 스코어\nA  {sA} : {sB}  B\n({reason})";
    }

    public void UpdateTimer(float remaining)
    {
        if (timerText) timerText.text = Mathf.CeilToInt(Mathf.Max(remaining, 0f)).ToString();
    }

    public void ShowWaitingForOpponent(bool isKicker)
    {
        if (isKicker)
        {
            if (kickerGuidePanel) kickerGuidePanel.SetActive(true);
            if (kickerGuideText) kickerGuideText.text = "상대방 선택 대기 중...";
        }
        else
        {
            if (gkGuidePanel) gkGuidePanel.SetActive(true);
            if (gkGuideText) gkGuideText.text = "상대방 선택 대기 중...";
        }
    }
}
