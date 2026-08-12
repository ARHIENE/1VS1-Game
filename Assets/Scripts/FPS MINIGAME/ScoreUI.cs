using UnityEngine;
using TMPro;

namespace FPSMinigame
{
    public class ScoreUI : MonoBehaviour
    {
        public TMP_Text scoreText;

        private void Start()
        {
            UpdateScore();
        }

        public void UpdateScore()
        {
            if (scoreText != null && MG01Manager.Instance != null)
            {
                scoreText.text = MG01Manager.Instance.GetScoreText();
            }
        }
    }
}