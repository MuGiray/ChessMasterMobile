using System.Collections.Generic;
using Chess.Core.Models;
using System.Text;

namespace Chess.Core.AI
{
    public class GameReport
    {
        public int Blunders;   // Büyük Hata (Örn: Veziri uyumak)
        public int Mistakes;   // Hata (Pozisyonu kötüleştirmek)
        public int Inaccuracies; // Ufak Hata (En iyisi değil)
        
        public List<AnalysisComment> Comments = new List<AnalysisComment>();

        public string GetSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--- GAME ANALYSIS REPORT ---");
            sb.AppendLine($"Blunders: {Blunders}");
            sb.AppendLine($"Mistakes: {Mistakes}");
            sb.AppendLine($"Inaccuracies: {Inaccuracies}");
            sb.AppendLine("----------------------------");
            
            if (Comments.Count == 0)
            {
                sb.AppendLine("Perfect Game! (Or no analysis data)");
            }
            else
            {
                foreach (var comment in Comments)
                {
                    sb.AppendLine(comment.Message);
                }
            }
            return sb.ToString();
        }
    }

    public struct AnalysisComment
    {
        public int MoveIndex;
        public string Notation;
        public string Message;
        public int ScoreDrop; // Ne kadar puan kaybetti?

        public AnalysisComment(int index, string notation, string msg, int drop)
        {
            MoveIndex = index;
            Notation = notation;
            Message = msg;
            ScoreDrop = drop;
        }
    }
}