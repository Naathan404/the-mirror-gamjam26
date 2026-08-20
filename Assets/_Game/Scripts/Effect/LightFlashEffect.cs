using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LightFlashEffect : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image flashPanel;

    [Header("Effect Settings")]
    [SerializeField] private Color flashColorA = Color.white;
    [SerializeField] private Color flashColorB = Color.black;
    [SerializeField] private Ease flashEase = Ease.Flash;

    private Tween _flashTween;

    private void Awake()
    {
        if (flashPanel != null)
        {
            flashPanel.gameObject.SetActive(false);
        }
    }

    public void PlayLightFlash()
    {
        if (_flashTween != null && _flashTween.IsActive())
        {
            _flashTween.Kill();
        }

        if (flashPanel == null) return;

        flashPanel.gameObject.SetActive(true);
        flashPanel.color = flashColorA;

        Sequence flashSequence = DOTween.Sequence();

        float[] durations = { 0.03f, 0.04f, 0.05f, 0.1f, 0.15f };

        for (int i = 0; i < durations.Length; i++)
        {
            flashSequence.Append(flashPanel.DOColor(flashColorB, durations[i]).SetEase(flashEase));
            flashSequence.Append(flashPanel.DOColor(flashColorA, durations[i]).SetEase(flashEase));
        }

        flashSequence.Append(flashPanel.DOFade(0f, 0.3f).SetEase(flashEase));
        flashSequence.OnComplete(() =>
        {
            flashPanel.gameObject.SetActive(false);
        });

        _flashTween = flashSequence;
    }
}