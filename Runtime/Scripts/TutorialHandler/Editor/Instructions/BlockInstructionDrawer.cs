using UnityEngine;
using UnityEditor;

namespace tglGames.tutorial_manager.tgl_tutorial_handler.data.Editor
{
    [CustomPropertyDrawer(typeof(BlockInstruction))]
    public class BlockInstructionDrawer : PropertyDrawer
    {
        // Caching texture for the rotation preview arrow
        private Texture2D _arrowTexture;
        float _iconSize = 40f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Fallback arrow texture if it's not loaded
            if (_arrowTexture == null)
            {
                // Up Icon found in editor
                _arrowTexture = EditorGUIUtility.FindTexture("StateMachineEditor.UpButtonHover") ?? EditorGUIUtility.whiteTexture;
            }

            EditorGUI.BeginProperty(position, label, property);

            // Find serialized properties
            SerializedProperty colorHexCodeProp = property.FindPropertyRelative("colorHexCode");
            SerializedProperty colorAlphaProp = property.FindPropertyRelative("colorAlpha");
            SerializedProperty pointerPositionProp = property.FindPropertyRelative("pointerPosition");
            SerializedProperty zRotationProp = property.FindPropertyRelative("zRotationForPointerToLookUp");
            SerializedProperty pointerAngleProp = property.FindPropertyRelative("pointerAngle");
            SerializedProperty useScreenPercentageProp = property.FindPropertyRelative("useScreenPercentage");
            SerializedProperty pointerOffsetProp = property.FindPropertyRelative("pointerOffset");
            SerializedProperty textsToShowProp = property.FindPropertyRelative("textsToShow");

            // Single-line height helper
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect currentRect = new Rect(position.x, position.y, position.width, lineHeight);

            // --- 1. Draw Title Label ---
            EditorGUI.LabelField(currentRect, label, EditorStyles.boldLabel);
            currentRect.y += lineHeight + spacing;

            // Indent content for clean nesting look
            EditorGUI.indentLevel++;

            // --- 2. Color Conversion Setup (Hex -> Unity Color Field) ---
            string hex = colorHexCodeProp.stringValue;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            if (!ColorUtility.TryParseHtmlString(hex, out Color currentDisplayColor))
            {
                currentDisplayColor = BlockInstruction.DefaultImageColor;
            }

            currentDisplayColor.a = (colorAlphaProp.floatValue >= 0f && colorAlphaProp.floatValue <= 1f)
                ? colorAlphaProp.floatValue
                : BlockInstruction.DefaultAlpha;

            // Draw Color Field
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUI.ColorField(currentRect, new GUIContent("Block Color"), currentDisplayColor, true,
                false, false);
            if (EditorGUI.EndChangeCheck())
            {
                colorHexCodeProp.stringValue = ColorUtility.ToHtmlStringRGB(newColor);
                colorAlphaProp.floatValue = newColor.a; // sync alpha alongside color change
            }

            currentRect.y += lineHeight + spacing;

            // --- 3. Color Alpha Horizontal Slider ---
            EditorGUI.BeginChangeCheck();
            // Uses standard slider, but users will naturally see the visual alpha representation in the step above
            float newAlpha = EditorGUI.Slider(currentRect, new GUIContent("Color Alpha"), colorAlphaProp.floatValue, 0f,
                1f);
            if (EditorGUI.EndChangeCheck())
            {
                colorAlphaProp.floatValue = newAlpha;
            }

            currentRect.y += lineHeight + spacing;

            // --- 4. Pointer Position (Normal RectTransform Layout) ---
            EditorGUI.PropertyField(currentRect, pointerPositionProp);
            currentRect.y += lineHeight + spacing;

            // --- 5 & 6. Pointer Rotation Group (Offset + Angle + Image) ---
            // Calculate the total height needed for two sliders, ensuring it's at least as tall as the icon
            float twoLineHeight = (lineHeight * 2) + spacing;
            float groupHeight = Mathf.Max(twoLineHeight, _iconSize);

            // Define layout bounds
            float sliderWidth = currentRect.width - (_iconSize + 10f); // Leave room on the right for the icon

            // Center the sliders vertically if the icon happens to be taller than two lines
            float slidersStartY = currentRect.y + (groupHeight - twoLineHeight) / 2f;
            Rect zRotRect = new Rect(currentRect.x, slidersStartY, sliderWidth, lineHeight);
            Rect angleSliderRect = new Rect(currentRect.x, zRotRect.y + lineHeight + spacing, sliderWidth, lineHeight);

