using System;
using System.Collections.Generic;
using tglGames.tutorial_manager.tgl_tutorial_handler.data;
using TMPro;
using UnityEngine;

namespace tglGames.tutorial_manager.tgl_tutorial_handler
{
    public class TutorialTextHandler : MonoBehaviour
    {
        [SerializeField] private List<TextMeshProUGUI> textFields; // Set text fields in order of appearance
        private TextMeshProUGUI[]  texts;
        private bool isInitialized;

        [ContextMenu("Find all TMP Text Fields")]
        private void FindAllTextFields()
        {
            TextMeshProUGUI[] tmpTexts = GetComponentsInChildren<TextMeshProUGUI>();
            if (tmpTexts != null && tmpTexts.Length != 0) // data is available and loaded
            {
                textFields  = new List<TextMeshProUGUI>(tmpTexts);
                return;
            }
            
            Debug.LogError("Text fields are not properly initialized. Cannot find text fields.");
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (textFields == null || textFields.Count == 0)
            {
                Debug.LogError("Text fields list is not set or empty. Please assign text fields in the inspector.");
                FindAllTextFields();
                if (textFields == null || textFields.Count == 0)
                {
                    return;
                }
            }

            texts =  new TextMeshProUGUI[textFields.Count];
            TextMeshProUGUI textField = null;

            for (int i = 0; i < textFields.Count; i++)
            {
                textField = textFields[i];
                if (textField == null)
                {
                    Debug.LogError($"Child at index {i} does not have a TextMeshProUGUI component.");
                    texts = null;
                    break;
                }
                else
                {
                    texts[i] = textField;
                }
            }

            isInitialized = true;
        }

        public void ShowText(List<TutorialTextData> textData)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (texts == null || texts.Length == 0)
            {
                Debug.LogError("Text fields are not properly initialized. Cannot show text.");
                return;
            }

            if(textData == null || textData.Count == 0)
            {
                Debug.LogWarning("No text data provided to show.");
                return;
            }

            HideAllText();

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            foreach (TutorialTextData textInfo in textData)
            {
                ShowText(textInfo.displayText, textInfo.textPosition);
            }
        }

        private void ShowText(string text, int position)
        {
            if(position < 0 || position >= textFields.Count)
            {
                Debug.LogError("Position out of range for text fields.");
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogError("Text is null or empty. Cannot display text.");
                return;
            }

            texts[position].gameObject.SetActive(true);
            texts[position].text = text;
        }

        public void HideAllText()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            foreach (TextMeshProUGUI textField in textFields)
            {
                textField.gameObject.SetActive(false);
                textField.text = string.Empty;
            }

            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
