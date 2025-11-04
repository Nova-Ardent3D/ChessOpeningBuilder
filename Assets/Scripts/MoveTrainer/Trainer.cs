using Board;
using Board.BoardMarkers;
using Board.History;
using MoveTrainer.Move;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static MoveTrainer.AutoTrainer;
using static MoveTrainer.TrainerData;

namespace MoveTrainer
{
    public class Trainer : MonoBehaviour
    {
        public BoardController BoardController;
        public BoardHistory BoardHistory;
        public AutoTrainer AutoTrainer;

        public MoveInformationDisplay MoveInformationDisplayPrefab;
        public TMP_Dropdown TypeDropDown;
        public TMP_InputField DepthInputField;
        public TMP_Dropdown DepthTypeDropDown;
        public TMP_Dropdown StatsTypeDropDown;

        public GameObject PreviousMoveContainer;
        public GameObject CurrentMoveContainer;
        public GameObject NextMoveContainer;

        MoveInformationDisplay PreviousMoveDisplay = null;
        MoveInformationDisplay CurrentMoveDisplay = null;
        List<MoveInformationDisplay> NextMovesDisplay = new List<MoveInformationDisplay>();

        TrainerData TrainerData = new TrainerData();
        MoveInformation CurrentMove = null;

        void CreateStartingMove()
        {
            MoveInformation moveInformation = new MoveInformation();
            moveInformation.MoveNotation = "Start Position";

            CurrentMove = moveInformation;
            TrainerData.StartingMove = CurrentMove;

            UpdateViewedMove();
        }

