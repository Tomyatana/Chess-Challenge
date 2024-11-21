using System;
using System.Threading.Tasks;
using ChessChallenge.API;
using System.Linq;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int weight = 0;
        int moveidx = 0;
        Move[] moves = board.GetLegalMoves();
        for(int i = 0; i < moves.Length; i++) {
            int newWeight = 0;
            Move move = moves[i];

            if(move.IsCapture) {
                switch(move.CapturePieceType) {
                case PieceType.Queen:
                    newWeight += 5;
                    break;
                case PieceType.Rook:
                    newWeight += 3;
                    break;
                case PieceType.Bishop:
                case PieceType.Knight:
                    newWeight += 2;
                    break;
                case PieceType.Pawn: if(move.TargetSquare.Rank == 7 || move.TargetSquare.Rank == 2) newWeight += 3;
                    else newWeight++;
                    break;
                }
            }

            if(move.MovePieceType == PieceType.Pawn) {
                if(move.TargetSquare.File < 3 || move.TargetSquare.File > 6) continue;
                if(move.TargetSquare.Rank < 7 && move.TargetSquare.Rank > 2) {
                    if(move.TargetSquare.File == 4 || move.TargetSquare.File == 5) {
                        newWeight += 1;
                        Console.WriteLine($"Moving to {move.TargetSquare.Name}, Rank: {move.TargetSquare.Rank}, File: {move.TargetSquare.File}");
                    }
                }
                if(move.IsPromotion && move.PromotionPieceType == PieceType.Queen){
                    newWeight += 5;
                }
            }


            board.MakeMove(move);
            if(board.IsInCheck()) {
                newWeight += (board.FiftyMoveCounter > 90) ? 10 : 2;
            }
            if(board.IsInCheckmate()) {
                return move;
            }
            if(board.IsRepeatedPosition()) {
                i = 0;
                moves = moves.Where(val => val != move).ToArray();
            }
            if(board.GameRepetitionHistory.Length > 2)
                if(board.ZobristKey == board.GameRepetitionHistory[^2]) newWeight = -999;

            if(board.IsFiftyMoveDraw()) newWeight = -999;

            board.UndoMove(move);

            if(isAttacked(move.TargetSquare, board, board.IsWhiteToMove)) {
                newWeight -= 6;
            }
            if(move.IsEnPassant) newWeight = 999;
            if(move.MovePieceType == PieceType.King && !board.IsInCheck()) continue;
            if(newWeight >= weight) {
                moveidx = i;
                weight = newWeight;
            }
        }
        Console.WriteLine(Enum.GetName<PieceType>(moves[moveidx].MovePieceType));
        Console.WriteLine(weight);
        board.MakeMove(moves[moveidx]);
        board.UndoMove(moves[moveidx]);
        return moves[moveidx];
    }

    bool isAttacked(Square square, Board board, bool isWhite) {
        int lsb;

        foreach(PieceType type in Enum.GetValues<PieceType>()) {
            if(type == PieceType.None) continue;

            ulong pieceBoard = board.GetPieceBitboard(type, !isWhite);

            lsb = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBoard);
            while(lsb != 64) {
                Square pos = new Square(lsb);
                lsb = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBoard);
                ulong Attacks = BitboardHelper.GetPieceAttacks(type, pos, board, !isWhite);
                BitboardHelper.VisualizeBitboard(Attacks);
                if(BitboardHelper.SquareIsSet(Attacks, square)) return true;
            }

        }

        return false/*res*/;
    }
}
