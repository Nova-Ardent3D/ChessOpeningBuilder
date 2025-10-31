using UnityEngine;
using MoveTrainer.Move;
using Board;
using System.Collections.Generic;
using System.Linq;

namespace MoveTrainer
{
    public class Trainer : MonoBehaviour
    {
        public MoveInformationDisplay MoveInformationDisplayPrefab;

        public GameObject PreviousMoveContainer;
        public GameObject CurrentMoveContainer;
        public GameObject NextMoveContainer;

        MoveInformationDisplay PreviousMoveDisplay = null;
        MoveInformationDisplay CurrentMoveDisplay = null;
        List<MoveInformationDisplay> NextMovesDisplay = new List<MoveInformationDisplay>();

        MoveInformation StartingMove = null;
        MoveInformation CurrentMove = null;

        void CreateStartingMove()
        {
            MoveInformation moveInformation = new MoveInformation();
            moveInformation.Fen = BoardState.DefaultFEN;
            moveInformation.MoveNotation = "Start Position";

            CurrentMove = moveInformation;
            StartingMove = CurrentMove;

            UpdateViewedMove();
        }

        public void UpdateViewedMove()
        {
            GameObject.Destroy(PreviousMoveDisplay?.gameObject);
            GameObject.Destroy(CurrentMoveDisplay?.gameObject);
            foreach (var nextMoveDisplay in NextMovesDisplay ?? Enumerable.Empty<MoveInformationDisplay>())
            {
                GameObject.Destroy(nextMoveDisplay.gameObject);
            }

            PreviousMoveDisplay = null;
            NextMovesDisplay.Clear();

            if (CurrentMove.ParentMove != null)
            {
                PreviousMoveDisplay = MakeMoveDisplay(CurrentMove.ParentMove, PreviousMoveContainer.transform);
                PreviousMoveDisplay.SetCallBack(x =>
                {
                    CurrentMove = x;
                    UpdateViewedMove();
                });
            }

            CurrentMoveDisplay = MakeMoveDisplay(CurrentMove, CurrentMoveContainer.transform);
            for (int i = 0; i < CurrentMove.PossibleNextMoves.Count; i++)
            {
                var nextMove = CurrentMove.PossibleNextMoves[i];
                var nextMoveDisplay = MakeMoveDisplay(nextMove, NextMoveContainer.transform);
                NextMovesDisplay.Add(nextMoveDisplay);

                nextMoveDisplay.SetCallBack(x =>
                {
                    CurrentMove = x;
                    UpdateViewedMove();
                });
                nextMoveDisplay.transform.localPosition = new Vector3
                    ( 0
                    , ((MoveInformationDisplayPrefab.transform as RectTransform).rect.height + 5) * (0.5f + i - CurrentMove.PossibleNextMoves.Count / 2f)
                    , 0
                    );
            }
        }

        MoveInformationDisplay MakeMoveDisplay(MoveInformation moveInformation, Transform parent)
        {
            CurrentMoveDisplay = Instantiate(MoveInformationDisplayPrefab, parent);
            CurrentMoveDisplay.Init(moveInformation);
            CurrentMoveDisplay.transform.localPosition = Vector3.zero;
            return CurrentMoveDisplay;
        }

        public void AddVariation(IEnumerable<(string move, string fen)> moveInfo)
        {
            if (StartingMove == null)
            {
                CreateStartingMove();
            }

            bool brokenChain = false;
            MoveInformation currentMove = StartingMove;

            foreach (var (move, fen) in moveInfo)
            {
                if (!brokenChain && currentMove.PossibleNextMoves.Any(x => x.Fen == fen && x.MoveNotation == move))
                {
                    currentMove = currentMove.PossibleNextMoves.First(x => x.Fen == fen && x.MoveNotation == move);
                }
                else
                {
                    brokenChain = true;

                    MoveInformation newMoveInformation = new MoveInformation()
                    {
                        ParentMove = currentMove,
                        Fen = fen,
                        MoveNotation = move,
                    };

                    currentMove.PossibleNextMoves.Add(newMoveInformation);
                    currentMove = newMoveInformation;
                }
            }

            UpdateViewedMove();
        }
    }
}
