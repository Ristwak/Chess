using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [Tooltip("Tick if AI should play White; untick for Black.")]
    public bool aiPlaysWhite = false;

    [Tooltip("Seconds AI waits before making its move.")]
    public float moveDelay = 1f;

    [Tooltip("Reference to your GameController instance")]
    public GameController gameController;

    // Prevent multiple coroutines at once
    private bool isThinking = false;

    void Start()
    {
        if (gameController == null)
            gameController = FindObjectOfType<GameController>();
    }

    void Update()
    {
        // Determine if it's AI's turn
        bool isAITurn = aiPlaysWhite 
            ? gameController.WhiteTurn 
            : !gameController.WhiteTurn;

        if (isAITurn && !isThinking)
        {
            isThinking = true;
            StartCoroutine(ThinkAndMove());
        }
    }

    private IEnumerator ThinkAndMove()
    {
        // 1. small delay so player sees it's AI thinking
        yield return new WaitForSeconds(moveDelay);

        // 2. gather all AI pieces from the correct container
        var container = aiPlaysWhite 
            ? gameController.WhitePieces 
            : gameController.BlackPieces;

        var pieces = new List<PieceController>();
        foreach (Transform t in container.transform)
        {
            var pc = t.GetComponent<PieceController>();
            if (pc != null) pieces.Add(pc);
        }

        // 3. shuffle for randomness
        for (int i = 0; i < pieces.Count - 1; i++)
        {
            int r = Random.Range(i, pieces.Count);
            var tmp = pieces[i];
            pieces[i] = pieces[r];
            pieces[r] = tmp;
        }

        // 4. brute-force: for each piece, try every board square
        foreach (var pc in pieces)
        {
            Vector3 oldPos = pc.transform.position;

            foreach (Transform square in gameController.Board.transform)
            {
                var newPos = new Vector3(
                    square.position.x,
                    square.position.y,
                    oldPos.z);

                // Check validity
                if (pc.ValidateMovement(oldPos, newPos, out var enemy))
                {
                    // Perform the move
                    gameController.SelectPiece(pc.gameObject);
                    pc.MovePiece(newPos);

                    // Deselect and end turn immediately
                    gameController.DeselectPiece();
                    gameController.EndTurn();

                    isThinking = false;
                    yield break;
                }
            }
        }

        // 5. If no move found, still end turn
        gameController.EndTurn();
        isThinking = false;
    }
}