        public void UpdateViewedMove()
        {
            GameObject.Destroy(PreviousMoveDisplay?.gameObject);
            GameObject.Destroy(CurrentMoveDisplay?.gameObject);
            foreach (var nextMoveDisplay in NextMovesDisplay)
            {
                GameObject.Destroy(nextMoveDisplay.gameObject);
            }

            PreviousMoveDisplay = null;
            NextMovesDisplay.Clear();

            if (CurrentMove.ParentMove != null)
            {
                PreviousMoveDisplay = MakeMoveDisplay(CurrentMove.ParentMove, PreviousMoveContainer.transform);
                PreviousMoveDisplay.name = "PreviousMoveDisplay";
                PreviousMoveDisplay.SetCallBack(x =>
                {
                    CurrentMove = x;
                    UpdateViewedMove();
                });
            }

            CurrentMoveDisplay = MakeMoveDisplay(CurrentMove, CurrentMoveContainer.transform);
            CurrentMoveDisplay.name = "CurrentMoveDisplay";
            CurrentMoveDisplay.SetCallBack(x =>
            {
                BoardHistory.ClearHistory();

                List<MoveInformation> moves = new List<MoveInformation>();
                MoveInformation current = x;
                while (current != null)
                {
                    moves.Add(current);
                    current = current.ParentMove;
                }

                moves.Reverse();
                bool isWhite = true;
                foreach (var move in moves.Skip(1))
                {
                    BoardController.BoardState.MovePiece(move.MoveNotation, isWhite, false, true);
                    isWhite = !isWhite;
                }

                Board.History.Move lastMove = BoardHistory.GetLatestMove();
                if (lastMove != null)
                {
                    BoardController.highlighting.SetLastMove
                        ( new Vector2Int((int)lastMove.FromFile, (int)lastMove.FromRank)
                        , new Vector2Int((int)lastMove.ToFile, (int)lastMove.ToRank)
                        );
                }
            });

            for (int i = 0; i < CurrentMove.PossibleNextMoves.Count; i++)
            {
                var nextMove = CurrentMove.PossibleNextMoves[i];
                var nextMoveDisplay = MakeMoveDisplay(nextMove, NextMoveContainer.transform);
                nextMoveDisplay.name = "NextMoveDisplay_" + i;
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
            var nextMove = Instantiate(MoveInformationDisplayPrefab, parent);
            nextMove.Init(moveInformation);
            nextMove.transform.localPosition = Vector3.zero;

            if (TrainerData.StatsDisplay == StatsView.ByMove)
            {
                if (moveInformation.TimesGuessed > 0)
                {
                    nextMove.PercentageBar.Percentage = 1.0f * moveInformation.TimesCorrect / moveInformation.TimesGuessed;
                }
            }
            else
            {
                int timesGuessed = 0;
                int timesCorrect = 0;
                GetStatsOfBranch(ref timesGuessed, ref timesCorrect, moveInformation);

                if (timesGuessed > 0)
                {
                    nextMove.PercentageBar.Percentage = 1.0f * timesCorrect / timesGuessed;
                }
            }
            return nextMove;
        }

        void GetStatsOfBranch(ref int guessed, ref int correct, MoveInformation moveInformation)
        {
            guessed += moveInformation.TimesGuessed;
            correct += moveInformation.TimesCorrect;

            foreach (var move in moveInformation.PossibleNextMoves)
            {
                GetStatsOfBranch(ref guessed, ref correct, move);
            }
        }

        public void AddVariation(IEnumerable<(string move, string hint1, string hint2)> moveInfo)
        {
            if (TrainerData.StartingMove == null)
            {
                CreateStartingMove();
            }

            List<(string move, string hint1, string hint2)> test = moveInfo.ToList();

            bool brokenChain = false;
            MoveInformation currentMove = TrainerData.StartingMove;

            foreach (var (move, hint1, hint2) in moveInfo)
            {
                if (!brokenChain && currentMove.PossibleNextMoves.Any(x => x.MoveNotation == move))
                {
                    currentMove = currentMove.PossibleNextMoves.First(x => x.MoveNotation == move);
                }
                else
                {
                    brokenChain = true;

                    MoveInformation newMoveInformation = new MoveInformation()
                    {
                        ParentMove = currentMove,
                        MoveNotation = move,
                        HintOne = hint1,
                        HintTwo = hint2,
                    };

                    currentMove.PossibleNextMoves.Add(newMoveInformation);
                    currentMove = newMoveInformation;
                }
            }

            UpdateViewedMove();
        }

        public void Save()
        {
            if (FileBrowser.IsOpen)
                return;

            FileBrowser.SetFilters(true, new FileBrowser.Filter("Openings", ".open"));
            FileBrowser.SetDefaultFilter(".open");
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            FileBrowser.ShowSaveDialog((paths) =>
            {
                if (TrainerData.StartingMove == null)
                {
                    return;
                }

                using (StreamWriter writer = new StreamWriter(paths[0], false))
                {
                    foreach (var line in TrainerData.Serialize(TrainerData))
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            , () =>
            {
                Debug.Log("Canceled");
            }
            , FileBrowser.PickMode.Files
            , false, "C:\\Users", "MyOpening");
        }

        public void Load()
        {
            if (FileBrowser.IsOpen)
                return;

            FileBrowser.SetFilters(true, new FileBrowser.Filter("Openings", ".open"));
            FileBrowser.SetDefaultFilter(".open");
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            FileBrowser.ShowLoadDialog((paths) =>
            {
                using (StreamReader reader = new StreamReader(paths[0]))
                {
                    TrainerData = TrainerData.Deserialize(ReadStreamer(reader).GetEnumerator());
                    CurrentMove = TrainerData.StartingMove;

                    TypeDropDown.value = TrainerData.IsWhiteTrainer ? 0 : 1;
                    DepthInputField.text = TrainerData.Depth.ToString();
                    DepthTypeDropDown.value = (int)TrainerData.DepthType;
                    StatsTypeDropDown.value = (int)TrainerData.StatsDisplay;
                }

                UpdateViewedMove();
            }
            , () =>
            {
                Debug.Log("Canceled");
            }
            , FileBrowser.PickMode.Files
            , false, "C:\\Users", "MyOpening");
        }

        public void LoadAndCombine()
        {
            if (FileBrowser.IsOpen)
                return;

            FileBrowser.SetFilters(true, new FileBrowser.Filter("Openings", ".open"));
            FileBrowser.SetDefaultFilter(".open");
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            FileBrowser.ShowLoadDialog((paths) =>
            {
                using (StreamReader reader = new StreamReader(paths[0]))
                {
                    var trainerData = TrainerData.Deserialize(ReadStreamer(reader).GetEnumerator());

                    List<List<MoveInformation>> moveInformations = new List<List<MoveInformation>>();
                    GetVariations(trainerData.StartingMove, moveInformations);
                    
                    foreach (var variation in moveInformations)
                    {
                        AddVariation(variation.Skip(1).Select(x => (x.MoveNotation, x.HintOne, x.HintTwo)));
                    }
                }

                UpdateViewedMove();
            }
            , () =>
            {
                Debug.Log("Canceled");
            }
            , FileBrowser.PickMode.Files
            , false, "C:\\Users", "MyOpening"
            , title : "Load and Combine");
        }

        void GetVariations(MoveInformation move, List<List<MoveInformation>> moveInformations)
        {
            if (move.PossibleNextMoves.Count == 0)
            {
                moveInformations.Add(GetMovesToLeaf(move));
            }

            foreach (var nextMove in move.PossibleNextMoves)
            {
                GetVariations(nextMove, moveInformations);
            }
        }

        List<MoveInformation> GetMovesToLeaf(MoveInformation leaf)
        {
            List<MoveInformation> moves = new List<MoveInformation>();
            MoveInformation current = leaf;
            while (current != null)
            {
                moves.Add(current);
                current = current.ParentMove;
            }

            moves.Reverse();
            return moves;
        }

        public void Run()
        {
            AutoTrainer.Run(TrainerData);
        }

        public void OnOpeningTypeChanged()
        {
            TrainerData.IsWhiteTrainer = TypeDropDown.value == 0;
        }

        public void OnDepthChanged()
        {
            if (int.TryParse(DepthInputField.text, out int value))
            {
                TrainerData.Depth = value;
                if (value <= 0)
                {
                    DepthInputField.text = "";
                    TrainerData.Depth = -1;
                }
            }
            else
            {
                DepthInputField.text = "";
                TrainerData.Depth = -1;
            }
        }

        public void OnDepthTypeChanged()
        {
            TrainerData.DepthType = (TrainerType)DepthTypeDropDown.value;
        }

        public void DeleteCurrentMoveAndChildren()
        {
            if (CurrentMove.ParentMove != null)
            {
                var move = CurrentMove;
                CurrentMove = CurrentMove.ParentMove;
                CurrentMove.PossibleNextMoves.Remove(move);
            }
            else
            {
                CurrentMove.PossibleNextMoves.Clear();
            }

            UpdateViewedMove();
        }

        public void StatsTypeChanged()
        {
            TrainerData.StatsDisplay = (StatsView)StatsTypeDropDown.value;
            UpdateViewedMove();
        }

        IEnumerable<string> ReadStreamer(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                yield return reader.ReadLine();
            }
        }
    }
}
