using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using tglGames.tutorial_manager.tgl_tutorial_handler.data;

namespace tglGames.tutorial_manager.tgl_tutorial_handler
{
    /// <summary>
    /// Uses "Custom/TutorialShader" shader to create cutout areas on the screen for tutorial purposes.<br/>
    /// Call <see cref="ShowTutorialCutouts"/> for showing a tutorial<br/>
    /// Call <see cref="HideTutorial"/> for hiding the tutorial<br/>
    /// </summary>
    public class TutorialHandler : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField] private Material cutoutMaterial;
        public Color blockerColor = Color.black;

        [SerializeField] private Image blockerImage;
        [SerializeField] private TutorialPointerHandler pointerHandler;
        [SerializeField] private TutorialTextHandler textHandler;

        private static readonly int RectsID = Shader.PropertyToID("_Rects"); // name of a property in our shader(Assets/All Modules/TutorialManager/Runtime/Shaders/TutorialShader.shader), used to set the cutout areas
        private static readonly int CountID = Shader.PropertyToID("_RectCount"); // name of a property in our shader(Assets/All Modules/TutorialManager/Runtime/Shaders/TutorialShader.shader), used to set the cutout areas

        private Canvas canvas;
        private bool canUpdate;
        private List<RectTransform> _currentCutoutTargets  = new List<RectTransform>();
        private List<RectTransform> _currentReparentTargets  = new List<RectTransform>();

        private struct OriginalTransformState
        {
            public Transform Parent;
            public int SiblingIndex;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        // Dictionary to keep track of every target we are currently showing in the tutorial
        private Dictionary<RectTransform, OriginalTransformState> savedStates = new Dictionary<RectTransform, OriginalTransformState>();
        private bool isTutorialVisible = false;

        public static Action<TutorialInstruction> ShowTutorialEvent; // TODO: Change to EventBus or MessageBus later
        public static Action HideTutorialEvent; // TODO: Change to EventBus or MessageBus later
        public static Action RefreshTutorialCanvasEvent; // TODO: Change to EventBus or MessageBus later
        public static Action<BlockInstruction> BlockScreenEvent; // TODO: Change to EventBus or MessageBus later
        public static Action<bool> InturreptTutorialEvent; // TODO: Change to EventBus or MessageBus later


        #region MonoBehaviour_Methods
        [ContextMenu("Awake")]
        private void Awake()
        {
            Initialize();
            if (canUpdate)
            {
                AddEventListeners();
            }
        }

        private void OnDestroy()
        {
            if (canUpdate)
            {
                RemoveEventListeners();
            }
        }
        #endregion MonoBehaviour_Methods


        #region Essentials
        private void Initialize()
        {
            if (blockerImage == null)
            {
                Debug.LogError("TutorialController: Image component reference is missing. Please assign it in the inspector.");
                enabled = false;
                canUpdate = false;
                return;
            }

            // Create a unique instance of the material so we don't affect others
            blockerImage.material = new Material(cutoutMaterial);

            canvas = blockerImage.GetComponentInParent<Canvas>();
            if(canvas == null)
            {
                Debug.LogError("TutorialController must be a child of a Canvas.");
                enabled = false;
                canUpdate = false;
                return;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogError($"TutorialController requires the Canvas to be in Screen Space - Overlay mode. Current mode: {canvas.renderMode}");
                enabled = false;
                canUpdate = false;
                return;
            }

            canUpdate = true;

            pointerHandler.Initialize();
            textHandler.Initialize();
        }

        private void AddEventListeners()
        {
            ShowTutorialEvent += AttemptShowTutorial;
            HideTutorialEvent += HideTutorial;
            BlockScreenEvent += BlockScreen;
            RefreshTutorialCanvasEvent += ReFreshCanvas;
            InturreptTutorialEvent += HideCanvasElement;
        }

        private void RemoveEventListeners()
        {
            ShowTutorialEvent -= AttemptShowTutorial;
            HideTutorialEvent -= HideTutorial;
            BlockScreenEvent -= BlockScreen;
            RefreshTutorialCanvasEvent -= ReFreshCanvas;
            InturreptTutorialEvent -= HideCanvasElement;
        }
        #endregion Essentials


        #region ShowTutorial

        private void BlockScreen(BlockInstruction blockInstruction)
        {
            isTutorialVisible = true;
            // define default color and alpha
            blockInstruction.SetDefaultForNull();

            UpdateBlockerUI(blockInstruction.colorAlpha, blockInstruction.BlockColor);

            // Set the pointer and text
            SetPointer(blockInstruction);
            SetText(blockInstruction);
        }

        private void AttemptShowTutorial(TutorialInstruction tutorialInstruction)
        {
            Debug.Log($"[Testing Log] : [{nameof(TutorialHandler)}].{nameof(AttemptShowTutorial)}(): Attempting to show Tutorial with {tutorialInstruction?.targets?.Count ?? 0} targets.");
            isTutorialVisible = true;
            // define default color and alpha
            tutorialInstruction.SetDefaultForNull();

            // ValidateTutorialIsOff();
            // ValidateTutorialData(tutorialInstruction);
            // ShowTutorial(tutorialInstruction);

            // show targets
            ShowTargetTutorial(tutorialInstruction.targets, tutorialInstruction.colorAlpha, tutorialInstruction.BlockColor);

            SetPointer(tutorialInstruction);
            SetText(tutorialInstruction);
        }

        private void ShowTargetTutorial(List<TutorialDisplayStruct> targetObjects, float colorAlpha, Color imageColor)
        {
            if(targetObjects == null || targetObjects.Count == 0)
            {
                Debug.LogWarning("ShowTutorialCutouts : No targets provided. Hiding Tutorials.");
                HideTutorial();
                return;
            }

            (List<RectTransform> targetsForCutout, List<RectTransform> targetsForReparent) = GenerateCutoutAndReparentLists(targetObjects);
            Debug.Log($"[Testing Log] : [{nameof(TutorialHandler)}].{nameof(ShowTargetTutorial)}(): After {nameof(GenerateCutoutAndReparentLists)} method, we have {targetsForCutout?.Count ?? 0} 'Cutout' targets and {targetsForReparent?.Count ?? 0} 'Reparent' Target");

            bool canCutout = targetsForCutout is { Count: > 0 } && EligibleForCutout(targetsForCutout);
            bool canReparent = targetsForReparent is { Count: > 0 } && EligibleForReparent(targetsForReparent);
            if (canCutout || canReparent)
            {
                UpdateBlockerUI(colorAlpha, imageColor);
                if(canCutout)
                {
                    Debug.Log($"[Testing Log] : [{nameof(TutorialHandler)}].{nameof(ShowTargetTutorial)}(): calling {nameof(ShowTutorialCutouts)}() method with {targetsForCutout.Count} targets.");
                    ShowTutorialCutouts(targetsForCutout); // Updates the UI Material using shader values
                }

                if (canReparent)
                {
                    Debug.Log($"[Testing Log] : [{nameof(TutorialHandler)}].{nameof(ShowTargetTutorial)}(): calling {nameof(ShowTutorialReparent)}() method with {targetsForReparent.Count} targets.");
                    ShowTutorialReparent(targetsForReparent);
                }

                if (!blockerImage.IsActive())
                {
                    Debug.LogError($"The blocker image is still off, verify the data");
                }
            }
            else
            {
                Debug.LogError($"Eligibility conditions failed, check the sent targets and the canvas setup. Hiding Tutorials.");
                HideTutorial();
            }
        }

        #endregion ShowTutorial


        #region CutoutLogic

        private bool EligibleForCutout(List<RectTransform> targets)
        {
            if(targets == null || targets.Count == 0)
            {
                Debug.LogWarning("EligibleForCutout : No targets provided. returning");
                return false;
            }

            if (!canUpdate)
            {
                Debug.LogError($"EligibleForCutout : Cannot update cutouts because the canvas is not in the correct render mode or is missing.");
                return false;
            }

            // Validate that all targets are valid scene objects
            foreach (var target in targets)
            {
                // Check if target is null
                if (target == null)
                {
                    Debug.LogWarning($"EligibleForCutout: Skipped null target. It is not a valid scene object.");
                    return false;
                }

                // Check if the GameObject is in an active scene
                if (!target.gameObject.scene.isLoaded)
                {
                    Debug.LogWarning($"EligibleForCutout: Skipped '{target.name}': It is not part of an active/loaded scene. Ensure it's not a prefab or is already instantiated in the scene.");
                    return false;
                }

                // Check if the GameObject is part of a prefab asset (not instantiated)
                #if UNITY_EDITOR
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(target.gameObject))
                {
                    Debug.LogWarning($"EligibleForCutout: Skipped '{target.name}': It is a prefab asset, not an instantiated scene object. Instantiate it in the scene first.");
                    return false;
                }
                #endif
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(blockerImage.rectTransform);
            Canvas.ForceUpdateCanvases();
            return true;

        }

        private void ShowTutorialCutouts(List<RectTransform> targets)
        {
            _currentCutoutTargets = targets;
            SetImageCutouts(targets);
        }

        private void SetImageCutouts(List<RectTransform> targets)
        {
            Vector4[] rects = new Vector4[targets.Count];

            // The shader uses SV_POSITION which is in actual screen pixels.
            // For Screen Space - Overlay, WorldToScreenPoint(null, ...) returns positions
            // that are already in screen pixel space, so no scaling is needed.
            // We must clamp and flip Y using actual screen dimensions (not canvas dimensions).
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                {
                    Debug.LogWarning($"SetImageCutouts: Target at index {i} is null. Skipping.");
                    continue;
                }

                Vector3[] corners = new Vector3[4];
                targets[i].GetWorldCorners(corners);

                // Convert World Corners to Screen Space
                // corners[0] is bottom-left, corners[2] is top-right
                Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

                #if UNITY_EDITOR
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                Debug.Log($"[CUTOUT DEBUG PRE-CLAMP] Target '{targets[i].name}': " +
                    $"ScreenMin={screenMin}, ScreenMax={screenMax}, " +
                    $"Canvas Resolution: {canvasRect.rect.width}x{canvasRect.rect.height}, " +
                    $"Screen Resolution: {screenWidth}x{screenHeight}");
                #endif

                // Clamp to screen bounds
                screenMin.x = Mathf.Clamp(screenMin.x, 0, screenWidth);
                screenMax.x = Mathf.Clamp(screenMax.x, 0, screenWidth);
                screenMin.y = Mathf.Clamp(screenMin.y, 0, screenHeight);
                screenMax.y = Mathf.Clamp(screenMax.y, 0, screenHeight);

                // Ensure min and max are in correct order
                float minX = Mathf.Min(screenMin.x, screenMax.x);
                float maxX = Mathf.Max(screenMin.x, screenMax.x);
                float minY = Mathf.Min(screenMin.y, screenMax.y);
                float maxY = Mathf.Max(screenMin.y, screenMax.y);

                // Flip Y coordinates for shader (rendermode has Y=0 at bottom, we need to invert it)
                #if UNITY_EDITOR
                #if UNITY_EDITOR_LINUX
                    rects[i] = new Vector4(minX, minY, maxX, maxY);
                #else
                    float flippedMinY = screenHeight - maxY;
                    float flippedMaxY = screenHeight - minY;
                    rects[i] = new Vector4(minX, flippedMinY, maxX, flippedMaxY);
                #endif
                #elif UNITY_ANDROID
                    rects[i] = new Vector4(minX, minY, maxX, maxY);
                #else
                    Debug.LogError($"Undefined handling for non-Android platforms in SetImageCutouts. Defaulting to no chnage. Please verify if this is intended.");
                #endif

                Debug.Log($"[CUTOUT DEBUG] Target '{targets[i].name}': " +
                    $"Corner0={corners[0]}, Corner2={corners[2]}, " +
                    $"Clamped: X=[{minX}, {maxX}], Y=[{minY}, {maxY}], " +
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
                    $"FinalRect=({minX}, {minY}, {maxX}, {maxY}), " +
                    $"ScreenSize=({screenWidth}, {screenHeight})");
#else
                    $"FlippedY: MinY={flippedMinY}, MaxY={flippedMaxY}, " +
                    $"FinalRect=({minX}, {flippedMinY}, {maxX}, {flippedMaxY}), " +
                    $"ScreenSize=({screenWidth}, {screenHeight})");
#endif
                    
#elif UNITY_ANDROID
                    $"FinalRect=({minX}, {minY}, {maxX}, {maxY}), " +
                    $"ScreenSize=({screenWidth}, {screenHeight})");
