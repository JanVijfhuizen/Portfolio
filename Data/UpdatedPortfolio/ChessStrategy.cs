using CurveSearchLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessLib
{
    [CreateAssetMenu(fileName = "Chess Strategy", menuName = "Chess/Strategy", order = 1)]
    public class ChessStrategy : ScriptableObject, IStrategy<ChessTactic>
    {
        [SerializeField]
        private ChessTactic core;
        public ChessTactic Core
        {
            get
            {
                return core;
            }
        }

        [SerializeField]
        private ChessTactic[] required;
        public ChessTactic[] Required
        {
            get
            {
                return required;
            }
        }

        [SerializeField]
        private ChessTacticArray[] mutations;
        public ChessTactic[][] Mutations { get; private set; }

        public void Init()
        {
            int length = mutations.Length;
            Mutations = new ChessTactic[length][];
            for (int i = 0; i < length; i++)
                Mutations[i] = mutations[i].tactics;
        }

        public int maxUses;
    }

    [Serializable]
    public class ChessTactic
    {
        public enum PieceType { Pawn, Rook, Knight, Bishop, King, Queen }

        public Vector2Int relativePosition;
        public PieceType[] possibleTypes;

        public bool ContainsType(Type type)
        {
            int length = possibleTypes.Length;
            for (int i = 0; i < length; i++)
                switch (possibleTypes[i])
                {
                    case PieceType.Pawn:
                        if (type == typeof(Chess.Pawn))
                            return true;
                        break;
                    case PieceType.Rook:
                        if (type == typeof(Chess.Rook))
                            return true;
                        break;
                    case PieceType.Knight:
                        if (type == typeof(Chess.Knight))
                            return true;
                        break;
                    case PieceType.Bishop:
                        if (type == typeof(Chess.Bishop))
                            return true;
                        break;
                    case PieceType.Queen:
                        if (type == typeof(Chess.Queen))
                            return true;
                        break;
                    case PieceType.King:
                        if (type == typeof(Chess.King))
                            return true;
                        break;
                }

            return false;
        }
    }

    [Serializable]
    public class ChessTacticArray
    {
        public ChessTactic[] tactics;
    }
}
