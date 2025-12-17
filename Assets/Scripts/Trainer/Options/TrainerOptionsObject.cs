using Board;
using Board.Common;
using Board.Moves;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Trainer.Data;
using Trainer.Data.Moves;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static Trainer.Data.TrainerData;

namespace Trainer.Options
{
    public class TrainerOptionsObject : MonoBehaviour
    {
        [SerializeField] TrainerObejct _trainerObject;

        public TrainerData TrainerData = new TrainerData();

        public ReactiveOption<PieceColor> Color = new ReactiveOption<PieceColor>();
        public ReactiveOption<TrainerType> TrainerType = new ReactiveOption<TrainerType>();
        public ReactiveOption<StatsView> StatsView = new ReactiveOption<StatsView>();
        public ReactiveOption<TrainingMethod> TrainingMethod = new ReactiveOption<TrainingMethod>();

        public TMP_InputField DepthInputField;
        
        public TMP_InputField EvolutionAccelerationField;
        public GameObject EvolutionMode;

        public TextMeshProUGUI DepthText;

        void Awake()
        {
            Color.Init();
            TrainerType.Init();
            StatsView.Init();
            TrainingMethod.Init();

            Color.RegisterOnValueChanged((newValue) => { TrainerData.Color = newValue; });
            TrainerType.RegisterOnValueChanged(VariationTypeChanged);
            StatsView.RegisterOnValueChanged((newValue) => { TrainerData.StatsDisplay = newValue; });
            TrainingMethod.RegisterOnValueChanged((newValue) => { TrainerData.Method = newValue; });
        }

        void VariationTypeChanged(TrainerType newValue)
        {
            TrainerData.DepthType = newValue;
            EvolutionMode.gameObject.SetActive(newValue == TrainerData.TrainerType.EvolutionMode);
            if (DepthText != null)
            {
                switch (newValue)
                {
                    case TrainerData.TrainerType.EvolutionMode:
                    case TrainerData.TrainerType.MarathonMode:
                    case TrainerData.TrainerType.MarathonUnique:
                        DepthText.text = "Starting Depth";
                        break;
                    case TrainerData.TrainerType.ByCompleteVariation:
                        DepthText.text = "Variations";
                        break;
                    case TrainerData.TrainerType.ByMoveCount:
                        DepthText.text = "Max Depth";
                        break;
                }
            }
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
                    TrainerData = TrainerData.Deserialize(ReadStream(reader).GetEnumerator());

                    Color.Value = TrainerData.Color;
                    TrainerType.Value = TrainerData.DepthType;
                    StatsView.Value = TrainerData.StatsDisplay;
                    TrainingMethod.Value = TrainerData.Method;

                    DepthInputField.text = TrainerData.Depth.ToString();
                    EvolutionAccelerationField.text = TrainerData.EvolutionAcceleration.ToString();

                    _trainerObject.UpdateViewedMove(TrainerData.StartingMove);
                    StartCoroutine(_trainerObject.SetChain(TrainerData.StartingMove));
                }
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
                    var trainerData = TrainerData.Deserialize(ReadStream(reader).GetEnumerator());

                    List<List<TrainerMoveInformation>> moveInformations = new List<List<TrainerMoveInformation>>();
                    GetVariations(trainerData.StartingMove, moveInformations);

                    foreach (var variation in moveInformations)
                    {
                        AddVariation(variation);
                    }
                }

                _trainerObject.UpdateViewedMove(TrainerData.StartingMove);
                StartCoroutine(_trainerObject.SetChain(TrainerData.StartingMove));
            }
            , () =>
            {
                Debug.Log("Canceled");
            }
            , FileBrowser.PickMode.Files
            , false, "C:\\Users", "MyOpening"
            , title: "Load and Combine");
        }

        void GetVariations(TrainerMoveInformation move, List<List<TrainerMoveInformation>> moveInformations)
        {
            if (move.PossibleNextMoves.Count == 0)
            {
                moveInformations.Add(move.GetMoveChain());
            }

            foreach (var nextMove in move.PossibleNextMoves)
            {
                GetVariations(nextMove, moveInformations);
            }
        }

        void AddVariation(IEnumerable<TrainerMoveInformation> Variation)
        {
            bool brokenChain = false;
            TrainerMoveInformation currentMove = TrainerData.StartingMove;

            foreach (TrainerMoveInformation move in Variation)
            {
                if (!brokenChain && currentMove.PossibleNextMoves.Any(x => x.MoveNotation == move.MoveNotation))
                {
                    currentMove = currentMove.PossibleNextMoves.First(x => x.MoveNotation == move.ToString());
                }
                else
                {
                    brokenChain = true;

                    TrainerMoveInformation newMove = new TrainerMoveInformation()
                    {
                        ParentMove = currentMove,
                        MoveNotation = move.MoveNotation,
                        HintOne = move.HintOne,
                        HintTwo = move.HintTwo,
                        Color = move.Color
                    };

                    currentMove.PossibleNextMoves.Add(newMove);
                    currentMove = newMove;
                }
            }
        }

        IEnumerable<string> ReadStream(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                yield return reader.ReadLine();
            }
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

        public void OnEvolutionAccelerationChanged()
        {
            if (int.TryParse(EvolutionAccelerationField.text, out int value))
            {
                TrainerData.EvolutionAcceleration = value;
                if (value <= 0)
                {
                    EvolutionAccelerationField.text = "";
                    TrainerData.EvolutionAcceleration = 1;
                }
            }
            else
            {
                EvolutionAccelerationField.text = "";
                TrainerData.EvolutionAcceleration = 1;
            }
        }
    }
}
