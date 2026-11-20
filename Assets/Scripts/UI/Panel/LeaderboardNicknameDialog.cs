using System;
using Anchor.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Anchor.UI.Panel
{
    public sealed class LeaderboardNicknameDialog : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button saveButton;
        [SerializeField] private TextMeshProUGUI errorText;

        private string runId;
        private Action onSaved;

        private void Awake()
        {
            nicknameInput.characterLimit = GameLeaderboardService.MaxNicknameLength;
            nicknameInput.onValueChanged.AddListener(OnNicknameChanged);
            saveButton.onClick.AddListener(SaveNickname);
        }

        public void Open(string completedRunId, Action saved)
        {
            runId = completedRunId;
            onSaved = saved;
            nicknameInput.SetTextWithoutNotify(string.Empty);
            errorText.text = string.Empty;
            gameObject.SetActive(true);
#if UNITY_WEBGL && !UNITY_EDITOR
            nicknameInput.transition = Selectable.Transition.None;
            nicknameInput.interactable = false;
            LeaderboardNicknameWebInput.Open(this, nicknameInput);
#else
            nicknameInput.Select();
            nicknameInput.ActivateInputField();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [UnityEngine.Scripting.Preserve]
        public void SetBrowserNickname(string value)
        {
            nicknameInput.text = value ?? string.Empty;
        }

        [UnityEngine.Scripting.Preserve]
        public void SubmitBrowserNickname()
        {
            SaveNickname();
        }
#endif

        private void SaveNickname()
        {
            if (string.IsNullOrEmpty(runId))
            {
                return;
            }

            if (!ValidateNickname(nicknameInput.text, out string nickname, out string error))
            {
                errorText.text = error;
                return;
            }

            if (!GameLeaderboardService.TrySetNickname(runId, nickname, out error))
            {
                errorText.text = error;
                return;
            }

            runId = null;
            gameObject.SetActive(false);
            onSaved?.Invoke();
            onSaved = null;
        }

        private void OnNicknameChanged(string value)
        {
            errorText.text = string.IsNullOrWhiteSpace(value) || ValidateNickname(value, out _, out string error)
                ? string.Empty
                : error;
        }

        private bool ValidateNickname(string value, out string nickname, out string error)
        {
            if (!GameLeaderboardService.TryNormalizeNickname(value, out nickname, out error))
            {
                return false;
            }

            if (nicknameInput.textComponent.font.HasCharacters(
                    nickname, out uint[] _, searchFallbacks: true, tryAddCharacter: true))
            {
                return true;
            }

            error = "有字符无法显示，请换一个昵称";
            return false;
        }
    }
}
