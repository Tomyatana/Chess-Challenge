using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    const ulong centre = 0b0000000000000000000000001100000011000000000000000000000000000;
    const ulong outerRing = 0b000000000000000001111000010010000100100001111000000000000000000;
    const int centreWt = 400;
    static readonly int[] MaterialValue = {
        0, 100, 300, 400, 600, 1500, 200000
    };

    const int mobilityWeight = 30;
    const int checkWeight = 600;
    const int castlingWeight = 500;
    const int kingProtectionWt = 300;


    public Move Think(Board board, Timer timer)
    {
        bool isWhite = board.IsWhiteToMove;
        ulong b = board.BlackPiecesBitboard;
        Move bestMove = board.GetLegalMoves()[0];
        int position = NegaMax(3, 3, board, isWhite, ref bestMove);
        Console.WriteLine(position);
        return bestMove;
    }

    int EvalPos(Board board, bool isWhite, int depth) {
        int score = 0;

        if(board.IsInCheckmate()) {
            return (9999999 - depth * bool2int(!board.IsWhiteToMove))*bool2int(!board.IsWhiteToMove);
        }

        score += checkWeight * bool2int(!board.IsWhiteToMove) * ((board.IsInCheck()) ? 1 : 0);

        score += castlingWeight * (
                ((board.HasKingsideCastleRight(true)) ? 1 : 0) - ((board.HasKingsideCastleRight(false)) ? 1 : 0)
                + ((board.HasQueensideCastleRight(true)) ? 1 : 0) - ((board.HasQueensideCastleRight(false)) ? 1 : 0)
                );

        foreach(PieceType type in Enum.GetValues<PieceType>()) {
            ulong whiteBits = board.GetPieceBitboard(type, true);
            ulong blackBits = board.GetPieceBitboard(type, false);
            score += (BitboardHelper.GetNumberOfSetBits(whiteBits) - BitboardHelper.GetNumberOfSetBits(blackBits)) * MaterialValue[(int)type];

            ulong whiteCpy = whiteBits;
            ulong blackCpy = blackBits;
            int lsb;
            int whiteMobility = 0;
            int blackMobility = 0;
            while((lsb = BitboardHelper.ClearAndGetIndexOfLSB(ref whiteCpy)) < 64) {
                whiteMobility += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(type, new Square(lsb), board, true));
            }
            while((lsb = BitboardHelper.ClearAndGetIndexOfLSB(ref blackCpy)) < 64) {
                blackMobility += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(type, new Square(lsb), board, false));
            }
            score += (whiteMobility - blackMobility) * mobilityWeight;

            switch(type) {
            case PieceType.Pawn:
                score += (BitboardHelper.GetNumberOfSetBits(whiteBits & centre) - BitboardHelper.GetNumberOfSetBits(blackBits & centre)) * centreWt;
                break;
            default:
                score += (BitboardHelper.GetNumberOfSetBits(whiteBits & outerRing) - BitboardHelper.GetNumberOfSetBits(blackBits & outerRing)) * mobilityWeight;
                break;
            }

            whiteCpy = whiteBits;
            blackCpy = blackBits;
            if(type != PieceType.King) {
                int whiteCtrl = 0;
                int blackCtrl = 0;
                while((lsb = BitboardHelper.ClearAndGetIndexOfLSB(ref whiteCpy)) < 64) {
                    whiteCtrl += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(type, new Square(lsb), board, true) & centre);
                }
                while((lsb = BitboardHelper.ClearAndGetIndexOfLSB(ref blackCpy)) < 64) {
                    blackCtrl += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(type, new Square(lsb), board, false) & centre);
                }
                score += (whiteCtrl - blackCtrl) * centreWt;
            } else {
                int whiteKingProtection = 0;
                int blackKingProtection = 0;
                Square whiteKing = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref whiteCpy));
                whiteKingProtection += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(type, whiteKing, board, true) & board.GetPieceBitboard(PieceType.Pawn, true));

                Square blackKing = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref blackCpy));
                blackKingProtection += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(type, blackKing, board, true) & board.GetPieceBitboard(PieceType.Pawn, true));

                score += (whiteKingProtection - blackKingProtection) * kingProtectionWt;

                score -= (
                        BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(PieceType.Queen, whiteKing, board, true)) 
                        - BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(PieceType.Queen, blackKing, board, false))
                        ) * mobilityWeight;
            }

        }


        return score * bool2int(!isWhite);
    }

    int NegaMax(int depth, int initialDepth, Board board, bool isWhite, ref Move bestMove, int alpha = int.MinValue, int beta = int.MaxValue) {
        Move[] moves = board.GetLegalMoves();
        if(depth == 0 || moves.Length == 0) 
            return quiesce(board, isWhite, initialDepth, alpha, beta); //EvalPos(board, isWhite, initialDepth); 
        int max = int.MinValue;

        foreach(Move move in moves) {
            board.MakeMove(move);
            if(board.IsFiftyMoveDraw() || board.IsRepeatedPosition()) {
                board.UndoMove(move);
                continue;
            }
            int score = -NegaMax(depth-1, initialDepth, board, isWhite, ref bestMove, alpha, beta);
            if(score > max) {
                max = score;
                if (initialDepth == depth)
                    bestMove = move;
                if(score > alpha)
                    alpha = score;
            }
            if (score >= beta)
                break;
            board.UndoMove(move);
        }

        return max;
    }
        
    int quiesce(Board board, bool isWhite, int depth, int alpha, int beta) {
        int eval = EvalPos(board, isWhite, depth);
        if(eval >= beta) {
            return beta;
        } 
        if(alpha < eval) {
            alpha = eval;
        }
        foreach(Move move in board.GetLegalMoves(true)) {
            board.MakeMove(move);
            int score = -quiesce(board, isWhite, depth, -alpha, -beta);
            board.UndoMove(move);

            if(score >= beta)
                return beta;
            if(score > alpha)
                alpha = score;
        }
        return alpha;
    }
        
    int bool2int(bool b) {
        return (b) ? 1 : 0 * 2 - 1;
    }
}
