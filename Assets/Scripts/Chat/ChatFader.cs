using UnityEngine;
using System.Collections;

public class ChatFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup chatCanvasGroup;
    [SerializeField] private float fadeTime = 3f;
    [SerializeField] private float fadeDuration = 1f;

    private float lastMessageTime;
    private bool isFading = false;

    void Update()
    {
        if (!isFading && Time.time - lastMessageTime > fadeTime)
        {
            StartCoroutine(FadeChat());
        }
    }

    public void OnNewMessage()
    {
        lastMessageTime = Time.time;
        chatCanvasGroup.alpha = 1f;
        isFading = false;
    }

    IEnumerator FadeChat()
    {
        isFading = true;
        float startAlpha = chatCanvasGroup.alpha;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            chatCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0.3f, t / fadeDuration);
            yield return null;
        }

        isFading = false;
    }
}
