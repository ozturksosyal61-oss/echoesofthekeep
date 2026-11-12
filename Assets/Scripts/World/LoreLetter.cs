using UnityEngine;

namespace CoopGame
{
    /// <summary>Okunabilir mektup: etkileşimde yerel okuma ekranını açar (ağ yok). Metin LoreTexts'te.</summary>
    [RequireComponent(typeof(Interactable))]
    public class LoreLetter : MonoBehaviour
    {
        [SerializeField] int id;

        void Awake() => GetComponent<Interactable>().LocalHandler = () => LetterView.Show(id);
    }
}
