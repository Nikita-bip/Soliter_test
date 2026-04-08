using TMPro;
using UnityEngine;

namespace Assets.Scripts.Views
{
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        public void ResetPanels()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
        }

        public void ShowWin() => winPanel?.SetActive(true);
        public void ShowLose() => losePanel?.SetActive(true);
    }
}