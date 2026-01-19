using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;
using Chess.Core.Models;
using Chess.Core.Logic;
using Chess.Architecture.Commands;
using Debug = UnityEngine.Debug; // Debug.Log karışıklığını önlemek için

// --- KRİTİK DÜZELTME: İSİM ÇAKIŞMASINI ÖNLEME ---
// Kodda "Vector2Int" dediğimizde Unity'ninkini değil, bizimkini kullan:
using Vector2Int = Chess.Core.Models.Vector2Int; 

namespace Chess.Core.Diagnostics
{
    // Bu sınıf sadece test amaçlıdır, oyun içinde kullanılmaz.
    public class PerftRunner : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private int _depth = 3; // 3 idealdir.
        
        // Standart Başlangıç Pozisyonu
        private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        
        // "KiwiPete" - Meşhur zorlu test pozisyonu
        private const string KiwiPeteFen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        [ContextMenu("Run Perft (Start Pos)")]
        public void RunStandardPerft()
        {
            RunTest("Standard Start", StartFen, _depth);
        }

        [ContextMenu("Run Perft (KiwiPete - Complex)")]
        public void RunKiwiPetePerft()
        {
            // KiwiPete çok karmaşık olduğu için derinliği 1 düşürerek test etmek güvenlidir
            RunTest("KiwiPete (Tricky)", KiwiPeteFen, _depth > 1 ? _depth - 1 : 1); 
        }

        private void RunTest(string testName, string fen, int depth)
        {
            Board board = new Board();
            FenUtility.LoadPositionFromFen(board, fen);

            Debug.Log($"<color=yellow>--- PERFT STARTED: {testName} (Depth: {depth}) ---</color>");
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

            long totalNodes = Perft(board, depth);

            sw.Stop();
            
            double seconds = sw.Elapsed.TotalSeconds;
            // Sıfıra bölme hatasını önle
            double nps = seconds > 0 ? totalNodes / seconds : 0;

            Debug.Log($"<color=green>--- PERFT COMPLETED ---</color>\n" +
                      $"Nodes: {totalNodes}\n" +
                      $"Time: {sw.ElapsedMilliseconds}ms ({seconds:F2}s)\n" +
                      $"NPS: {nps:F0} nodes/sec");
        }

        private long Perft(Board board, int depth)
        {
            if (depth == 0) return 1;

            long nodes = 0;
            
            // GetAllPiecePositions artık "using Vector2Int = ..." sayesinde hata vermeyecek
            List<Vector2Int> pieces = GetAllPiecePositions(board, board.Turn);

            foreach (var pos in pieces)
            {
                var moves = Arbiter.GetLegalMoves(board, pos);
                foreach (var target in moves)
                {
                    Piece movedPiece = board.GetPieceAt(pos);
                    
                    if (movedPiece.Type == PieceType.Pawn && IsPromotion(movedPiece, target))
                    {
                        PieceType[] promos = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
                        foreach (var pType in promos)
                        {
                            ICommand cmd = new MoveCommand(board, pos, target, pType);
                            cmd.Execute();
                            nodes += Perft(board, depth - 1);
                            cmd.Undo();
                        }
                    }
                    else
                    {
                        ICommand cmd = new MoveCommand(board, pos, target, PieceType.Queen);
                        cmd.Execute();
                        nodes += Perft(board, depth - 1);
                        cmd.Undo();
                    }
                }
            }

            return nodes;
        }

        private List<Vector2Int> GetAllPiecePositions(Board board, PieceColor color)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = board.GetPieceAt(new Vector2Int(x, y));
                    if (p.Type != PieceType.None && p.Color == color)
                    {
                        positions.Add(new Vector2Int(x, y));
                    }
                }
            }
            return positions;
        }

        private bool IsPromotion(Piece piece, Vector2Int target)
        {
            int lastRank = piece.IsWhite ? 7 : 0;
            return target.y == lastRank;
        }
    }
}