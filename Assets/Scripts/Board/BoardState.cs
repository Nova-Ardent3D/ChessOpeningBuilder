using Board.Audio;
using Board.History;
using Board.Pieces;
using Board.Pieces.Moves;
using System.Collections.Generic;
using UnityEngine;
using static Board.Pieces.Piece;
using System.Linq;
using System.Net.NetworkInformation;
using Board.BoardMarkers.Promotion;
using System;
using Unity.VisualScripting;

namespace Board
{
    public class BoardState
    {
        public enum Move
        {
            White,
            Black
        }

        public enum CastlingRights
        {
            None = 0,
            KingSide = 1 << 0,
            QueenSide = 1 << 1,
            Both = KingSide | QueenSide,
        }

        public const string DefaultFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        MoveAudio _moveAudio;

        PiecePrefabs _piecesPrefabs;
        RectTransform _piecesContainer;

        public Move CurrentMove { get; private set; }
        public CastlingRights WhiteCastlingRights { get; private set; }
        public CastlingRights BlackCastlingRights { get; private set; }

        Piece[,] _pieces = new Piece[8, 8];
        King _blackKing;
        King _whiteKing;

        Pawn _enPassantPawn;

        BoardHistory _boardHistory;

        public Piece[,] Pieces => _pieces;

        public BoardState(BoardHistory boardHistory, MoveAudio moveAudio, RectTransform piecesContainer, PiecePrefabs piecePrefabs)
        {
            _piecesContainer = piecesContainer;
            _piecesPrefabs = piecePrefabs;
            _moveAudio = moveAudio;
            _boardHistory = boardHistory;
            _boardHistory._boardState = this;
        }

        public Piece GetPieceInfo(out IEnumerable<MoveData> moveData, int file, int rank)
        {
            if (_pieces[file, rank] == null)
            {
                moveData = null;
                return null;
            }

            if (CurrentMove == Move.White && !_pieces[file, rank].IsWhite)
            {
                moveData = null;
                return null;
            }

            if (CurrentMove == Move.Black && _pieces[file, rank].IsWhite)
            {
                moveData = null;
                return null;
            }

            moveData = ValidateMoves(file, rank, _pieces[file, rank].GetMoves(this));
            return _pieces[file, rank];
        }

        public void Restart()
        {
            SetFEN(_boardHistory.StartingFen);
        }

        public void SetStartingFEN(string fen)
        {
            _boardHistory.StartingFen = fen;
            SetFEN(fen);
        }

