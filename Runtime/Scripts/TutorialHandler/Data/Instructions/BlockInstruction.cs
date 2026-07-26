using System.Collections.Generic;
using UnityEngine;

namespace tglGames.tutorial_manager.tgl_tutorial_handler.data
{
    [System.Serializable]
    public class BlockInstruction
    {
        public const float DefaultAlpha = 0.9f;
        public static readonly Color DefaultImageColor = new Color32(0x00, 0x15, 0x3B, 0xFF);// 00153BFF

        [SerializeField] private string colorHexCode;
        public float colorAlpha = -1;
        public RectTransform pointerPosition;
        public float zRotationForPointerToLookUp;
        public float pointerAngle;
        public bool useScreenPercentage;
        public Vector2  pointerOffset;
        public List<TutorialTextData> textsToShow;

        public Color BlockColor
        {
            get
            {
                if(!string.IsNullOrWhiteSpace(colorHexCode))
                { 
                    string hex = colorHexCode.StartsWith("#") ? colorHexCode : "#" + colorHexCode;
                    if (ColorUtility.TryParseHtmlString(hex, out Color parsedColor))
                    {
                        // Apply the current alpha state to the returned color context
                        parsedColor.a = (colorAlpha >= 0f && colorAlpha <= 1f) ? colorAlpha : 1f;
                        return parsedColor;
                    }
                    else
                    {
                        return DefaultImageColor;
                    }
                }
                else
                {
                    return DefaultImageColor;
                }
            }
            set
            {
                colorHexCode = ColorUtility.ToHtmlStringRGB(value);
            }
        }

        public void SetDefaultForNull()
        {
            // define default color and alpha
            if (colorAlpha <= 0 || colorAlpha > 1)
            {
                colorAlpha = DefaultAlpha; // It should be between 0 and 1. Defaulting to default value
            }

            if(!string.IsNullOrWhiteSpace(colorHexCode))
            {
                BlockColor = DefaultImageColor;
            }

            if (textsToShow == null)
            {
                textsToShow = new List<TutorialTextData>();
            }
        }
    }
}


