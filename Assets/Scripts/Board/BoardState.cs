using Board.Pieces;
using Board.Pieces.Moves;
using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Board.Audio;
using TMPro.EditorUtilities;

namespace Board.BoardMarkers
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

        public const string DefaultFEN = "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1";

        MoveAudio _moveAudio;

        PiecePrefabs _piecesPrefabs;
        RectTransform _piecesContainer;

        Move _currentMove;
        public CastlingRights WhiteCastlingRights { get; private set; }
        public CastlingRights BlackCastlingRights { get; private set; }

        Piece[,] _pieces = new Piece[8, 8];
        King BlackKing;
        King WhiteKing;

        public Piece[,] Pieces => _pieces;

        public BoardState(MoveAudio moveAudio, RectTransform piecesContainer, PiecePrefabs piecePrefabs)
        {
            _piecesContainer = piecesContainer;
            _piecesPrefabs = piecePrefabs;
            _moveAudio = moveAudio;
        }

        public Piece GetPieceInfo(out IEnumerable<MoveData> moveData, int file, int rank)
        {
            if (_pieces[file, rank] == null)
            {
                moveData = null;
                return null;
            }

            if (_currentMove == Move.White && !_pieces[file, rank].IsWhite)
            {
                moveData = null;
                return null;
            }

            if (_currentMove == Move.Black && _pieces[file, rank].IsWhite)
            {
                moveData = null;
                return null;
            }

            moveData = ValidateMoves(file, rank, _pieces[file, rank].GetMoves(this));
            return _pieces[file, rank];
        }

        public void SetFEN(string fen)
        {
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
                                WhiteKing = king;
                            }
                            else
                            {
                                BlackKing = king;
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
                    _currentMove = Move.White;
                }
                else if (c == 'b' || c == 'B')
                {
                    _currentMove = Move.Black;
                }
                else if (c == 'K' && _pieces[7, 0] is Rook && _pieces[7, 0].IsWhite && WhiteKing?.CurrentRank == Piece.Rank.One && WhiteKing?.CurrentFile == Piece.File.E)
                {
                    WhiteCastlingRights |= CastlingRights.KingSide;
                }
                else if (c == 'Q' && _pieces[0, 0] is Rook && _pieces[0, 0].IsWhite && WhiteKing?.CurrentRank == Piece.Rank.One && WhiteKing?.CurrentFile == Piece.File.E)
                {
                    WhiteCastlingRights |= CastlingRights.QueenSide;
                }
                else if (c == 'k' && _pieces[7, 7] is Rook && !_pieces[7, 7].IsWhite && BlackKing?.CurrentRank == Piece.Rank.Eight && BlackKing?.CurrentFile == Piece.File.E)
                {
                    BlackCastlingRights |= CastlingRights.KingSide;
                }
                else if (c == 'q' && _pieces[0, 7] is Rook && !_pieces[0, 7].IsWhite && BlackKing?.CurrentRank == Piece.Rank.Eight && BlackKing?.CurrentFile == Piece.File.E)
                {
                    BlackCastlingRights |= CastlingRights.QueenSide;
                }
            }

            if (BlackKing == null || WhiteKing == null)
            {
                Debug.LogError("invalid position, missing king.");
            }
        }

        public void MovePiece(int fromX, int fromY, int toX, int toY, MoveType moveType)
        {
            bool moveTook = false;

            Piece piece = _pieces[fromX, fromY];
            if (piece == null)
            {
                Debug.LogError("attempted to null piece to square.");
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

            piece.CurrentFile = (Piece.File)toX;
            piece.CurrentRank = (Piece.Rank)toY;
            _pieces[toX, toY] = piece;
            _pieces[fromX, fromY] = null;

            if (moveType == MoveType.Castle)
            {
                int rank;
                int start;
                int fin;

                if (_currentMove == Move.White)
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

            _currentMove = _currentMove == Move.White ? Move.Black : Move.White;
            if ((_currentMove == Move.White && WhiteKing.IsAttacked(Pieces)) || (_currentMove == Move.Black && BlackKing.IsAttacked(Pieces)))
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
            else
            {
                _moveAudio.Play(MoveAudio.Clips.Move);
            }

            if (piece is King king)
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
            else if (piece is Rook rook)
            {
                if (rook.IsWhite)
                {
                    if (fromX == 0 && fromY == 0)
                    {
                        WhiteCastlingRights &= ~CastlingRights.QueenSide;
                    }
                    else if (fromX == 7 && fromY == 0)
                    {
                        WhiteCastlingRights &= ~CastlingRights.KingSide;
                    }
                }
                else
                {
                    if (fromX == 0 && fromY == 7)
                    {
                        BlackCastlingRights &= ~CastlingRights.QueenSide;
                    }
                    else if (fromX == 7 && fromY == 7)
                    {
                        BlackCastlingRights &= ~CastlingRights.KingSide;
                    }
                }
            }
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
                if ((_currentMove == Move.White && WhiteKing.IsAttacked(Pieces, toX, toY)) || (_currentMove == Move.Black && BlackKing.IsAttacked(Pieces, toX, toY)))
                {
                    _pieces[fromX, fromY] = piece;
                    _pieces[toX, toY] = targetSquare;
                    return false;
                }
            }
            else
            {
                if ((_currentMove == Move.White && WhiteKing.IsAttacked(Pieces)) || (_currentMove == Move.Black && BlackKing.IsAttacked(Pieces)))
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
    }
}
