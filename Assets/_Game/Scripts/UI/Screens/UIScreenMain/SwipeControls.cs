using System.Collections;
using System.Collections.Generic;
using Balduu;
using UnityEngine;
using UnityEngine.UI;
public class SwipeControls : MonoBehaviour
{
    public RectTransform swipeArea;
    private float rootSwipeAreaPosX;
    private float rootSwipeAreaPosY;
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float dragTime;
    private bool draging;
    
    private void Update()
    {
        if(draging) 
            dragTime += Time.deltaTime;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            startTouchPosition = Input.GetTouch(0).position;
            dragTime = 0;
            draging = true;
            rootSwipeAreaPosX = swipeArea.position.x;
            rootSwipeAreaPosY = swipeArea.position.y;
            UIScreenMain.Events.OnStartDragSwipeEvent?.Invoke();
        }
        if(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            endTouchPosition = Input.GetTouch(0).position;

            Vector2 inputVector = endTouchPosition - startTouchPosition;
            if(Mathf.Abs(inputVector.x) > Mathf.Abs(inputVector.y))
            {
                if(inputVector.x > 0)
                {
                    RightSwipe();
                }
                else
                {
                    LeftSwipe();
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
            draging = false;
            dragTime = 0;
            UIScreenMain.Events.OnEndDragSwipeEvent?.Invoke();
        }
        SwipeAreaMove();
    }

    private void UpSwipe()
    {
        print("up");
    }
    private void DownSwipe()
    {
        print("down");
    }
    private void LeftSwipe()
    {
        print("left");
    }
    private void RightSwipe()
    {
        print("right");
    }
    
    private void SwipeAreaMove()
    {
        if (draging && Input.touchCount > 0)
        {
            var delta = Input.GetTouch(0).position.x - startTouchPosition.x;
            swipeArea.position = new Vector2(rootSwipeAreaPosX + delta, rootSwipeAreaPosY);
        }
    }
}