        public void SetFEN(string fen)
        {
            ClearBoard();

            string[] split = fen.Split(new[] { ' ' }, 2);
            string boardFen = split.Length > 1 ? split[0] : fen;
            string gameStateFen = split.Length > 1 ? split[1] : "";

            int rank = 7;
            int file = 0;
            foreach (char c in boardFen)
            {
                if (c == ' ')
                {
                    break;
                }

                if (PiecePrefabs.PieceIDs.Contains(c))
                {
                    Piece prefab = _piecesPrefabs.GetPiece(c);

                    if (prefab != null)
                    {
                        Piece piece = GameObject.Instantiate<Piece>(prefab, _piecesContainer);
                        piece.CurrentRank = (Piece.Rank)rank;
                        piece.CurrentFile = (Piece.File)file;
                        _pieces[file, rank] = piece;

                        if (piece is King king)
                        {
                            if (king.IsWhite)
                            {
                                _whiteKing = king;
                            }
                            else
                            {
                                _blackKing = king;
                            }
                        }
                    }
                    file++;
                }
                else if (c == '/')
                {
                    rank--;
                    file = 0;
                }
                else if (char.IsDigit(c))
                {
                    file += (int)char.GetNumericValue(c);
                }
            }

            foreach (char c in gameStateFen)
            {
                if (c == 'w' || c == 'W')
                {
                    CurrentMove = Move.White;
                }
                else if (c == 'b' || c == 'B')
                {
                    CurrentMove = Move.Black;
                }
                else if (c == 'K' && _pieces[7, 0] is Rook && _pieces[7, 0].IsWhite && _whiteKing?.CurrentRank == Piece.Rank.One && _whiteKing?.CurrentFile == Piece.File.E)
                {
                    WhiteCastlingRights |= CastlingRights.KingSide;
                }
                else if (c == 'Q' && _pieces[0, 0] is Rook && _pieces[0, 0].IsWhite && _whiteKing?.CurrentRank == Piece.Rank.One && _whiteKing?.CurrentFile == Piece.File.E)
                {
                    WhiteCastlingRights |= CastlingRights.QueenSide;
                }
                else if (c == 'k' && _pieces[7, 7] is Rook && !_pieces[7, 7].IsWhite && _blackKing?.CurrentRank == Piece.Rank.Eight && _blackKing?.CurrentFile == Piece.File.E)
                {
                    BlackCastlingRights |= CastlingRights.KingSide;
                }
                else if (c == 'q' && _pieces[0, 7] is Rook && !_pieces[0, 7].IsWhite && _blackKing?.CurrentRank == Piece.Rank.Eight && _blackKing?.CurrentFile == Piece.File.E)
                {
                    BlackCastlingRights |= CastlingRights.QueenSide;
                }
            }

            for (int i = 0; i < gameStateFen.Length - 1; i++)
            {
                if (char.IsLetter(gameStateFen[i]) && char.IsNumber(gameStateFen[i + 1]))
                {
                    char enPassantfile = char.ToLower(gameStateFen[i]);
                    char enPassantrank = gameStateFen[i + 1];

                    if (CurrentMove == Move.White)
                    {
                        enPassantrank--;
                    }
                    else
                    {
                        enPassantrank++;
                    }


                    Piece piece = _pieces[enPassantfile - 'a', enPassantrank - '1'];
                    if (piece != null && piece is Pawn pawn)
                    {
                        pawn.CanEnPassant = true;
                        _enPassantPawn = pawn;
                    }
                    else
                    {
                        Debug.LogError($"invalid enpassant piece in FEN position {enPassantfile}{enPassantrank}");
                    }
                }
            }

            if (_blackKing == null || _whiteKing == null)
            {
                Debug.LogError("invalid position, missing king.");
            }

            foreach (Piece piece in _pieces)
            {
                if (piece != null)
                {
                    piece.UpdateRotation(true);
                }
            }
        }

        public string GetFen()
        {
            string fen = "";
            for (int rank = 7; rank >= 0; rank--)
            {
                int skip = 0;
                for (int file = 0; file < 8; file++)
                {
                    Piece piece = _pieces[file, rank];
                    if (piece != null)
                    {
                        if (skip > 0)
                        {
                            fen += skip.ToString();
                            skip = 0;
                        }

                        switch (piece.Type)
                        {
                            case PieceTypes.Pawn:
                                fen += piece.IsWhite ? 'P' : 'p';
                                break;
                            case PieceTypes.Rook:
                                fen += piece.IsWhite ? 'R' : 'r';
                                break;
                            case PieceTypes.Knight:
                                fen += piece.IsWhite ? 'N' : 'n';
                                break;
                            case PieceTypes.Bishop:
                                fen += piece.IsWhite ? 'B' : 'b';
                                break;
                            case PieceTypes.Queen:
                                fen += piece.IsWhite ? 'Q' : 'q';
                                break;
                            case PieceTypes.King:
                                fen += piece.IsWhite ? 'K' : 'k';
                                break;

                        }
                    }
                    else
                    {
                        skip++;
                    }
                }

                if (skip > 0)
                {
                    fen += skip.ToString();
                    skip = 0;
                }

                if (rank > 0)
                {
                    fen += '/';
                }
            }

            fen += ' ';
            fen += CurrentMove == Move.White ? 'w' : 'b';
            fen += ' ';


            if (WhiteCastlingRights.HasFlag(CastlingRights.KingSide))
                fen += 'K';
            if (WhiteCastlingRights.HasFlag(CastlingRights.QueenSide))
                fen += 'Q';
            if (BlackCastlingRights.HasFlag(CastlingRights.KingSide))
                fen += 'k';
            if (BlackCastlingRights.HasFlag(CastlingRights.QueenSide))
                fen += 'q';

            if (_enPassantPawn != null)
            {
                fen += " " + (char)(_enPassantPawn.CurrentFile + 'a');
                if (_enPassantPawn.IsWhite)
                {
                    fen += (int)(_enPassantPawn.CurrentRank);
                }
                else
                {
                    fen += (int)(_enPassantPawn.CurrentRank + 2);
                }
            }
            else
            {
                fen += " -";
            }

            return fen;
        }

