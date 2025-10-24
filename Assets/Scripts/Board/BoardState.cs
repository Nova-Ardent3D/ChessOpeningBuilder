using Board.Pieces;
using Board.Pieces.Moves;
using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

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
            KingSide = 1 << 0,
            QueenSide = 1 << 1,
            Both = KingSide | QueenSide,
        }

        public const string DefaultFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        PiecePrefabs _piecesPrefabs;
        RectTransform _piecesContainer;

        Move _currentMove;
        CastlingRights _whiteCastlingRights;
        CastlingRights _blackCastlingRights;

        Piece[,] _pieces = new Piece[8, 8];
        public Piece[,] Pieces => _pieces;

        public BoardState(RectTransform piecesContainer, PiecePrefabs piecePrefabs)
        {
            _piecesContainer = piecesContainer;
            _piecesPrefabs = piecePrefabs;
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

            moveData = _pieces[file, rank].GetMoves(this);
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
                else if (c == 'K')
                {
                    _whiteCastlingRights |= CastlingRights.KingSide;
                }
                else if (c == 'Q')
                {
                    _whiteCastlingRights |= CastlingRights.QueenSide;
                }
                else if (c == 'k')
                {
                    _blackCastlingRights |= CastlingRights.KingSide;
                }
                else if (c == 'q')
                {
                    _blackCastlingRights |= CastlingRights.QueenSide;
                }
            }
        }

        public void MovePiece(int fromX, int fromY, int toX, int toY)
        {
            Piece piece = _pieces[fromX, fromY];
            if (piece == null)
            {
                Debug.LogError("attempted to null piece to square.");
                return;
            }

            Piece targetSquare = _pieces[toX, toY];
            if (targetSquare != null)
            {
                GameObject.Destroy(targetSquare.gameObject);
            }

            piece.CurrentFile = (Piece.File)toX;
            piece.CurrentRank = (Piece.Rank)toY;
            _pieces[toX, toY] = piece;

            _currentMove = _currentMove == Move.White ? Move.Black : Move.White;
        }
    }
}
