using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>"Kamera" düğmesi: üçüncü / birinci şahıs geçişi. Yalnızca yerel kamerayı değiştirir.</summary>
    public class CameraToggle : MonoBehaviour
    {
        [SerializeField] Button button;

        void Awake()
        {
            button.onClick.AddListener(() =>
            {
                if (FollowCamera.Instance != null) FollowCamera.Instance.Toggle();
            });
        }
    }
}
