using System;
using System.Collections.Generic;
using System.Linq;

namespace osero
{
    public static class AI
    {
        static readonly (int dx, int dy)[] Directions =
        {
            (0, -1),(0, 1),(-1, 0),(1, 0),
            (-1, -1),(1, -1),(-1, 1),(1, 1)
        };
        static ulong[,,] zobrist;
        static ulong zobristTurn;
        static Dictionary<ulong, int> TT = new Dictionary<ulong, int>(1_000_000);
        static Random rng = new Random();
        static AI()
        {
            zobrist = new ulong[8, 8, 2]; // 0:Black 1:White

            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    for (int c = 0; c < 2; c++)
                        zobrist[x, y, c] = RandomUlong();

            zobristTurn = RandomUlong();
        }

        static ulong RandomUlong()
        {
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        public static (int x, int y) ThinkHard(StoneColor[,] board)
        {
            var moves = GetValidMoves(board, StoneColor.White);
            if (moves.Count == 0) return (-1, -1);
            int emptyCount = board.Cast<StoneColor>().Count(s => s == StoneColor.None);

            int depth = 6;
            if (emptyCount <= 32) depth = 7;
            if (emptyCount <= 20) depth = 8;
            int bestScore = int.MinValue;
            (int x, int y) bestMove = moves[0];

            moves = OrderMovesForAI(board, moves, StoneColor.White);

            foreach (var (x, y) in moves)
            {
                var next = (StoneColor[,])board.Clone();
                ApplyMove(next, x, y, StoneColor.White);
                int score = MiniMax(next, depth - 1, false, int.MinValue, int.MaxValue);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = (x, y);
                }
            }

            return bestMove;
        }

        static List<(int x, int y)> OrderMovesForAI(StoneColor[,] board, List<(int x, int y)> moves, StoneColor color)
        {
            int emptyCount = board.Cast<StoneColor>().Count(s => s == StoneColor.None);

            return moves.OrderByDescending(m =>
            {
                int x = m.x, y = m.y;

                // 角優先
                if ((x == 0 && y == 0) || (x == 0 && y == 7) ||
                    (x == 7 && y == 0) || (x == 7 && y == 7)) return 1000;

                // XマスやCマスは危険
                if (IsDangerSquare(x, y)) return -500;

                // 辺は優先度高め
                if (x == 0 || x == 7 || y == 0 || y == 7) return 500;

                // 裏返せる枚数＋可動域
                int rev = CountReversible(board, x, y, color);
                int mobility = GetValidMovesAfterMove(board, x, y, color).Count;

                // 序盤は控えめ、中盤・終盤は重視
                if (emptyCount > 50) rev /= 2;
                return rev + mobility;
            }).ToList();
        }
        static ulong ZobristHash(StoneColor[,] board, bool whiteTurn)
        {
            ulong h = 0;

            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    if (board[x, y] == StoneColor.Black)
                        h ^= zobrist[x, y, 0];
                    else if (board[x, y] == StoneColor.White)
                        h ^= zobrist[x, y, 1];
                }

            if (whiteTurn)
                h ^= zobristTurn;

            return h;
        }
        static int MiniMax(StoneColor[,] board, int depth, bool maximizing, int alpha, int beta)
        {
            ulong hash = ZobristHash(board, maximizing);
            if (TT.TryGetValue(hash, out int cached))
                return cached;

            int emptyCount = board.Cast<StoneColor>().Count(s => s == StoneColor.None);
            if (depth <= 0)
                return EvaluateBoardFull(board);
            if (emptyCount <= 10)
            {
                return EvaluateEndgame(board) * 1000;
            }
            
            StoneColor color = maximizing ? StoneColor.White : StoneColor.Black;
            var moves = GetValidMoves(board, color);
            moves = OrderMovesForAI(board, moves, color);

            if (moves.Count == 0)
            {
                StoneColor enemy = color == StoneColor.White ? StoneColor.Black : StoneColor.White;
                if (GetValidMoves(board, enemy).Count == 0)
                    return EvaluateEndgame(board);

                int v = MiniMax(board, depth - 1, !maximizing, alpha, beta);
                TT[hash] = v;
                return v;
            }

            int best;

            if (maximizing)
            {
                best = int.MinValue;
                foreach (var (x, y) in moves)
                {
                    var next = (StoneColor[,])board.Clone();
                    ApplyMove(next, x, y, StoneColor.White);
                    best = Math.Max(best, MiniMax(next, depth - 1, false, alpha, beta));
                    alpha = Math.Max(alpha, best);
                    if (beta <= alpha) break;
                }
            }
            else
            {
                best = int.MaxValue;
                foreach (var (x, y) in moves)
                {
                    var next = (StoneColor[,])board.Clone();
                    ApplyMove(next, x, y, StoneColor.Black);
                    best = Math.Min(best, MiniMax(next, depth - 1, true, alpha, beta));
                    beta = Math.Min(beta, best);
                    if (beta <= alpha) break;
                }
            }

            TT[hash] = best;
            return best;
        }

