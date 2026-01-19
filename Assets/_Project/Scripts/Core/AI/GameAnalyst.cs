using System.Collections.Generic;
using UnityEngine;
using Chess.Core.Models;

namespace Chess.Core.AI
{
    public static class GameAnalyst
    {
        // Eşik Değerler (Centipawn: 100 = 1 Piyon)
        private const int BLUNDER_THRESHOLD = 300; 
        private const int MISTAKE_THRESHOLD = 100; 
        private const int INACCURACY_THRESHOLD = 50; 

        public static GameReport Analyze(List<MoveRecord> history)
        {
            GameReport report = new GameReport();
            
            // Başlangıç puanı 0 (Eşit)
            int previousScore = 0; 

            for (int i = 0; i < history.Count; i++)
            {
                MoveRecord move = history[i];
                int currentScore = move.EvalScore;

                // Hamleyi kim yaptı? (Çift sayılar Beyaz, Tek sayılar Siyah)
                bool isWhiteMove = (i % 2 == 0);

                // Puan farkını hesapla
                int scoreDrop = 0;
                if (isWhiteMove)
                    scoreDrop = previousScore - currentScore;
                else
                    scoreDrop = currentScore - previousScore;

                // Hata Tespiti
                if (scoreDrop > 0)
                {
                    string commentType = "";
                    
                    // --- TÜRKÇELEŞTİRME KISMI ---
                    if (scoreDrop >= BLUNDER_THRESHOLD)
                    {
                        report.Blunders++;
                        commentType = "ÇOK BÜYÜK HATA"; // BLUNDER
                    }
                    else if (scoreDrop >= MISTAKE_THRESHOLD)
                    {
                        report.Mistakes++;
                        commentType = "HATA"; // MISTAKE
                    }
                    else if (scoreDrop >= INACCURACY_THRESHOLD)
                    {
                        report.Inaccuracies++;
                        commentType = "EKSİKLİK"; // INACCURACY
                    }

                    if (commentType != "")
                    {
                        // "White" -> "Beyaz", "Black" -> "Siyah"
                        string who = isWhiteMove ? "Beyaz" : "Siyah";
                        
                        // Mesaj Formatı: "BÜYÜK HATA - 8. Hamle (Beyaz): Nc3, 135 puan kaybettirdi."
                        string msg = $"{commentType} - {i/2 + 1}. Hamle ({who}): {move.Notation}, {scoreDrop} puan kaybettirdi.";
                        
                        report.Comments.Add(new AnalysisComment(i, move.Notation, msg, scoreDrop));
                    }
                }

                previousScore = currentScore;
            }

            return report;
        }
    }
}