#else
                    $"FinalRect='undefined', " +
                    $"ScreenSize=({screenWidth}, {screenHeight})");
#endif
                #if UNITY_EDITOR
                DebugLogCutoutCorners(targets[i], i);
                #endif
            }
            blockerImage.material.SetVectorArray(RectsID, rects);
            blockerImage.material.SetInt(CountID, targets.Count);

            LayoutRebuilder.ForceRebuildLayoutImmediate(blockerImage.rectTransform);
            Canvas.ForceUpdateCanvases();
        }

        #endregion CutoutLogic


        #region ReparentLogic

        private bool EligibleForReparent(List<RectTransform> targets)
        {
            if(targets == null || targets.Count == 0)
            {
                Debug.LogWarning("EligibleForReparent : No targets provided. returning");
                return false;
            }

            if (!canUpdate)
            {
                Debug.LogError($"ShowTutorialCutouts : Cannot update cutouts because the canvas is not in the correct render mode or is missing.");
                return false;
            }

            // Validate that all targets are valid scene objects
            foreach (var target in targets)
            {
                // Check if target is null
                if (target == null)
                {
                    Debug.LogWarning($"Skipped null target: It is not a valid scene object.");
                    return false;
                }

                // Check if the GameObject is in an active scene
                if (!target.gameObject.scene.isLoaded)
                {
                    Debug.LogWarning($"Skipped '{target.name}': It is not part of an active/loaded scene. Ensure it's not a prefab or is already instantiated in the scene.");
                    return false;
                }

                // Check if the GameObject is part of a prefab asset (not instantiated)
                #if UNITY_EDITOR
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(target.gameObject))
                {
                    Debug.LogWarning($"Skipped '{target.name}': It is a prefab asset, not an instantiated scene object. Instantiate it in the scene first.");
                    return false;
                }
                #endif
            }

            return true;
        }

        private void ShowTutorialReparent(List<RectTransform> targets)
        {
            _currentReparentTargets = targets;
            foreach (var target in targets)
            {
                var state = new OriginalTransformState
                {
                    Parent = target.parent,
                    SiblingIndex = target.GetSiblingIndex(),
                    AnchoredPosition = target.anchoredPosition,
                    SizeDelta = target.sizeDelta,
                    AnchorMin = target.anchorMin,
                    AnchorMax = target.anchorMax,
                    Pivot = target.pivot,
                    LocalRotation = target.localRotation,
                    LocalScale = target.localScale
                };

                // Store in our dictionary
                if(!savedStates.ContainsKey(target))
                {
                    savedStates.Add(target, state);
                }
                else
                {
                    Debug.LogWarning($"ShowTutorialReparent: Target '{target.name}' is already in savedStates. This should not happen if duplicates are properly removed. Overwriting existing state.");
                    savedStates[target] = state; // Overwrite with the latest state, but ideally this should not happen due to duplicate checks
                }

                // Reparent to the blocker image.
                // worldPositionStays = true ensures it doesn't visually jump around the screen when parented
                target.SetParent(blockerImage.rectTransform, true);
            }

            // After reparenting, force the layout system to rebuild so any subsequent reads of
            // world position / corners (e.g. by TutorialPointerHandler) get accurate values.
            LayoutRebuilder.ForceRebuildLayoutImmediate(blockerImage.rectTransform);
            Canvas.ForceUpdateCanvases();
        }

        #endregion ReparentLogic


        #region HideTutorial

        [ContextMenu("hide tutorial")]
        private void HideTutorial()
        {
            isTutorialVisible = false;
            _currentCutoutTargets.Clear();
            HideReparentTargets();
            pointerHandler.HidePointer();
            textHandler.HideAllText();
            blockerImage.gameObject.SetActive(false);
        }

        private void HideReparentTargets()
        {
            foreach (var kvp in savedStates)
            {
                RectTransform target = kvp.Key;
                OriginalTransformState originalState = kvp.Value;

                if (target != null)
                {
                    // Reparent back to original parent
                    if (originalState.Parent != null)
                    {
                        target.SetParent(originalState.Parent, false);
                        _currentReparentTargets.Remove(target); // Remove from current list to avoid issues if HideTutorial is called multiple times
                        // in case no parent is found, we will delete the object using _currentReparentTargets later
                    }

                    // Restore the original sibling index (where it sat in the hierarchy among its peers)
                    target.SetSiblingIndex(originalState.SiblingIndex);

                    // Restore all layout properties
                    target.anchorMin = originalState.AnchorMin;
                    target.anchorMax = originalState.AnchorMax;
                    target.pivot = originalState.Pivot;
                    target.sizeDelta = originalState.SizeDelta;
                    target.anchoredPosition = originalState.AnchoredPosition;
                    target.localRotation = originalState.LocalRotation;
                    target.localScale = originalState.LocalScale;
                }
            }

            if (_currentReparentTargets.Count > 0)
            {
                Debug.LogWarning($"HideTutorial: There are {_currentReparentTargets.Count} reparented targets that were not restored. This may cause issues if dynamic object are used or HideTutorial is called multiple times without new ShowTutorial calls. \n Targets not restored: {string.Join(", ", _currentReparentTargets.ConvertAll(t => t.name))}");
                foreach (RectTransform reparentTarget in _currentReparentTargets)
                {
                    if (reparentTarget != null)
                    {
                        Destroy(reparentTarget.gameObject);
                    }
                }
            }

            // Clear out the state list for the next tutorial step
            savedStates.Clear();
            _currentReparentTargets.Clear();

            LayoutRebuilder.ForceRebuildLayoutImmediate(blockerImage.rectTransform);
            Canvas.ForceUpdateCanvases();
        }

        private void HideCanvasElement(bool hide)
        {
            if (isTutorialVisible)
            {
                blockerImage.gameObject.SetActive(!hide);
            }
        }

        #endregion HideTutorial


        #region Utility

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (_currentCutoutTargets != null)
            {
                foreach (var target in _currentCutoutTargets)
                {
                    if (target != null && RectTransformUtility.RectangleContainsScreenPoint(target, sp, eventCamera))
                        return false;
                }
            }
            return true;
        }

        private void UpdateBlockerUI(float colorAlpha, Color imageColor)
        {
            blockerImage.gameObject.SetActive(true);
            blockerImage.enabled = true;
            blockerImage.material = new Material(cutoutMaterial);


            colorAlpha = Mathf.Clamp(colorAlpha, 0, 1);
            blockerColor = imageColor;
            blockerColor.a = colorAlpha;
            blockerImage.color = blockerColor;
        }

        private (List<RectTransform>, List<RectTransform>) GenerateCutoutAndReparentLists(List<TutorialDisplayStruct> targets)
        {
            // divide the list into 2, show tutorial for both if there are no intersecting items between them.
            List<RectTransform> targetsForCutout = targets.FindAll(t => t.displayType == TutorialDisplayType.Cutout).ConvertAll(t => t.targetObject);
            List<RectTransform> targetsForReparent = targets.FindAll(t => t.displayType == TutorialDisplayType.Reparent).ConvertAll(t => t.targetObject);

            // 1. Remove duplicates WITHIN the Reparent list
            HashSet<RectTransform> seenReparents = new HashSet<RectTransform>();
            targetsForReparent.RemoveAll(target =>
            {
                // Add returns false if the item was already in the HashSet
                if (!seenReparents.Add(target))
                {
                    Debug.LogWarning($"Tutorial Duplicate: '{target.name}' appears multiple times in the Reparent list. Removing extra entry.");
                    return true; // remove duplicate
                }
                return false;
            });
            Debug.Log($"[Testing Log] : [{nameof(TutorialHandler)}].{nameof(GenerateCutoutAndReparentLists)}(): we have {targetsForReparent?.Count ?? 0} 'Reparent' Target");

            // 2. Remove duplicates WITHIN the Cutout list
            HashSet<RectTransform> seenCutouts = new HashSet<RectTransform>();
            targetsForCutout.RemoveAll(target =>
            {
                if (!seenCutouts.Add(target))
                {
                    Debug.LogWarning($"Tutorial Duplicate: '{target.name}' appears multiple times in the Cutout list. Removing extra entry.");
                    return true; // remove duplicate
                }
                return false;
            });
            Debug.Log($"[Testing Log] : [{nameof(TutorialHandler)}].{nameof(GenerateCutoutAndReparentLists)}(): we have {targetsForCutout?.Count ?? 0} 'Cutout' targets");


            // 3. Remove conflicts (exists in both Cutout and Reparent)
            targetsForCutout.RemoveAll(target =>
            {
                // If the reparent list also has this target...
                if (targetsForReparent.Contains(target))
                {
                    // Log the warning using the GameObject's name for easier debugging
                    Debug.LogWarning($"Tutorial Conflict: '{target.name}' is requested for both Cutout and Reparent. Defaulting to Reparent (removed from Cutout).");
                    // Return true to remove it from targetsForCutout
                    return true;
                }
                // Return false to keep it in targetsForCutout
                return false;
            });

            Debug.Log($"[Testing Log] : [{nameof(TutorialHandler)}].{nameof(GenerateCutoutAndReparentLists)}(): After this method, we have {targetsForCutout?.Count ?? 0} 'Cutout' targets and {targetsForReparent?.Count ?? 0} 'Reparent' Target");
            return (targetsForCutout, targetsForReparent) ;
        }

        private void SetPointer(BlockInstruction instruction)
        {
            if(instruction is { pointerPosition: not null})
            {
                pointerHandler.ShowPointer(instruction.pointerPosition, instruction.pointerAngle, instruction.useScreenPercentage, instruction.pointerOffset);
                pointerHandler.transform.SetAsLastSibling(); // Ensure pointer is on top of everything
            }
            else
            {
                pointerHandler.HidePointer();
            }
        }

        private void SetText(BlockInstruction instruction)
        {
            if (instruction.textsToShow is { Count: > 0 })
            {
                textHandler.ShowText(instruction.textsToShow);
                textHandler.transform.SetAsLastSibling(); // pointer becomes last second and text is last
            }
            else
            {
                textHandler.HideAllText();
            }
        }

        [ContextMenu("Refresh Canvas")]
        private void ReFreshCanvas()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(blockerImage.rectTransform);
            Canvas.ForceUpdateCanvases();
        }

        #endregion Utility


        #region Debug
        #if UNITY_EDITOR

        private void DebugLogCutoutCorners(RectTransform target, int index)
        {
            if (target == null)
            {
                Debug.LogWarning($"[CUTOUT DEBUG] Target at index {index} is null.");
                return;
            }

            RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect == null)
            {
                Debug.LogWarning($"[CUTOUT DEBUG] Canvas RectTransform is null for target '{target.name}'.");
                return;
            }

            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 4 world corners of the target RectTransform
            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);
            // order: 0=BL, 1=TL, 2=TR, 3=BR

            // Convert to screen-space for cutout math
            Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(null, worldCorners[0]);
            Vector2 screenTL = RectTransformUtility.WorldToScreenPoint(null, worldCorners[1]);
            Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(null, worldCorners[2]);
            Vector2 screenBR = RectTransformUtility.WorldToScreenPoint(null, worldCorners[3]);

            // Current logic in your code uses BL and TR
            Vector2 screenMin = screenBL;
            Vector2 screenMax = screenTR;

            // Clamp against both canvas rect size and screen size to compare
            Vector2 clampedCanvasMin = new Vector2(
                Mathf.Clamp(screenMin.x, 0f, canvasWidth),
                Mathf.Clamp(screenMin.y, 0f, canvasHeight)
            );
            Vector2 clampedCanvasMax = new Vector2(
                Mathf.Clamp(screenMax.x, 0f, canvasWidth),
                Mathf.Clamp(screenMax.y, 0f, canvasHeight)
            );

            Vector2 clampedScreenMin = new Vector2(
                Mathf.Clamp(screenMin.x, 0f, screenWidth),
                Mathf.Clamp(screenMin.y, 0f, screenHeight)
            );
            Vector2 clampedScreenMax = new Vector2(
                Mathf.Clamp(screenMax.x, 0f, screenWidth),
                Mathf.Clamp(screenMax.y, 0f, screenHeight)
            );

            // Rect built from canvas-clamped values (your current implementation)
            float canvasMinX = Mathf.Min(clampedCanvasMin.x, clampedCanvasMax.x);
            float canvasMaxX = Mathf.Max(clampedCanvasMin.x, clampedCanvasMax.x);
            float canvasMinY = Mathf.Min(clampedCanvasMin.y, clampedCanvasMax.y);
            float canvasMaxY = Mathf.Max(clampedCanvasMin.y, clampedCanvasMax.y);
            float canvasFlippedMinY = canvasHeight - canvasMaxY;
            float canvasFlippedMaxY = canvasHeight - canvasMinY;

            // Rect built from screen-clamped values (usually desired with WorldToScreenPoint)
            float screenMinX = Mathf.Min(clampedScreenMin.x, clampedScreenMax.x);
            float screenMaxX = Mathf.Max(clampedScreenMin.x, clampedScreenMax.x);
            float screenMinY = Mathf.Min(clampedScreenMin.y, clampedScreenMax.y);
            float screenMaxY = Mathf.Max(clampedScreenMin.y, clampedScreenMax.y);
            float screenFlippedMinY = screenHeight - screenMaxY;
            float screenFlippedMaxY = screenHeight - screenMinY;

            Debug.Log(
                $"[CUTOUT DEBUG] Target '{target.name}' (idx:{index})\n" +
                $"WorldCorners BL:{worldCorners[0]} TL:{worldCorners[1]} TR:{worldCorners[2]} BR:{worldCorners[3]}\n" +
                $"ScreenCorners BL:{screenBL} TL:{screenTL} TR:{screenTR} BR:{screenBR}\n" +
                $"CanvasRectSize: {canvasWidth}x{canvasHeight}, ScreenSize: {screenWidth}x{screenHeight}\n" +
                $"Using BL/TR => ScreenMin:{screenMin}, ScreenMax:{screenMax}\n" +
                $"CanvasClamp => Min:{clampedCanvasMin}, Max:{clampedCanvasMax}, " +
                $"FinalRect(canvas-space): ({canvasMinX}, {canvasFlippedMinY}, {canvasMaxX}, {canvasFlippedMaxY})\n" +
                $"ScreenClamp => Min:{clampedScreenMin}, Max:{clampedScreenMax}, " +
                $"FinalRect(screen-space): ({screenMinX}, {screenFlippedMinY}, {screenMaxX}, {screenFlippedMaxY})"
            );
        }

        #endif
        #endregion Debug


        #region ResetStatics
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ShowTutorialEvent = null;
            HideTutorialEvent = null;
            RefreshTutorialCanvasEvent = null;
            BlockScreenEvent = null;
            InturreptTutorialEvent = null;
        }
        #endregion ResetStatics



#if UNITY_EDITOR
        [SerializeField] private TutorialInstruction testTargets;
        [ContextMenu("Test Tutorial")]
        private void TestTutorial()
        {
            AttemptShowTutorial(testTargets);
        }
#endif

    }
}
