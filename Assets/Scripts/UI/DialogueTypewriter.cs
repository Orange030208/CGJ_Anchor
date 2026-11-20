using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Anchor.UI
{
    internal static class DialogueTypewriter
    {
        internal static IEnumerator Reveal(
            TMP_Text text, float charactersPerSecond, bool unscaledTime,
            Action<int, int> charactersRevealed = null)
        {
            int total = text.textInfo.characterCount;
            float progress = 0f;
            int visible = 0;
            text.maxVisibleCharacters = 0;

            while (visible < total)
            {
                progress += charactersPerSecond * (unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
                int next = Mathf.Min(Mathf.FloorToInt(progress), total);
                if (next > visible)
                {
                    text.maxVisibleCharacters = next;
                    charactersRevealed?.Invoke(visible, next);
                    visible = next;
                }

                yield return null;
            }

            text.maxVisibleCharacters = total;
        }
    }
}
