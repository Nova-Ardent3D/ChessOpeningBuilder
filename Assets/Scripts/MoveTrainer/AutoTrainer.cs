using Board.BoardMarkers;
using Board.History;
using MoveTrainer.Move;
using System.Collections.Generic;
using UnityEngine;
using static Board.BoardState;
using System.Collections;
using System.Linq;

namespace MoveTrainer
{
    public class AutoTrainer : MonoBehaviour
    {
        public class Variation
        {
            public List<MoveInformation> Moves;
            public List<bool> IsMoveCorrect;
        }

        public BoardController boardController;
        public BoardHistory boardHistory;
        public GameObject nextVariationButton;

        public GameObject[] DisableOnRun;
        public GameObject[] EnableOnRun;

        public int CurrentMove = 0;
        public Variation CurrentVariation { get; private set; }


        public List<Variation> Variations = new List<Variation>();

        public TrainerData TrainerData { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsUsersTurn 
        { 
            get => boardController.BoardState.CurrentMove == Board.BoardState.Move.White && TrainerData.IsWhiteTrainer
                || boardController.BoardState.CurrentMove == Board.BoardState.Move.Black && !TrainerData.IsWhiteTrainer;
        }

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


            foreach (var obj in DisableOnRun)
            {
                obj.SetActive(false);
            }

            foreach (var obj in EnableOnRun)
            {
                obj.SetActive(true);
            }

            if (!trainerData.IsWhiteTrainer)
            {
                boardController.SetBoardRotation(true);
            }

            TrainerData = trainerData;
            IsRunning = true;

            SetNextVariation();

            if (!trainerData.IsWhiteTrainer)
            {
                StartCoroutine(BotMove());
            }
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
        }

        public void SetNextVariation()
        {
            boardHistory.ClearHistory();

            int next = Random.Range(0, Variations.Count);
            CurrentVariation = Variations[next];
            Variations.RemoveAt(next);

            CurrentMove = 1;

            nextVariationButton.SetActive(false);
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
                IsMoveCorrect = new List<bool>(moves.Select(x => true))
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
                CurrentVariation.Moves[CurrentMove].TimesGuessed++;

                string latestMove = boardHistory.GetLatestMove().ToString();
                string variationMove = CurrentVariation.Moves[CurrentMove].ToString();

                if (latestMove != variationMove)
                {
                    boardHistory.RemoveLastMove();
                    CurrentVariation.IsMoveCorrect[CurrentMove] = false;
                }
                else
                {
                    CurrentVariation.Moves[CurrentMove].TimesCorrect++;
                    if (!CurrentVariation.IsMoveCorrect[CurrentMove])
                    {
                        boardHistory.GetLatestMoveLabel().SetColorToFailed();
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
            if (Variations.Count == 0)
            {
                CompleteRun();
            }
            else
            {
                nextVariationButton.SetActive(true);
            }
        }
    }
}