            // Center the image vertically within the group bounds on the far right
            Rect arrowRect = new Rect(currentRect.x + currentRect.width - _iconSize, currentRect.y + (groupHeight - _iconSize) / 2f, _iconSize, _iconSize);

            // Draw Base Z-Rotation Slider
            EditorGUI.BeginChangeCheck();
            float zRot = EditorGUI.Slider(zRotRect, new GUIContent("At What Rotation your sprite looks Up (Offset)"), zRotationProp.floatValue, -360f, 360f);
            if (EditorGUI.EndChangeCheck())
            {
                zRotationProp.floatValue = zRot;
            }

            // Draw Pointer Angle Slider
            EditorGUI.BeginChangeCheck();
            float angle = EditorGUI.Slider(angleSliderRect, new GUIContent("Pointer Angle"),
                pointerAngleProp.floatValue, 0f, 360f);
            if (EditorGUI.EndChangeCheck())
            {
                pointerAngleProp.floatValue = angle;
            }

            // Draw rotating preview GUI arrow matrix
            // Draw rotating preview GUI arrow matrix
            Matrix4x4 matrixBackup = GUI.matrix;

            // 1. Invert the angle to match Scene Counter-Clockwise rotation
            // 2. Add the offset to simulate your unaligned base sprite
            float visualPreviewAngle = -angle + zRot; 

            GUIUtility.RotateAroundPivot(visualPreviewAngle, arrowRect.center);
            GUI.DrawTexture(arrowRect, _arrowTexture, ScaleMode.ScaleToFit);
            GUI.matrix = matrixBackup;

            // Advance the vertical layout by the entire group's height
            currentRect.y += groupHeight + spacing;

            // --- 7. Use Screen Percentage Toggle ---
            EditorGUI.PropertyField(currentRect, useScreenPercentageProp, new GUIContent("Use Screen Percentage"));
            currentRect.y += lineHeight + spacing;

            // --- 8. Pointer Offset (Adaptive Fields) ---
            if (useScreenPercentageProp.boolValue)
            {
                // Dynamic Slider Vector layout when Percentage is set to true
                Vector2 currentOffset = pointerOffsetProp.vector2Value;

                // X-Axis Limit Clamp Range (-1 to 1)
                Rect xRect = new Rect(currentRect.x, currentRect.y, currentRect.width, lineHeight);
                currentOffset.x = EditorGUI.Slider(xRect, new GUIContent("Pointer Offset X (%)"), currentOffset.x, -1f,
                    1f);
                currentRect.y += lineHeight + spacing;

                // Y-Axis Limit Clamp Range (-1 to 1)
                Rect yRect = new Rect(currentRect.x, currentRect.y, currentRect.width, lineHeight);
                currentOffset.y = EditorGUI.Slider(yRect, new GUIContent("Pointer Offset Y (%)"), currentOffset.y, -1f,
                    1f);
                currentRect.y += lineHeight + spacing;

                pointerOffsetProp.vector2Value = currentOffset;
            }
            else
            {
                // Standard Vector2 unit input field when false
                EditorGUI.PropertyField(currentRect, pointerOffsetProp, new GUIContent("Pointer Offset (Units)"));
                currentRect.y += lineHeight + spacing;
            }

            // --- 9. Texts To Show (List Context) ---
            // Lists/Arrays require variable height handles via dynamic GUI layouts
            EditorGUI.PropertyField(currentRect, textsToShowProp, new GUIContent("Texts To Show"), true);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        // Tells Unity how much vertical pixels to reserve for this structure in Inspector layouts
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Header/Label

            // Add base variables heights for variables that are not changing (Color, Alpha, Position, ScreenPercentToggle = 4 lines)
            totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4; 
    
            // Calculate the custom group height for the Offset + Angle + Image block
            float twoLineHeight = (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing;
            float groupHeight = Mathf.Max(twoLineHeight, _iconSize);
            totalHeight += groupHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty useScreenPercentageProp = property.FindPropertyRelative("useScreenPercentage");
            if (useScreenPercentageProp != null && useScreenPercentageProp.boolValue)
            {
                totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2; // Two sliders for X and Y
            }
            else
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Single standard vector field
            }

            // Dynamic heights for lists
            SerializedProperty textsToShowProp = property.FindPropertyRelative("textsToShow");
            if (textsToShowProp != null)
            {
                totalHeight += EditorGUI.GetPropertyHeight(textsToShowProp, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            // Include Targets list height ONLY for TutorialInstructionDrawer.cs
            SerializedProperty targetsProp = property.FindPropertyRelative("targets");
            if (targetsProp != null)
            {
                totalHeight += EditorGUI.GetPropertyHeight(targetsProp, true);
            }

            return totalHeight;
        }
    }
}
