using TMPro;
using UnityEngine;

public class GuessHistoryRow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] _symbolIcons;
    [SerializeField] private TextMeshPro _resultText;

    public void Setup(int[] guess, Sprite[] symbolSprites, int exact, int partial)
    {
        for (int i = 0; i < _symbolIcons.Length; i++)
        {
            bool active = i < guess.Length;
            _symbolIcons[i].gameObject.SetActive(active);
            if (active) _symbolIcons[i].sprite = symbolSprites[guess[i]];
        }

        _resultText.text = $"● {exact}   ○ {partial}";
    }
}