        public void MovePiece(string notation, bool isWhite, bool playNoises = true, bool addToHistory = true)
        {
            try
            {
                if (notation == "O-O-O")
                {
                    if (isWhite)
                    {
                        MovePiece((int)_whiteKing.CurrentFile, (int)_whiteKing.CurrentRank, (int)Piece.File.C, (int)_whiteKing.CurrentRank, MoveType.Castle, null, playNoises, addToHistory);
                    }
                    else
                    {
                        MovePiece((int)_blackKing.CurrentFile, (int)_blackKing.CurrentRank, (int)Piece.File.C, (int)_blackKing.CurrentRank, MoveType.Castle, null, playNoises, addToHistory);
                    }
                    return;
                }
                
                if (notation == "O-O")
                {
                    if (isWhite)
                    {
                        MovePiece((int)_whiteKing.CurrentFile, (int)_whiteKing.CurrentRank, (int)Piece.File.G, (int)_whiteKing.CurrentRank, MoveType.Castle, null, playNoises, addToHistory);
                    }
                    else
                    {
                        MovePiece((int)_blackKing.CurrentFile, (int)_blackKing.CurrentRank, (int)Piece.File.G, (int)_blackKing.CurrentRank, MoveType.Castle, null, playNoises, addToHistory);
                    }
                    return;
                }
                

                //bool isCheck = false;
                if (notation.Last() == '+')
                {
                    notation = notation.Remove(notation.Length - 1);
                    //isCheck = true;
                }

                PieceTypes? promotion = null;
                if (notation.Contains("=Q"))
                {
                    promotion = PieceTypes.Queen;
                    notation = notation.Replace("=Q", "");
                }
                else if (notation.Contains("=N"))
                {
                    promotion = PieceTypes.Knight;
                    notation = notation.Replace("=N", "");
                }
                else if (notation.Contains("=R"))
                {
                    promotion = PieceTypes.Rook;
                    notation = notation.Replace("=R", "");
                }
                else if (notation.Contains("=B"))
                {
                    promotion = PieceTypes.Bishop;
                    notation = notation.Replace("=B", "");
                }

                PieceTypes pieceType;
                switch (notation[0])
                {
                    case 'K': pieceType = PieceTypes.King; break;
                    case 'Q': pieceType = PieceTypes.Queen; break;
                    case 'R': pieceType = PieceTypes.Rook; break;
                    case 'B': pieceType = PieceTypes.Bishop; break;
                    case 'N': pieceType = PieceTypes.Knight; break;
                    default: pieceType = PieceTypes.Pawn; break;
                }

                if (pieceType != PieceTypes.Pawn)
                {
                    notation = notation.Substring(1, notation.Length - 1);
                }

                Rank toRank = (Rank)(notation.Last() - '1');
                notation = notation.Substring(0, notation.Length - 1);

                File toFile = (File)(notation.Last() - 'a');
                notation = notation.Substring(0, notation.Length - 1);

                //bool isTake = false;
                if (notation.Contains('x'))
                {
                    //isTake = true;
                    notation = notation.Replace("x", "");
                }

                File? fileDisambiguation = null;
                Rank? rankDisambiguation = null;
                for (int i = 0; i < 2; i++)
                {
                    if (notation.Length > 0)
                    {
                        char disambiguation = notation.Last();
                        if (char.IsDigit(disambiguation))
                        {
                            rankDisambiguation = (Rank)(disambiguation - '1');
                        }
                        else if (char.IsLetter(disambiguation))
                        {
                            fileDisambiguation = (File)(disambiguation - 'a');
                        }
                    }
                }

                List<Piece> pieces = new List<Piece>();
                foreach (Piece piece in _pieces)
                {
                    if (piece != null && piece.Type == pieceType && piece.IsWhite == isWhite)
                    {
                        if (fileDisambiguation != null && piece.CurrentFile != fileDisambiguation)
                        {
                            continue;
                        }

                        if (rankDisambiguation != null && piece.CurrentRank != rankDisambiguation)
                        {
                            continue;
                        }

                        pieces.Add(piece);
                    }
                }

                List<Piece> resultingPieces = new List<Piece>();
                List<MoveData> possibleMoves = new List<MoveData>();
                foreach (Piece piece in pieces)
                {
                    foreach (MoveData move in piece.GetMoves(this))
                    {
                        if (move.Rank == toRank && move.File == toFile)
                        {
                            possibleMoves.Add(move);
                            resultingPieces.Add(piece);
                        }
                    }
                }

                if (resultingPieces.Count > 1)
                {
                    Debug.LogError("Something went wrong with move calculation, there is still piece ambiguity");
                    return;
                }
                else if (resultingPieces.Count == 0)
                {
                    Debug.LogError("Something went wrong with the move calculation, couldn't find valid piece.");
                    return;
                }
                else if (possibleMoves.Count > 1)
                {
                    Debug.LogError("Something went wrong with move calculation, there is still move ambiguaty");
                    return;
                }
                else if (possibleMoves.Count == 0)
                {
                    Debug.LogError("Something went wrong with move calculation, couldn't fine move");
                    return;
                }
                else
                {
                    Piece piece = resultingPieces.First();
                    MoveData move = possibleMoves.First();
                    MovePiece
                        ( (int)piece.CurrentFile
                        , (int)piece.CurrentRank
                        , (int)move.File
                        , (int)move.Rank
                        , move.Type
                        , promotion
                        , playNoises
                        , addToHistory
                        );
                }

            }
            catch (Exception)
            {
                Debug.LogError("Failed to parse move");
            }
        }

