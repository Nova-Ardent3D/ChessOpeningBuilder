using Board.BoardMarkers;
using Board.History;
using MoveTrainer.Move;
using System.Collections.Generic;
using UnityEngine;
using static Board.BoardState;
using System.Collections;
using System.Linq;
using TMPro;

namespace MoveTrainer
{
    public class AutoTrainer : MonoBehaviour
    {
        public class Variation
        {
            public List<MoveInformation> Moves;
            public List<bool> IsMoveCorrect;
            public List<bool> HintUsed;
        }

        public BoardController boardController;
        public BoardHistory boardHistory;
        public GameObject nextVariationButton;
        public TextMeshProUGUI textMeshProUGUI;


        public GameObject[] DisableOnRun;
        public GameObject[] EnableOnRun;

        public int CurrentMove = 0;
        public Variation CurrentVariation { get; private set; }


        public List<Variation> Variations = new List<Variation>();

        public bool IsRunningFailedVariations = false;
        public List<Variation> FailedVaritions = new List<Variation>();

        public TrainerData TrainerData { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsUsersTurn 
        { 
            get => boardController.BoardState.CurrentMove == Board.BoardState.Move.White && TrainerData.IsWhiteTrainer
                || boardController.BoardState.CurrentMove == Board.BoardState.Move.Black && !TrainerData.IsWhiteTrainer;
        }

        public int TotalVariationCount { get; private set; }
        public int CurrentVariationIndex { get; private set; }

        private void Start()
        {
            boardController.SetMovePieceCallback(OnPieceMoved);
        }

        private void Update()
        {
            if (nextVariationButton.activeSelf && Input.GetKeyDown(KeyCode.Space))
            {
                SetNextVariation();
            }
        }

        public void Run(TrainerData trainerData)
        {
            Variations.Clear();

            BuildVariations(trainerData, trainerData.StartingMove, 0);
            if (Variations.Count == 0)
                return;

            TotalVariationCount = Variations.Count;
            CurrentVariationIndex = 0;


            foreach (var obj in DisableOnRun)
            {
                obj.SetActive(false);
            }

            foreach (var obj in EnableOnRun)
            {
                obj.SetActive(true);
            }


            boardController.SetBoardRotation(!trainerData.IsWhiteTrainer);

            TrainerData = trainerData;
            IsRunning = true;
            IsRunningFailedVariations = false;

            SetNextVariation();
        }

        public void CompleteRun()
        {
            foreach (var obj in EnableOnRun)
            {
                obj.SetActive(false);
            }

            foreach (var obj in DisableOnRun)
            {
                obj.SetActive(true);
            }

            TrainerData = null;
            IsRunning = false;

            boardHistory.ClearHistory();
            CurrentVariation = null;
            CurrentMove = 1;
            Variations.Clear();

            nextVariationButton.SetActive(false);
            textMeshProUGUI.gameObject.SetActive(false);

            IsRunningFailedVariations = true;
        }

        public void SetNextVariation()
        {
            boardHistory.ClearHistory();
            boardController.ClearAllHighlights();
            boardController.moveDisplayManager.ClearMarkers();
            boardController.rightClickData = new Board.MouseClickData.MouseData();
            boardController.leftClickData = new Board.MouseClickData.MouseData();
            boardController.HighlightedPiece = null;
            boardController.HighlightedPieceMoves = null;

            int next = Random.Range(0, Variations.Count);
            CurrentVariation = Variations[next];
            Variations.RemoveAt(next);

            CurrentMove = 1;

            nextVariationButton.SetActive(false);
            
            CurrentVariationIndex++;

            if (IsRunningFailedVariations)
            {
                textMeshProUGUI.text = $"Failed Variation {CurrentVariationIndex} / {TotalVariationCount}";
            }
            else
            {
                textMeshProUGUI.text = $"Variation {CurrentVariationIndex} / {TotalVariationCount}";
            }
            textMeshProUGUI.gameObject.SetActive(true);

            if (!TrainerData.IsWhiteTrainer)
            {
                StartCoroutine(BotMove());
            }
        }

        void BuildVariations(TrainerData trainerData, MoveInformation currentMove, int depth)
        {
            currentMove.TimesCorrect = 0;
            currentMove.TimesGuessed = 0;

            if (depth == trainerData.Depth && trainerData.DepthType == TrainerData.TrainerType.DepthFirst)
            {
                Variations.Add(GetMovesToLeaf(currentMove));
                return;
            }

            if (currentMove.PossibleNextMoves.Count == 0)
            {
                if (trainerData.DepthType == TrainerData.TrainerType.BreadthFirst && Variations.Count == trainerData.Depth && trainerData.Depth > 0)
                {
                    return;
                }
                else
                {
                    Variations.Add(GetMovesToLeaf(currentMove));
                }
                return;
            }

            foreach (var nextMove in currentMove.PossibleNextMoves)
            {
                BuildVariations(trainerData, nextMove, depth + 1);
            }
        }

        Variation GetMovesToLeaf(MoveInformation leaf)
        {
            List<MoveInformation> moves = new List<MoveInformation>();
            MoveInformation current = leaf;
            while (current != null)
            {
                moves.Add(current);
                current = current.ParentMove;
            }

            moves.Reverse();
            return new Variation()
            {
                Moves = moves,
                IsMoveCorrect = new List<bool>(moves.Select(x => true)),
                HintUsed = new List<bool>(moves.Select(x => false)),
            };
        }

        void OnPieceMoved()
        {
            if (nextVariationButton.activeSelf)
            {
                return;
            }

            if (!IsRunning)
            {
                return;
            }

            // this happens after the move, so it is reversed.
            if (IsUsersTurn)
            {

            }
            else
            {
                string latestMove = boardHistory.GetLatestMove().ToString();
                string variationMove = CurrentVariation.Moves[CurrentMove].ToString();

                if (latestMove != variationMove)
                {
                    boardHistory.RemoveLastMove();
                    CurrentVariation.IsMoveCorrect[CurrentMove] = false;
                }
                else
                {
                    CurrentVariation.Moves[CurrentMove].TimesGuessed++;
                    if (!CurrentVariation.IsMoveCorrect[CurrentMove])
                    {
                        boardHistory.GetLatestMoveLabel().SetColorToFailed();
                    }
                    else
                    {
                        CurrentVariation.Moves[CurrentMove].TimesCorrect++;
                    }

                    CurrentMove++;
                    if (CurrentMove >= CurrentVariation.Moves.Count)
                    {
                        VariationComplete();
                    }
                    else
                    {
                        StartCoroutine(BotMove());
                    }
                }
            }
        }

        IEnumerator BotMove()
        {
            yield return new WaitForSeconds(0.25f);
            boardController.BoardState.MovePiece
                ( CurrentVariation.Moves[CurrentMove].ToString()
                , boardController.BoardState.CurrentMove == Board.BoardState.Move.White
                , false
                , true);

            CurrentVariation.Moves[CurrentMove].TimesCorrect++;
            CurrentVariation.Moves[CurrentMove].TimesGuessed++;
            CurrentVariation.IsMoveCorrect[CurrentMove] = true;

            Board.History.Move botsMove = boardHistory.GetLatestMove();
            boardController.AnimatePieceMove
                ( (int)botsMove.ToFile
                , (int)botsMove.ToRank
                , (int)botsMove.FromFile
                , (int)botsMove.FromRank
                , (int)botsMove.ToFile
                , (int)botsMove.ToRank
                , botsMove.GetMoveAudio()
                );

            CurrentMove++;

            if (CurrentMove >= CurrentVariation.Moves.Count)
            {
                VariationComplete();
            }
        }

        void VariationComplete()
        {
            if (TrainerData.Method == TrainerData.TrainingMethod.ReplayFailedMoves && !IsRunningFailedVariations)
            {
                if (CurrentVariation.IsMoveCorrect.Any(x => !x))
                {
                    FailedVaritions.Add(CurrentVariation);
                }
            }

            if (TrainerData.Method == TrainerData.TrainingMethod.RepeatVariationOnFailed)
            {
                if (!IsRunningFailedVariations && CurrentVariation.IsMoveCorrect.Any(x => !x))
                {
                    CurrentMove = 1;
                    boardHistory.ClearHistory();
                    IsRunningFailedVariations = true;

                    if (!TrainerData.IsWhiteTrainer)
                    {
                        StartCoroutine(BotMove());
                    }
                    return;
                }
                IsRunningFailedVariations = false;
            }

            if (Variations.Count == 0)
            {
                if (TrainerData.Method == TrainerData.TrainingMethod.ReplayFailedMoves && !IsRunningFailedVariations)
                {
                    Variations.AddRange(FailedVaritions);
                    FailedVaritions.Clear();
                    IsRunningFailedVariations = true;
                    CurrentVariationIndex = 0;
                    TotalVariationCount = Variations.Count;
                    nextVariationButton.SetActive(true);
                }
                else
                {
                    CompleteRun();
                }
            }
            else
            {
                nextVariationButton.SetActive(true);
            }
        }

        public void GetHint()
        {
            if (!IsRunning)
            {
                return;
            }

            if (CurrentMove >= CurrentVariation.IsMoveCorrect.Count)
            {
                return;
            }

            CurrentVariation.IsMoveCorrect[CurrentMove] = false;

            if (!CurrentVariation.HintUsed[CurrentMove])
            {
                CurrentVariation.HintUsed[CurrentMove] = true;

                string hint = CurrentVariation.Moves[CurrentMove].HintOne;
                int file = hint[0] - 'A';
                int rank = hint[1] - '1';

                boardController.highlighting.Highlight(file, rank, true);
            }
            else
            {
                string hint1 = CurrentVariation.Moves[CurrentMove].HintOne;
                int file1 = hint1[0] - 'A';
                int rank1 = hint1[1] - '1';

                string hint2 = CurrentVariation.Moves[CurrentMove].HintTwo;
                int file2 = hint2[0] - 'A';
                int rank2 = hint2[1] - '1';

                boardController.highlighting.Highlight(file1, rank1, true);
                boardController.arrows.UpdateArrow(new Board.MouseClickData.MouseData()
                {
                    FromPosition = new Vector2Int(file1, rank1),
                    ToPosition = new Vector2Int(file2, rank2),
                });
            }
        }
    }
}
