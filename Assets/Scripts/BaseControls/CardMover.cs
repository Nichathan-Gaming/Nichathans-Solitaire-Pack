using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Moves a card from one place to another
 */
public class CardMover : MonoBehaviour
{
    public bool isActive;

    private Vector3 moveToLocalPosition;

    private float startTime = 0;

    //The minimum distance that we move a card at
    const float MIN_DISTANCE = 30;

    Canvas canvas;

    private void Update()
    {
        if (isActive && moveToLocalPosition!=null && Time.time > startTime)
        {
            Vector3 step = GetModifiedMoveDistance(transform.localPosition, moveToLocalPosition);

            //if we shouldn't move
            if (step.Equals(Vector3.zero))
            {
                //make position exact
                transform.localPosition = moveToLocalPosition;

                //deactiate the move
                isActive = false;

                Destroy(canvas);
            }
            else
            {
                transform.position += step;
            }
        }
    }

    public void MoveTo(Vector3 moveToLocalPosition)
    {
        this.moveToLocalPosition = moveToLocalPosition;
        isActive = true;

        AddCanvas();
    }

    public void MoveTo(Vector3 moveToLocalPosition, float startTime)
    {
        this.startTime = startTime;
        this.moveToLocalPosition = moveToLocalPosition;
        isActive = true;

        AddCanvas();
    }

    private void AddCanvas()
    {
        if (canvas != null) return;
        canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1;
    }

    private Vector3 GetModifiedMoveDistance(Vector3 from, Vector3 to)
    {
        float newX = to.x - from.x,
            newY = to.y - from.y;

        bool isNegX = false,
            isNegY = false;

        if (newX < 0)
        {
            newX = -newX;
            isNegX = true;
        }

        if (newY < 0)
        {
            newY = -newY;
            isNegY = true;
        }

        float difference = 1;

        float moveX = (newX > MIN_DISTANCE) ? (isNegX ? (-MIN_DISTANCE) : MIN_DISTANCE) : 0,
            moveY = (newY > MIN_DISTANCE) ? (isNegY ? (-MIN_DISTANCE) : MIN_DISTANCE) : 0;

        bool findDiff = true;

        //see which is greater - with absolute value
        if (newX == 0 || newY == 0)
        {
            if (newX == 0 && newY == 0)
            {
                return Vector3.zero;
            }

            findDiff = false;
        }

        if (newX > newY)
        {
            if (findDiff) difference = newY / newX;

            //mult diff with newY
            return new Vector3(moveX, (moveY * difference));
        }
        else
        {
            if (findDiff) difference = newX / newY;

            //mult diff with newX
            return new Vector3((moveX * difference), moveY);
        }
    }
}