        // ----------------- 評価関数 -----------------
        static int EvaluateBoardFull(StoneColor[,] board)
        {
            int[,] weight =
            {
                {120, -20, 20, 5, 5, 20, -20, 120},
                {-20, -40, -5, -5, -5, -5, -40, -20},
                {20, -5, 15, 3, 3, 15, -5, 20},
                {5, -5, 3, 3, 3, 3, -5, 5},
                {5, -5, 3, 3, 3, 3, -5, 5},
                {20, -5, 15, 3, 3, 15, -5, 20},
                {-20, -40, -5, -5, -5, -5, -40, -20},
                {120, -20, 20, 5, 5, 20, -20, 120}
            };

            int score = 0;
            int whiteCount = 0, blackCount = 0;
            int emptyCount = board.Cast<StoneColor>().Count(s => s == StoneColor.None);

            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    if (board[x, y] == StoneColor.White)
                    {
                        score += weight[y, x];
                        whiteCount++;
                    }
                    else if (board[x, y] == StoneColor.Black)
                    {
                        score -= weight[y, x];
                        blackCount++;
                    }
                }

            // 石差加点（終盤重視）
            score += (whiteCount - blackCount) * (10 + (60 - emptyCount) / 2);

            // 安定石
            score += EvaluateStableStonesFull(board) * 15;

            // 裏返せる枚数
            score += EvaluateReversible(board, StoneColor.White) - EvaluateReversible(board, StoneColor.Black);

            // 危険マスペナルティ
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    if (!IsDangerSquare(x, y)) continue;
                    if (board[x, y] == StoneColor.White) score -= 200;
                    if (board[x, y] == StoneColor.Black) score += 200;
                }

            // 可動域
            score += EvaluateMobility(board) * 10;

            return score;
        }
        // 裏返せる石枚数を数える（自分と相手で差を出す）
        static int EvaluateReversible(StoneColor[,] board, StoneColor color)
        {
            int count = 0;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    if (board[x, y] != StoneColor.None) continue;
                    count += CountReversible(board, x, y, color);
                }
            return count;
        }

        static int EvaluateEndgame(StoneColor[,] board)
        {
            int blackCount = 0, whiteCount = 0;
            foreach (var s in board)
            {
                if (s == StoneColor.Black) blackCount++;
                else if (s == StoneColor.White) whiteCount++;
            }
            return whiteCount - blackCount;
        }

        // --------------------------------------------
        static List<(int x, int y)> GetValidMoves(StoneColor[,] board, StoneColor color)
        {
            var moves = new List<(int x, int y)>();
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (board[x, y] == StoneColor.None && CountReversible(board, x, y, color) > 0)
                        moves.Add((x, y));
            return moves;
        }

        static List<(int x, int y)> GetValidMovesAfterMove(StoneColor[,] board, int x, int y, StoneColor color)
        {
            var temp = (StoneColor[,])board.Clone();
            ApplyMove(temp, x, y, color);
            return GetValidMoves(temp, color);
        }

        static int CountReversible(StoneColor[,] board, int x, int y, StoneColor color)
        {
            int count = 0;
            StoneColor enemy = color == StoneColor.White ? StoneColor.Black : StoneColor.White;

            foreach (var (dx, dy) in Directions)
            {
                int cx = x + dx, cy = y + dy, temp = 0;
                while (cx >= 0 && cx < 8 && cy >= 0 && cy < 8)
                {
                    if (board[cx, cy] == enemy) temp++;
                    else if (board[cx, cy] == color) { count += temp; break; }
                    else break;
                    cx += dx; cy += dy;
                }
            }
            return count;
        }

        static void ApplyMove(StoneColor[,] board, int x, int y, StoneColor color)
        {
            board[x, y] = color;
            StoneColor enemy = color == StoneColor.White ? StoneColor.Black : StoneColor.White;

            foreach (var (dx, dy) in Directions)
            {
                List<(int, int)> temp = new List<(int, int)>();
                int cx = x + dx, cy = y + dy;
                while (cx >= 0 && cx < 8 && cy >= 0 && cy < 8)
                {
                    if (board[cx, cy] == enemy) temp.Add((cx, cy));
                    else if (board[cx, cy] == color) { foreach (var p in temp) board[p.Item1, p.Item2] = color; break; }
                    else break;
                    cx += dx; cy += dy;
                }
            }
        }

        static bool IsDangerSquare(int x, int y)
        {
            return (x == 1 && y == 1) || (x == 6 && y == 1) || (x == 1 && y == 6) || (x == 6 && y == 6) ||
                   (x == 1 && y == 0) || (x == 0 && y == 1) || (x == 6 && y == 0) || (x == 7 && y == 1) ||
                   (x == 0 && y == 6) || (x == 1 && y == 7) || (x == 7 && y == 6) || (x == 6 && y == 7);
        }

        static int EvaluateStableStonesFull(StoneColor[,] board)
        {
            int score = 0;
            StoneColor[] colors = { StoneColor.White, StoneColor.Black };
            foreach (var color in colors)
                for (int i = 0; i < 8; i++)
                {
                    if (board[i, 0] == StoneColor.White) score += 5;
                    else if (board[i, 0] == StoneColor.Black) score -= 5;

                    if (board[i, 7] == StoneColor.White) score += 5;
                    else if (board[i, 7] == StoneColor.Black) score -= 5;
                }
            return score;
        }

        static int EvaluateMobility(StoneColor[,] board)
        {
            return (GetValidMoves(board, StoneColor.White).Count - GetValidMoves(board, StoneColor.Black).Count) * 5;
        }
    }
}
