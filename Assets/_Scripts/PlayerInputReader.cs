using UnityEngine;

public readonly struct PlayerInputSignal
{
    public readonly Vector2Int DigitalMove;
    public readonly Vector2 AnalogMove;
    public readonly Vector2Int FirstHeldMove;
    public readonly Vector2Int SecondHeldMove;
    public readonly bool HasMove;
    public readonly bool RuaaaPressed;

    public PlayerInputSignal(Vector2Int digitalMove, Vector2 analogMove, Vector2Int firstHeldMove, Vector2Int secondHeldMove, bool ruaaaPressed)
    {
        DigitalMove = digitalMove;
        AnalogMove = analogMove;
        FirstHeldMove = firstHeldMove;
        SecondHeldMove = secondHeldMove;
        HasMove = digitalMove != Vector2Int.zero;
        RuaaaPressed = ruaaaPressed;
    }
}

public static class PlayerInputReader
{
    private static readonly Vector2Int[] heldMoveOrder = new Vector2Int[4];
    private static int heldMoveCount;

    public static PlayerInputSignal Read()
    {
        UpdateHeldMoveOrder();

        Vector2Int digital = Vector2Int.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) digital.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) digital.y -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) digital.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) digital.x += 1;

        Vector2 analog = digital;
        if (analog.sqrMagnitude > 1f) analog.Normalize();

        GetFirstTwoActiveMoves(digital, out Vector2Int firstMove, out Vector2Int secondMove);
        return new PlayerInputSignal(digital, analog, firstMove, secondMove, Input.GetKeyDown(KeyCode.F2));
    }

    private static void UpdateHeldMoveOrder()
    {
        RemoveReleasedMoves();

        AddMoveIfPressedThisFrame(Vector2Int.up, KeyCode.W, KeyCode.UpArrow);
        AddMoveIfPressedThisFrame(Vector2Int.down, KeyCode.S, KeyCode.DownArrow);
        AddMoveIfPressedThisFrame(Vector2Int.left, KeyCode.A, KeyCode.LeftArrow);
        AddMoveIfPressedThisFrame(Vector2Int.right, KeyCode.D, KeyCode.RightArrow);
    }

    private static void RemoveReleasedMoves()
    {
        for (int i = heldMoveCount - 1; i >= 0; i--)
        {
            if (!IsMoveHeld(heldMoveOrder[i]))
            {
                RemoveAt(i);
            }
        }
    }

    private static void AddMoveIfPressedThisFrame(Vector2Int move, KeyCode primaryKey, KeyCode alternateKey)
    {
        if (!Input.GetKeyDown(primaryKey) && !Input.GetKeyDown(alternateKey)) return;

        int existingIndex = IndexOf(move);
        if (existingIndex >= 0)
        {
            RemoveAt(existingIndex);
        }

        if (heldMoveCount >= heldMoveOrder.Length)
        {
            RemoveAt(0);
        }

        heldMoveOrder[heldMoveCount] = move;
        heldMoveCount++;
    }

    private static void GetFirstTwoActiveMoves(Vector2Int digitalMove, out Vector2Int firstMove, out Vector2Int secondMove)
    {
        firstMove = Vector2Int.zero;
        secondMove = Vector2Int.zero;

        for (int i = 0; i < heldMoveCount; i++)
        {
            Vector2Int move = heldMoveOrder[i];
            if (!ContributesToDigitalMove(move, digitalMove)) continue;

            if (firstMove == Vector2Int.zero)
            {
                firstMove = move;
            }
            else if (secondMove == Vector2Int.zero && move != firstMove)
            {
                secondMove = move;
                return;
            }
        }
    }

    private static bool ContributesToDigitalMove(Vector2Int move, Vector2Int digitalMove)
    {
        if (move.x != 0) return digitalMove.x == move.x;
        if (move.y != 0) return digitalMove.y == move.y;
        return false;
    }

    private static bool IsMoveHeld(Vector2Int move)
    {
        if (move == Vector2Int.up) return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        if (move == Vector2Int.down) return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (move == Vector2Int.left) return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        if (move == Vector2Int.right) return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        return false;
    }

    private static int IndexOf(Vector2Int move)
    {
        for (int i = 0; i < heldMoveCount; i++)
        {
            if (heldMoveOrder[i] == move) return i;
        }

        return -1;
    }

    private static void RemoveAt(int index)
    {
        heldMoveCount--;
        for (int i = index; i < heldMoveCount; i++)
        {
            heldMoveOrder[i] = heldMoveOrder[i + 1];
        }

        heldMoveOrder[heldMoveCount] = Vector2Int.zero;
    }
}
