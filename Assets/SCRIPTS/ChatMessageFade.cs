using System.Collections;
using UnityEngine;
using TMPro;

public class ChatMessageFade : MonoBehaviour
{
    public void StartFade(float duration, Color baseColor)
    {
        StartCoroutine(FadeRoutine(duration, baseColor));
    }

    IEnumerator FadeRoutine(float duration, Color baseColor)
    {
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null) yield break;

        // 1. Плавное появление (0.2 сек)
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
        tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        // 2. Ждем (независимо от того, закрыта консоль или нет)
        yield return new WaitForSecondsRealtime(duration);

        // 3. Плавное исчезновение (0.5 сек)
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        // 4. Удаляем текст
        Destroy(gameObject);
    }
}