        public void MovePiece(int fromX, int fromY, int toX, int toY, MoveType moveType, PieceTypes? promotion = null, bool playNoises = true, bool addToHistory = true)
        {
            bool moveTook = false;

            Piece piece = _pieces[fromX, fromY];
            if (piece == null)
            {
                Debug.LogError("attempted to move null piece to square.");
                return;
            }

            Piece targetSquare = _pieces[toX, toY];


            if (targetSquare != null)
            {
                moveTook = true;
            }

            if (targetSquare != null)
            {
                GameObject.Destroy(targetSquare.gameObject);
            }

            if (moveType == MoveType.Enpassant)
            {
                moveTook = true;
                Enpassant();
            }

            UpdateEnpassantPawn(piece, fromY, toY);

            piece.CurrentFile = (Piece.File)toX;
            piece.CurrentRank = (Piece.Rank)toY;
            _pieces[toX, toY] = piece;
            _pieces[fromX, fromY] = null;


            if (moveType == MoveType.Castle)
            {
                Castle(toX, toY);
            }
            else if (promotion != null)
            {
                PromoteSquare(toX, toY, promotion);
            }

            ChangeCurrentMove();
            if (playNoises)
            {
                PlayMoveAudio(moveType, moveTook, promotion != null);
            }
            UpdateCastling(piece, fromX, fromY);
            if (addToHistory)
            {
                AddMoveToHistory
                    ( piece.Type
                    , (Piece.File)fromX
                    , (Piece.Rank)fromY
                    , (Piece.File)toX
                    , (Piece.Rank)toY
                    , moveTook
                    , (CurrentMove == Move.White && _whiteKing.IsAttacked(Pieces)) || (CurrentMove == Move.Black && _blackKing.IsAttacked(Pieces))
                    , moveType == MoveType.Castle
                    , promotion
                    , piece.IsWhite
                    );
            }
        }

        void Enpassant()
        {
            GameObject.Destroy(_enPassantPawn.gameObject);
            _pieces[(int)_enPassantPawn.CurrentFile, (int)_enPassantPawn.CurrentRank] = null;
        }

        void UpdateEnpassantPawn(Piece movedPiece, int fromY, int toY)
        {
            if (_enPassantPawn != null)
            {
                _enPassantPawn.CanEnPassant = false;
                _enPassantPawn = null;
            }

            if (movedPiece is Pawn pawn && Mathf.Abs(toY - fromY) == 2)
            {
                _enPassantPawn = pawn;
                _enPassantPawn.CanEnPassant = true;
            }
        }

