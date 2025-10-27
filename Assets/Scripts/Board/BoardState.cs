using Board.Audio;
using Board.History;
using Board.Pieces;
using Board.Pieces.Moves;
using System.Collections.Generic;
using UnityEngine;
using static Board.Pieces.Piece;
using System.Linq;

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

        public void MovePiece(string notation, bool playNoises = true)
        {
            
        }

        public void MovePiece(int fromX, int fromY, int toX, int toY, MoveType moveType, Piece.PieceTypes? promotion = null, bool playNoises = true)
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
