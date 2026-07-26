using System;
using UnityEngine;
using UnityEngine.UI;

namespace tglGames.tutorial_manager.tgl_tutorial_handler
{
    public class TutorialPointerHandler : MonoBehaviour
    {
        [SerializeField] private GameObject focusPointerCenter; // Set center to position you want to point at
        [SerializeField] private GameObject rippleObj; // Ripple image
        [SerializeField] private RectTransform focusPointerRotation; // set the z-rotation to the value you want the hand to have

        private RectTransform centerRectTransform;
        private bool isInitialized;


        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (focusPointerCenter != null)
            {
                centerRectTransform = focusPointerCenter.GetComponent<RectTransform>();
            }

            isInitialized = true;
        }

        public void ShowPointer(RectTransform positionRt, float angle, bool useScreenPercentage, Vector2 pointerOffset)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            SetPointerPosition(positionRt, useScreenPercentage, pointerOffset);
            SetPointerRotation(angle);
            ShowFocusPointer();
        }

        private void SetPointerPosition(RectTransform positionRt, bool useScreenPercentage, Vector2 pointerOffset)
        {
            if (positionRt == null)
            {
                return;
            }

            // CRITICAL: Force layout + canvas update so any recent reparenting / layout group changes
            // are reflected in the world corner positions we are about to read. Without this, freshly
            // reparented targets can report stale world positions.
            LayoutRebuilder.ForceRebuildLayoutImmediate(positionRt);
            Canvas.ForceUpdateCanvases();

            // Get the 4 corners of the target in WORLD space.
            // GetWorldCorners returns: [0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right.
            // These are real Unity world-space coords (already account for CanvasScaler, anchors, pivot, parent chain).
            Vector3[] worldCorners = new Vector3[4];
            positionRt.GetWorldCorners(worldCorners);

            // True geometric center of the rect in world space (independent of the target's pivot).
            Vector3 worldCenter = (worldCorners[0] + worldCorners[2]) * 0.5f;

            // Convert that world point into the pointer's PARENT local space so anchoredPosition/localPosition
            // is computed correctly regardless of CanvasScaler scale or any parent transforms.
            // We use the parent because localPosition is always relative to the parent.
            Transform parent = centerRectTransform.parent;
            if (parent == null)
            {
                // Fallback: no parent, set world position directly.
                centerRectTransform.position = worldCenter;
                // LogPointerDebug(positionRt, worldCorners, worldCenter, Vector3.zero, "NO_PARENT");
                return;
            }

            Vector3 localInParent = parent.InverseTransformPoint(worldCenter);
            
            
            // Calculate final offset to apply in parent local space
            Vector2 finalOffset = Vector2.zero;
            if (useScreenPercentage)
            {
                // Clamp the percentage offsets strictly between -1 and 1
                float clampedXPercent = Mathf.Clamp(pointerOffset.x, -1f, 1f);
                float clampedYPercent = Mathf.Clamp(pointerOffset.y, -1f, 1f);

                // Get screen size (Game window in Editor, actual screen on Android)
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;

                // Convert screen pixels to World units using the main camera (assuming Screen Space - Camera/Overlay)
                // Then convert that World delta into the Parent's local space delta.
                Vector3 screenDeltaInWorld = new Vector3(clampedXPercent * screenWidth, clampedYPercent * screenHeight, 0f);
        
                // We use Canvas scale factor or InverseTransformVector to translate screen pixels into parent local space.
                // Transforming a directional vector ignores positioning and just scales the pixels correctly.
                Vector3 localOffset = parent.InverseTransformVector(screenDeltaInWorld);
        
                finalOffset.x = localOffset.x;
                finalOffset.y = localOffset.y;
            }
            else
            {
                // Use standard local units if percentage is disabled
                finalOffset = pointerOffset;
            }


            // Apply pointer offset (in parent's local units)
            localInParent.x += finalOffset.x;
            localInParent.y += finalOffset.y;
            // Preserve current Z so we don't accidentally move the pointer in front of/behind the canvas plane.
            localInParent.z = centerRectTransform.localPosition.z;
            centerRectTransform.localPosition = localInParent;
#if UNITY_EDITOR
            // LogPointerDebug(positionRt, worldCorners, worldCenter, localInParent, "OK");
#endif
        }

        #if UNITY_EDITOR
        private void LogPointerDebug(RectTransform target, Vector3[] worldCorners, Vector3 worldCenter, Vector3 computedLocal, string status)
        {
            // ---- Target info ----
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Vector3[] targetLocalCorners = new Vector3[4];
            target.GetLocalCorners(targetLocalCorners);
            Vector2 targetScreenCenter = RectTransformUtility.WorldToScreenPoint(null, worldCenter);

            // ---- Pointer info ----
            RectTransform pointerRt = centerRectTransform;
            Transform pointerParent = pointerRt != null ? pointerRt.parent : null;
            RectTransform pointerParentRt = pointerParent as RectTransform;
            Canvas pointerCanvas = pointerRt != null ? pointerRt.GetComponentInParent<Canvas>() : null;

            // ---- After-assignment pointer state ----
            Vector3 pointerWorldPos = pointerRt != null ? pointerRt.position : Vector3.zero;
            Vector3 pointerLocalPos = pointerRt != null ? pointerRt.localPosition : Vector3.zero;
            Vector2 pointerAnchoredPos = pointerRt != null ? pointerRt.anchoredPosition : Vector2.zero;
            Vector2 pointerScreenPos = RectTransformUtility.WorldToScreenPoint(null, pointerWorldPos);

            // ---- Compute delta to see error magnitude ----
            Vector2 screenDelta = pointerScreenPos - targetScreenCenter;
            Vector3 worldDelta = pointerWorldPos - worldCenter;

            Debug.Log(
                $"<color=cyan>[TUT-POINTER DEBUG]</color> status={status}\n" +
                $"--- SCREEN ---\n" +
                $"  Screen.size               = ({Screen.width} x {Screen.height})\n" +
                $"--- TARGET '{target.name}' ---\n" +
                $"  target.position (world)   = {target.position}\n" +
                $"  target.localPosition      = {target.localPosition}\n" +
                $"  target.anchoredPosition   = {target.anchoredPosition}\n" +
                $"  target.anchorMin/Max      = {target.anchorMin} / {target.anchorMax}\n" +
                $"  target.pivot              = {target.pivot}\n" +
                $"  target.rect (local)       = {target.rect}  (size={target.rect.size})\n" +
                $"  target.lossyScale         = {target.lossyScale}\n" +
                $"  target.parent             = {(target.parent != null ? target.parent.name : "<null>")}\n" +
                $"  target.canvas             = {(targetCanvas != null ? targetCanvas.name : "<null>")} " +
                    $"(mode={(targetCanvas != null ? targetCanvas.renderMode.ToString() : "?")}, " +
                    $"scaleFactor={(targetCanvas != null ? targetCanvas.scaleFactor.ToString("F3") : "?")})\n" +
                $"  target world corners: BL={worldCorners[0]}, TL={worldCorners[1]}, TR={worldCorners[2]}, BR={worldCorners[3]}\n" +
                $"  target local corners: BL={targetLocalCorners[0]}, TR={targetLocalCorners[2]}\n" +
                $"  target world CENTER       = {worldCenter}\n" +
                $"  target screen CENTER      = {targetScreenCenter}\n" +
                $"--- POINTER '{(pointerRt != null ? pointerRt.name : "<null>")}' ---\n" +
                $"  pointer.parent            = {(pointerParent != null ? pointerParent.name : "<null>")}\n" +
                $"  pointer.parent.lossyScale = {(pointerParent != null ? pointerParent.lossyScale.ToString() : "?")}\n" +
                $"  pointer.parentRect.rect   = {(pointerParentRt != null ? pointerParentRt.rect.ToString() : "<not a RectTransform>")}\n" +
                $"  pointer.canvas            = {(pointerCanvas != null ? pointerCanvas.name : "<null>")} " +
                    $"(mode={(pointerCanvas != null ? pointerCanvas.renderMode.ToString() : "?")})\n" +
                $"  pointer.anchorMin/Max     = {(pointerRt != null ? pointerRt.anchorMin.ToString() : "?")} / " +
                    $"{(pointerRt != null ? pointerRt.anchorMax.ToString() : "?")}\n" +
                $"  pointer.pivot             = {(pointerRt != null ? pointerRt.pivot.ToString() : "?")}\n" +
                $"  pointer.lossyScale        = {(pointerRt != null ? pointerRt.lossyScale.ToString() : "?")}\n" +
                $"  COMPUTED localInParent    = {computedLocal}\n" +
                $"  AFTER pointer.localPos    = {pointerLocalPos}\n" +
                $"  AFTER pointer.anchoredPos = {pointerAnchoredPos}\n" +
                $"  AFTER pointer.world pos   = {pointerWorldPos}\n" +
                $"  AFTER pointer.screen pos  = {pointerScreenPos}\n" +
                $"--- DELTA (pointer - target) ---\n" +
                $"  worldDelta                = {worldDelta} (mag={worldDelta.magnitude:F2})\n" +
                $"  screenDelta               = {screenDelta} (mag={screenDelta.magnitude:F2})\n" +
                $"  same canvas?              = {(targetCanvas != null && targetCanvas == pointerCanvas)}\n" +
                $"  same parent?              = {(target.parent == pointerParent)}"
            );
        }
        #endif

        private void SetPointerRotation(float angle)
        {
            focusPointerRotation.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void ShowFocusPointer()
        {
            focusPointerCenter.SetActive(true);
            rippleObj.SetActive(true);
        }

        public void HidePointer()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            focusPointerCenter.SetActive(false);
            rippleObj.SetActive(false);

            // remove position and rotation
            focusPointerRotation.rotation = Quaternion.identity;
            if (centerRectTransform != null)
            {
                centerRectTransform.anchoredPosition = Vector2.zero;
            }

            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