        void PromoteSquare(int x, int y, Piece.PieceTypes? promotion = null)
        {
            Piece piece = _pieces[x, y];

            if (promotion == null)
            {
                Debug.LogError("promotion type is null.");
                return;
            }

            Piece prefab;
            switch (promotion)
            {
                case Piece.PieceTypes.Queen:
                    prefab = _piecesPrefabs.GetPiece(piece.IsWhite ? 'Q' : 'q');
                    break;
                case Piece.PieceTypes.Rook:
                    prefab = _piecesPrefabs.GetPiece(piece.IsWhite ? 'R' : 'r');
                    break;
                case Piece.PieceTypes.Bishop:
                    prefab = _piecesPrefabs.GetPiece(piece.IsWhite ? 'B' : 'b');
                    break;
                case Piece.PieceTypes.Knight:
                    prefab = _piecesPrefabs.GetPiece(piece.IsWhite ? 'N' : 'n');
                    break;
                default:
                    Debug.LogError("invalid promotion type.");
                    return;
            }

            Piece newPiece = GameObject.Instantiate<Piece>(prefab, _piecesContainer);
            newPiece.CurrentRank = piece.CurrentRank;
            newPiece.CurrentFile = piece.CurrentFile;
            newPiece.UpdateRotation(true);
            _pieces[(int)newPiece.CurrentFile, (int)newPiece.CurrentRank] = newPiece;

            GameObject.Destroy(piece.gameObject);
        }

        void Castle(int toX, int toY)
        {
            int rank;
            int start;
            int fin;

            if (CurrentMove == Move.White)
            {
                if (toX == (int)Piece.File.C)
                {
                    rank = (int)Piece.Rank.One;
                    start = (int)Piece.File.A;
                    fin = (int)Piece.File.D;
                }
                else
                {
                    rank = (int)Piece.Rank.One;
                    start = (int)Piece.File.H;
                    fin = (int)Piece.File.F;
                }
            }
            else
            {
                if (toX == (int)Piece.File.C)
                {
                    rank = (int)Piece.Rank.Eight;
                    start = (int)Piece.File.A;
                    fin = (int)Piece.File.D;
                }
                else
                {
                    rank = (int)Piece.Rank.Eight;
                    start = (int)Piece.File.H;
                    fin = (int)Piece.File.F;
                }
            }

            _pieces[fin, rank] = _pieces[start, rank];
            _pieces[fin, rank].CurrentFile = (Piece.File)fin;
            _pieces[start, rank] = null;
        }

        void ChangeCurrentMove()
        {
            CurrentMove = CurrentMove == Move.White ? Move.Black : Move.White;
        }

        void PlayMoveAudio(MoveType moveType, bool moveTook, bool promotion)
        {
            if ((CurrentMove == Move.White && _whiteKing.IsAttacked(Pieces)) || (CurrentMove == Move.Black && _blackKing.IsAttacked(Pieces)))
            {
                _moveAudio.Play(MoveAudio.Clips.Check);
            }
            else if (moveType == MoveType.Castle)
            {
                _moveAudio.Play(MoveAudio.Clips.Castle);
            }
            else if (moveTook)
            {
                _moveAudio.Play(MoveAudio.Clips.Capture);
            }
            else if (promotion)
            {
                _moveAudio.Play(MoveAudio.Clips.Promotion);
            }
            else
            {
                _moveAudio.Play(MoveAudio.Clips.Move);
            }
        }

        void UpdateCastling(Piece pieceMoved, int piecePositionX, int piecePositionY)
        {
            if (pieceMoved is King king)
            {
                if (king.IsWhite)
                {
                    WhiteCastlingRights = CastlingRights.None;
                }
                else
                {
                    BlackCastlingRights = CastlingRights.None;
                }
            }
            else if (pieceMoved is Rook rook)
            {
                if (rook.IsWhite)
                {
                    if (piecePositionX == 0 && piecePositionY == 0)
                    {
                        WhiteCastlingRights &= ~CastlingRights.QueenSide;
                    }
                    else if (piecePositionX == 7 && piecePositionY == 0)
                    {
                        WhiteCastlingRights &= ~CastlingRights.KingSide;
                    }
                }
                else
                {
                    if (piecePositionX == 0 && piecePositionY == 7)
                    {
                        BlackCastlingRights &= ~CastlingRights.QueenSide;
                    }
                    else if (piecePositionX == 7 && piecePositionY == 7)
                    {
                        BlackCastlingRights &= ~CastlingRights.KingSide;
                    }
                }
            }
        }

