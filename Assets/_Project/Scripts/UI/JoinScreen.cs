using FreezeOrThrow.Networking;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    public class JoinScreen : MonoBehaviour
    {
        const string DefaultPrompt = "Click the boxes to type the host’s code";
        const string ConnectingPrompt = "Connecting…";
        const string NotFoundPrompt = "No game found with that code";
        const float ConnectTimeoutSeconds = 5f;

        [SerializeField] Image[] codeBoxes;
        [SerializeField] TMP_Text[] codeLetters;
        [SerializeField] Sprite emptyBoxSprite;
        [SerializeField] Sprite focusedBoxSprite;
        [SerializeField] Sprite filledBoxSprite;
        [SerializeField] GameObject caret;
        [SerializeField] TMP_Text promptText;
        [SerializeField] Button joinButton;
        [SerializeField] Button backButton;
        [SerializeField] MenuScreens menuScreens;

        string code = "";
        bool connecting;
        float connectDeadline;
        Keyboard subscribedKeyboard;

        void OnEnable()
        {
            code = "";
            connecting = false;
            promptText.text = DefaultPrompt;
            Refresh();

            subscribedKeyboard = Keyboard.current;
            if (subscribedKeyboard != null)
            {
                subscribedKeyboard.onTextInput += HandleTextInput;
            }
            joinButton.onClick.AddListener(Join);
            backButton.onClick.AddListener(GoBack);
        }

        void OnDisable()
        {
            if (subscribedKeyboard != null)
            {
                subscribedKeyboard.onTextInput -= HandleTextInput;
                subscribedKeyboard = null;
            }
            joinButton.onClick.RemoveListener(Join);
            backButton.onClick.RemoveListener(GoBack);
        }

        void Update()
        {
            if (connecting)
            {
                WatchConnection();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                RemoveLastCharacter();
            }
            if (IsShortcutHeld(keyboard) && keyboard.vKey.wasPressedThisFrame)
            {
                Paste();
            }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                Join();
            }
        }

        void HandleTextInput(char character)
        {
            if (connecting || !GameCode.IsValidSymbol(character) || IsShortcutHeld(Keyboard.current))
            {
                return;
            }
            AppendCharacter(character);
        }

        bool IsShortcutHeld(Keyboard keyboard)
        {
            return keyboard != null
                && (keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed);
        }

        void AppendCharacter(char character)
        {
            if (code.Length >= GameCode.Length)
            {
                return;
            }
            code += char.ToUpperInvariant(character);
            CodeChanged();
        }

        void RemoveLastCharacter()
        {
            if (code.Length == 0)
            {
                return;
            }
            code = code.Substring(0, code.Length - 1);
            CodeChanged();
        }

        void Paste()
        {
            foreach (char character in GUIUtility.systemCopyBuffer)
            {
                if (GameCode.IsValidSymbol(character))
                {
                    AppendCharacter(character);
                }
            }
        }

        void CodeChanged()
        {
            promptText.text = DefaultPrompt;
            Refresh();
        }

        void Join()
        {
            if (connecting || code.Length != GameCode.Length)
            {
                return;
            }

            if (!GameCode.TryDecode(code, GameCode.LocalAddress(), out string hostAddress) || !SessionLauncher.Join(hostAddress))
            {
                promptText.text = NotFoundPrompt;
                return;
            }

            connecting = true;
            connectDeadline = Time.unscaledTime + ConnectTimeoutSeconds;
            promptText.text = ConnectingPrompt;
            Refresh();
        }

        void WatchConnection()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsConnectedClient)
            {
                connecting = false;
                menuScreens.ShowLobby();
                return;
            }

            bool stillTrying = networkManager != null && networkManager.IsListening;
            if (!stillTrying || Time.unscaledTime >= connectDeadline)
            {
                if (networkManager != null)
                {
                    networkManager.Shutdown();
                }
                connecting = false;
                promptText.text = NotFoundPrompt;
                Refresh();
            }
        }

        void GoBack()
        {
            if (connecting && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            connecting = false;
            menuScreens.ShowHome();
        }

        void Refresh()
        {
            int focusedIndex = connecting ? -1 : code.Length;
            for (int boxIndex = 0; boxIndex < codeBoxes.Length; boxIndex++)
            {
                bool typed = boxIndex < code.Length;
                codeLetters[boxIndex].text = typed ? code[boxIndex].ToString() : "";
                codeBoxes[boxIndex].sprite = typed ? filledBoxSprite : boxIndex == focusedIndex ? focusedBoxSprite : emptyBoxSprite;
            }

            bool caretVisible = focusedIndex >= 0 && focusedIndex < codeBoxes.Length;
            caret.SetActive(caretVisible);
            if (caretVisible)
            {
                caret.transform.position = codeBoxes[focusedIndex].transform.position;
            }

            joinButton.interactable = code.Length == GameCode.Length && !connecting;
        }
    }
}
