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
                BoardController.highlighting.SetLastMove
                    ( new Vector2Int((int)lastMove.FromFile, (int)lastMove.FromRank)
                    , new Vector2Int((int)lastMove.ToFile, (int)lastMove.ToRank)
                    );
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
            if (moveInformation.TimesGuessed > 0)
            {
                nextMove.PercentageBar.Percentage = 1.0f * moveInformation.TimesCorrect / moveInformation.TimesGuessed;
            }
            return nextMove;
        }

        public void AddVariation(IEnumerable<(string move, string fen)> moveInfo)
        {
            if (TrainerData.StartingMove == null)
            {
                CreateStartingMove();
            }

            bool brokenChain = false;
            MoveInformation currentMove = TrainerData.StartingMove;

            foreach (var (move, fen) in moveInfo)
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

        IEnumerable<string> ReadStreamer(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                yield return reader.ReadLine();
            }
        }
    }
}