        void AddMoveToHistory(PieceTypes pieceType, File fileFrom, Rank rankfrom, File fileTo, Rank rankTo, bool isCapture, bool isCheck, bool isCastle, PieceTypes? promotion, bool isWhite)
        {
            History.Move move = new History.Move();
            move.IsCapture = isCapture;
            move.IsCheck = isCheck;
            move.IsCastle = isCastle;
            move.IsWhite = isWhite;
            move.PieceType = pieceType;
            move.Promotion = promotion;

            move.FromFile = fileFrom;
            move.FromRank = rankfrom;

            move.ToFile = fileTo;
            move.ToRank = rankTo;

            move.resultingFen = GetFen();
            Debug.Log(move.resultingFen);

            Piece piece = _pieces[(int)fileTo, (int)rankTo];
            if (piece == null)
            {
                Debug.LogError("attempted to add move to history for null piece.");
                return;
            }

            Piece[] ambiguousPieces = piece.GetAttackingPiecesOfType(Pieces, (int)fileFrom, (int)rankfrom).ToArray();
            if (ambiguousPieces.Length > 0)
            {
                bool isFileAmbiguous = false;
                bool isRankAmbiguous = false;
                foreach (var ambiguousPiece in ambiguousPieces)
                {
                    if (ambiguousPiece.CurrentFile == fileFrom)
                    {
                        isFileAmbiguous = true;
                    }
                    if (ambiguousPiece.CurrentRank == rankfrom)
                    {
                        isRankAmbiguous = true;
                    }
                }

                if (isFileAmbiguous)
                {
                    if (isRankAmbiguous)
                    {
                        move.FileDisambiguation = fileFrom;
                        move.RankDisambiguation = rankfrom;
                    }
                    else
                    {
                        move.RankDisambiguation = rankfrom;
                    }
                }
                else
                {
                    move.FileDisambiguation = fileFrom;
                }
            }

            _boardHistory.AddMove(move);
        }

        IEnumerable<MoveData> ValidateMoves(int fromX, int fromY, IEnumerable<MoveData> moves)
        {
            foreach (var move in moves)
            {
                if (move.Type == MoveType.Castle)
                {
                    yield return move;
                    continue;
                }

                if (ValidateMove(fromX, fromY, (int)move.File, (int)move.Rank))
                    yield return move; 
            }
        }

        bool ValidateMove(int fromX, int fromY, int toX, int toY)
        {
            Piece piece = _pieces[fromX, fromY];
            Piece targetSquare = _pieces[toX, toY];

            _pieces[fromX, fromY] = null;
            _pieces[toX, toY] = piece;

            if (piece is King king)
            {
                if ((CurrentMove == Move.White && _whiteKing.IsAttacked(Pieces, toX, toY)) || (CurrentMove == Move.Black && _blackKing.IsAttacked(Pieces, toX, toY)))
                {
                    _pieces[fromX, fromY] = piece;
                    _pieces[toX, toY] = targetSquare;
                    return false;
                }
            }
            else
            {
                if ((CurrentMove == Move.White && _whiteKing.IsAttacked(Pieces)) || (CurrentMove == Move.Black && _blackKing.IsAttacked(Pieces)))
                {
                    _pieces[fromX, fromY] = piece;
                    _pieces[toX, toY] = targetSquare;
                    return false;
                }
            }

            _pieces[fromX, fromY] = piece;
            _pieces[toX, toY] = targetSquare;
            return true;
        }

        void ClearBoard()
        {
            _blackKing = null;
            _whiteKing = null;

            BlackCastlingRights = CastlingRights.None;
            WhiteCastlingRights = CastlingRights.None;
            CurrentMove = Move.White;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (_pieces[i, j] != null)
                    {
                        GameObject.Destroy(_pieces[i, j].gameObject);
                        _pieces[i, j] = null;
                    }
                }
            }
        }
    }
}
