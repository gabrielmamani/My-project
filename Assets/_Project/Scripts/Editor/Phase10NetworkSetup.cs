#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Network;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 10 Network Setup:
    /// Constructs Panel_NetworkLobby UI (Host, Client, Disconnect, 10s Disconnection Timeout simulation)
    /// and configures NetworkLobbyManager and NetworkLobbyUI components (RF-07.1.1, RF-07.1.2, RF-07.1.3).
    /// </summary>
    [InitializeOnLoad]
    public static class Phase10NetworkSetup
    {
        static Phase10NetworkSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    ExecuteBatchSetup();
                }
            };
        }

        [MenuItem("Gunbound/Setup Phase 10 (Network Lobby Manager)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            // 1. Ensure [NetworkManager] GameObject with Netcode components
            GameObject netManagerObj = GameObject.Find("NetworkManager");
            if (netManagerObj == null)
            {
                netManagerObj = GameObject.Find("NetworkLobbyManager");
            }
            if (netManagerObj == null)
            {
                netManagerObj = new GameObject("NetworkManager");
                Undo.RegisterCreatedObjectUndo(netManagerObj, "Create NetworkManager");
            }
            else
            {
                netManagerObj.name = "NetworkManager";
            }

            var ngoManager = GetOrAdd<Unity.Netcode.NetworkManager>(netManagerObj);
            var utpTransport = GetOrAdd<Unity.Netcode.Transports.UTP.UnityTransport>(netManagerObj);
            if (ngoManager.NetworkConfig == null)
            {
                ngoManager.NetworkConfig = new Unity.Netcode.NetworkConfig();
            }
            ngoManager.NetworkConfig.NetworkTransport = utpTransport;
            ngoManager.NetworkConfig.EnableSceneManagement = false;

            AddSceneToBuildSettings.RegisterActiveSceneToBuildSettings();

            GetOrAdd<NetworkGameManager>(netManagerObj);
            GetOrAdd<NetworkLobbyManager>(netManagerObj);
            GetOrAdd<NetworkTurnManager>(netManagerObj);

            // 2. Find Canvas
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                var canvasComp = Object.FindAnyObjectByType<Canvas>();
                if (canvasComp != null) canvasObj = canvasComp.gameObject;
            }
            if (canvasObj == null) return;

            Transform parentTransform = canvasObj.transform.Find("HUDPanel") ?? canvasObj.transform;
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);

            // 3. Create Panel_NetworkLobby anchored in Top-Right (280x150)
            GameObject panelObj = GetOrCreateContainer(parentTransform, "Panel_NetworkLobby", new Vector2(1f, 1f), new Vector2(-15f, -15f), new Vector2(280f, 150f));
            RectTransform panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-15f, -15f);

            Image panelBg = GetOrAdd<Image>(panelObj);
            panelBg.color = new Color(0.05f, 0.08f, 0.14f, 0.88f); // Dark blue-gray

            VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(panelObj);
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperRight;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            // Header & Role Labels
            Text titleText = GetOrCreateText(panelObj.transform, "TitleText", "LOBBY MULTIJUGADOR (1v1)", defaultFont, 11, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
            Text roleText = GetOrCreateText(panelObj.transform, "RoleText", "Rol: OFFLINE", defaultFont, 11, FontStyle.Bold, Color.gray, TextAnchor.MiddleCenter);

            // Input Fields Row (IP & Port)
            GameObject rowInput = GetOrCreateContainer(panelObj.transform, "Row_Inputs", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 26f));
            HorizontalLayoutGroup hlgInputs = GetOrAdd<HorizontalLayoutGroup>(rowInput);
            hlgInputs.spacing = 6f;
            hlgInputs.childControlWidth = false;
            hlgInputs.childForceExpandWidth = false;

            InputField ipInput = CreateStyledInputField(rowInput.transform, "InputField_IP", "127.0.0.1", defaultFont, new Vector2(170f, 24f));
            InputField portInput = CreateStyledInputField(rowInput.transform, "InputField_Port", "7777", defaultFont, new Vector2(74f, 24f));

            // Buttons Row 1 (Host & Client)
            GameObject rowBtns1 = GetOrCreateContainer(panelObj.transform, "Row_HostClient", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 28f));
            HorizontalLayoutGroup hlgBtns1 = GetOrAdd<HorizontalLayoutGroup>(rowBtns1);
            hlgBtns1.spacing = 6f;
            hlgBtns1.childControlWidth = true;
            hlgBtns1.childForceExpandWidth = true;

            Button btnHost = CreateStyledButton(rowBtns1.transform, "Btn_Host", "Host (P1)", defaultFont, new Vector2(125f, 26f), new Color(0.2f, 0.6f, 0.9f));
            Button btnClient = CreateStyledButton(rowBtns1.transform, "Btn_Client", "Unirse (P2)", defaultFont, new Vector2(125f, 26f), new Color(0.9f, 0.55f, 0.15f));

            // Buttons Row 2 (Disconnect & Simulate Timeout)
            GameObject rowBtns2 = GetOrCreateContainer(panelObj.transform, "Row_Disconnect", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 24f));
            HorizontalLayoutGroup hlgBtns2 = GetOrAdd<HorizontalLayoutGroup>(rowBtns2);
            hlgBtns2.spacing = 6f;
            hlgBtns2.childControlWidth = true;
            hlgBtns2.childForceExpandWidth = true;

            Button btnDisconnect = CreateStyledButton(rowBtns2.transform, "Btn_Disconnect", "Salir", defaultFont, new Vector2(125f, 22f), new Color(0.6f, 0.2f, 0.2f));
            Button btnTimeout = CreateStyledButton(rowBtns2.transform, "Btn_SimulateTimeout", "Simular 10s DC", defaultFont, new Vector2(125f, 22f), new Color(0.5f, 0.3f, 0.6f));

            Text statusText = GetOrCreateText(panelObj.transform, "StatusText", "Desconectado de la red.", defaultFont, 10, FontStyle.Italic, Color.white, TextAnchor.MiddleCenter);

            // Bind UI to NetworkLobbyUI component
            NetworkLobbyUI lobbyUI = GetOrAdd<NetworkLobbyUI>(panelObj);
            lobbyUI.BindFields(ipInput, portInput, btnHost, btnClient, btnDisconnect, btnTimeout, statusText, roleText);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Phase10NetworkSetup] Panel_NetworkLobby UI (Top-Right) setup completed successfully!");
        }

        private static GameObject GetOrCreateContainer(Transform parent, string name, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
                obj.transform.SetParent(parent, false);
            }

            RectTransform rt = GetOrAdd<RectTransform>(obj);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return obj;
        }

        private static Text GetOrCreateText(Transform parent, string name, string textStr, Font font, int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
                obj.transform.SetParent(parent, false);
            }

            RectTransform rt = GetOrAdd<RectTransform>(obj);
            rt.sizeDelta = new Vector2(250f, 18f);

            Text text = GetOrAdd<Text>(obj);
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.text = textStr;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private static InputField CreateStyledInputField(Transform parent, string name, string defaultVal, Font font, Vector2 size)
        {
            GameObject obj = GetOrCreateContainer(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image bg = GetOrAdd<Image>(obj);
            bg.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);

            Text textComp = GetOrCreateText(obj.transform, "Text", defaultVal, font, 11, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
            RectTransform textRt = textComp.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = new Vector2(4f, 0f);

            InputField inputField = GetOrAdd<InputField>(obj);
            inputField.textComponent = textComp;
            inputField.text = defaultVal;
            return inputField;
        }

        private static Button CreateStyledButton(Transform parent, string name, string label, Font font, Vector2 size, Color bgColor)
        {
            GameObject obj = GetOrCreateContainer(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image bg = GetOrAdd<Image>(obj);
            bg.color = bgColor;

            Text textComp = GetOrCreateText(obj.transform, "Text", label, font, 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform textRt = textComp.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            Button btn = GetOrAdd<Button>(obj);
            btn.targetGraphic = bg;
            return btn;
        }

        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null) comp = obj.AddComponent<T>();
            return comp;
        }
    }
}
#endif
