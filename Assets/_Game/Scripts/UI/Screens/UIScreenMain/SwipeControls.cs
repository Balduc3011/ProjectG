using Balduu;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

public class SwipeControls : MonoBehaviour
    {
        public MenuNav menuNav;
        public RectTransform navMenu;
        public RectTransform swipeArea;
        public CanvasGroup swipeAreaCg;
        private float rootSwipeAreaPosX;
        private float rootSwipeAreaPosY;
        private Vector2 startTouchPosition;
        private Vector2 endTouchPosition;
        private bool dragging;
        private float draggingTime;
        private MotionHandle swipeAreaHandle;
        
        private void Update()
        {
            // no-op if not configured
            if (swipeArea == null) return;
            if(dragging)
                draggingTime += Time.deltaTime;

            // Use cross-platform pointer input (touch on mobile, mouse on PC)
            if (TryGetPointer(out TouchPhase phase, out Vector2 pointerPos) && phase == TouchPhase.Began)
            {
                if(pointerPos.y <= navMenu.rect.height)
                    return;
                startTouchPosition = pointerPos;
                dragging = true;
                rootSwipeAreaPosX = swipeArea.anchoredPosition.x;
                rootSwipeAreaPosY = swipeArea.anchoredPosition.y;
                CancelSwipeAreaMotionHandle();
                draggingTime = 0f;
            }

            if (TryGetPointer(out phase, out pointerPos) && phase == TouchPhase.Ended)
            {
                if(!dragging) 
                    return;
                endTouchPosition = pointerPos;
                dragging = false;
                Vector2 inputVector = endTouchPosition - startTouchPosition;
                if(CheckFastSwipe(inputVector))
                    return;
                UIScreenMain.Events.OnEndDragSwipeEvent?.Invoke();
            }
            SwipeAreaMove();
        }
        
        private bool CheckFastSwipe(Vector2 inputVector)
        {
            if(Mathf.Abs(inputVector.x) > Mathf.Abs(inputVector.y))
            {
                if(inputVector.x > 0)
                {
                    if(RightSwipe())
                        return true;
                }
                else
                {
                    if(LeftSwipe())
                        return true;
                }
            }
            else
            {
                if (inputVector.y > 0)
                {
                    UpSwipe();
                }
                else
                {
                    DownSwipe();
                }
            }
            return false;
        }

        private void UpSwipe()
        {
            
        }
        private void DownSwipe()
        {
            
        }
        private bool LeftSwipe()
        {
            if (draggingTime < 0.2f)
            {
                menuNav.LeftSwipe();
                return true;
            }
            return false;
        }
        private bool RightSwipe()
        {
            if (draggingTime < 0.2f)
            {
                menuNav.RightSwipe();
                return true;
            }
            return false;
        }
        
        private void SwipeAreaMove()
        {
            // allow movement for both touch and mouse while dragging
            if (dragging && TryGetPointer(out TouchPhase phase, out Vector2 pointerPos) && (phase == TouchPhase.Moved || phase == TouchPhase.Began))
            {
                var delta = pointerPos.x - startTouchPosition.x;
                // use anchoredPosition to match root values
                swipeArea.anchoredPosition = new Vector2(rootSwipeAreaPosX + delta, rootSwipeAreaPosY);
            }
        }

        public async UniTask AutoMoveSwipeArea(float from, float to)
        {
            CancelSwipeAreaMotionHandle();
            ActiveSwipeAreaCg(false);
            swipeAreaHandle = LMotion.Create(from, to, 0.2f).Bind(ChangeSwipeAreaPosX);
            await swipeAreaHandle;
            ActiveSwipeAreaCg(true);
        }
        private void ActiveSwipeAreaCg(bool active)
        {
            swipeAreaCg.interactable = active;
        }
        private void ChangeSwipeAreaPosX(float x)
        {
            swipeArea.anchoredPosition = new Vector2(x, swipeArea.anchoredPosition.y);
        }

        private void CancelSwipeAreaMotionHandle()
        {
            if (swipeAreaHandle.IsPlaying())
            {
                swipeAreaHandle.Cancel();
            }
        }

        // Returns true when there's a pointer (touch or mouse) and outputs a mapped TouchPhase and position
        private bool TryGetPointer(out TouchPhase phase, out Vector2 position)
        {
            // Mobile touch takes precedence if available
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                phase = t.phase;
                position = t.position;
                return true;
            }

            // Mouse support for PC / Editor
            if (Input.GetMouseButtonDown(0))
            {
                phase = TouchPhase.Began;
                position = Input.mousePosition;
                return true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                phase = TouchPhase.Ended;
                position = Input.mousePosition;
                return true;
            }
            if (Input.GetMouseButton(0))
            {
                // treat held mouse as Moved so swipe continues
                phase = TouchPhase.Moved;
                position = Input.mousePosition;
                return true;
            }

            phase = TouchPhase.Canceled;
            position = default;
            return false;
        }
    }