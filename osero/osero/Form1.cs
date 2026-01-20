using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using osero; // AI.cs の namespace をインポート

namespace osero
{
    public partial class Form1 : Form
    {
        private readonly Random rng = new Random();
        private bool isFullScreen = false;
        Point leftTopPoint = new Point(30, 30);
        Stone[,] StonePosition = new Stone[8, 8];
        private bool isYour; 
        private ToolStripMenuItem モードToolStripMenuItem;
        private ToolStripMenuItem playerVsAIMenuItem;
        private ToolStripMenuItem playerVsPlayerMenuItem;
        private ToolStripMenuItem playerVsThreeMenuItem;
        private Timer turnTimer;
        private int timeLeft = 30; // 制限時間（秒）

            enum Difficulty
        {
            優しい, // Easy
            普通,   //Normal
            鬼,     //Hard
        }
        enum GameMode
        {
            人VsAI,
            人Vs人,
            人Vs人Vs人
        }

        GameMode currentGameMode = GameMode.人VsAI; // デフォルトはAI戦
       
        // 現在の手番（黒 or 白）
        private StoneColor currentPlayer = StoneColor.Black;

        Difficulty currentDifficulty = Difficulty.鬼; // 今は固定
        string GetDifficultyName(Difficulty diff)
        {
            switch (diff)
            {
                case Difficulty.優しい:
                    return "優しい";
                case Difficulty.普通:
                    return "普通";
                case Difficulty.鬼:
                    return "鬼";
                default:
                    return diff.ToString();
            }
        }

        // 8方向
        readonly (int dx, int dy)[] Directions =
        {
            (0, -1),  // 上
            (0, 1),   // 下
            (-1, 0),  // 左
            (1, 0),   // 右
            (-1, -1), // 左上
            (1, -1),  // 右上
            (-1, 1),  // 左下
            (1, 1),   // 右下
        };

        // 1マスの大きさ（必要に応じて変更）
        private const int CELL_SIZE = 40;

        public Form1()
        {
            InitializeComponent();
            this.モードToolStripMenuItem = new ToolStripMenuItem();
            this.playerVsAIMenuItem = new ToolStripMenuItem();
            this.playerVsPlayerMenuItem = new ToolStripMenuItem();
            this.playerVsThreeMenuItem = new ToolStripMenuItem();

            // モードメニュー
            this.モードToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
            this.playerVsAIMenuItem,
            this.playerVsPlayerMenuItem,
            this.playerVsThreeMenuItem
            });
            this.モードToolStripMenuItem.Name = "モードToolStripMenuItem";
            this.モードToolStripMenuItem.Text = "モード";
            this.menuStrip1.Items.Add(this.モードToolStripMenuItem);

            // 各項目
            this.playerVsAIMenuItem.Name = "playerVsAIMenuItem";
            this.playerVsAIMenuItem.Text = "プレイヤー vs AI";
            this.playerVsAIMenuItem.Click += new EventHandler(this.playerVsAIMenuItem_Click);

            this.playerVsPlayerMenuItem.Name = "playerVsPlayerMenuItem";
            this.playerVsPlayerMenuItem.Text = "プレイヤー vs プレイヤー";
            this.playerVsPlayerMenuItem.Click += new EventHandler(this.playerVsPlayerMenuItem_Click);

            this.playerVsThreeMenuItem.Name = "playerVsThreeMenuItem";
            this.playerVsThreeMenuItem.Text = "プレイヤー vs プレイヤー vs プレイヤー";
            this.playerVsThreeMenuItem.Click += new EventHandler(this.playerVsThreeMenuItem_Click);
            //タイマー
            turnTimer = new Timer();
            turnTimer.Interval = 1000;
            turnTimer.Tick += TurnTimer_Tick;

            // フルスクリーン化
            this.FormBorderStyle = FormBorderStyle.None; // 枠なしにする
            this.WindowState = FormWindowState.Maximized; // 最大化
            this.TopMost = true; // 常に前面に表示（任意）

            // キー入力を受け取れるようにする
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown; // イベント登録

            // 初期化処理
            toolStripStatusLabel1.Font = new Font("Meiryo", 20, FontStyle.Bold);  // ★文字を大きくする
            CreatePictureBoxes();
            GameStart();
            this.Resize += Form1_Resize;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            ResizeBoard();

            // 画面サイズに応じた基本フォントサイズ
            float baseFontSize = Math.Max(12, this.ClientSize.Width / 40f);

            // MenuStrip のフォント変更
            if (menuStrip1 != null)
            {
                menuStrip1.Font = new Font("Meiryo", baseFontSize, FontStyle.Bold);
                menuStrip1.AutoSize = false;                // 自動サイズ OFF
                menuStrip1.Height = (int)(baseFontSize * 2); // 高さを指定
                menuStrip1.PerformLayout();                 // 再描画
            }

            // StatusStrip のフォント変更
            if (statusStrip1 != null)
            {
                statusStrip1.Font = new Font("Meiryo", baseFontSize, FontStyle.Bold);
                statusStrip1.AutoSize = false;              // 自動サイズ OFF
                statusStrip1.Height = (int)(baseFontSize * 1.5); // 高さを指定
                statusStrip1.PerformLayout();
            }
        }
        //おせろの盤面の作成
        private void CreatePictureBoxes()
            {
                for (int row = 0; row < 8; row++)
                {
                    for (int colum = 0; colum < 8; colum++)
                    {
                        Stone stone = new Stone(colum, row);
                        stone.Parent = this;
                        stone.Size = new Size(CELL_SIZE, CELL_SIZE);
                        stone.BorderStyle = BorderStyle.FixedSingle;
                        stone.Location = new Point(leftTopPoint.X + colum * CELL_SIZE, leftTopPoint.Y + row * CELL_SIZE);
                        StonePosition[colum, row] = stone;
                        stone.StoneClick += Box_PictureBoxExClick;
                        stone.BackColor = Color.Green;
                    }
                }
            }
        //esc押すと画面切り替え
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (!isFullScreen)
                {
                    // フルスクリーンにする
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                    this.TopMost = true;
                    isFullScreen = true;
                }
                else
                {
                    // 通常画面に戻す
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Normal;
                    this.TopMost = false;
                    isFullScreen = false;
                }
            }
        }
        //画面サイズが変わっても盤面が自動で大きさ変える
        private void ResizeBoard()
        {
            int boardWidth = this.ClientSize.Width - 110;  // 左右マージン
            int boardHeight = this.ClientSize.Height - 110; // 上下マージン
            int size = Math.Min(boardWidth, boardHeight);  // 正方形にする

            int cellSize = size / 8;

            leftTopPoint = new Point(
                (this.ClientSize.Width - cellSize * 8) / 2,
                (this.ClientSize.Height - cellSize * 8) / 2
            );

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Stone s = StonePosition[x, y];
                    s.Size = new Size(cellSize, cellSize);
                    s.Location = new Point(leftTopPoint.X + x * cellSize, leftTopPoint.Y + y * cellSize);
                }
            }
        }
        //モード切替
        private void playerVsAIMenuItem_Click(object sender, EventArgs e)
        {
            currentGameMode = GameMode.人VsAI;
            toolStripStatusLabel1.Text = "モード：プレイヤー vs AI";
            GameStart();
        }
        private void playerVsPlayerMenuItem_Click(object sender, EventArgs e)
        {
            currentGameMode = GameMode.人Vs人;
            toolStripStatusLabel1.Text = "モード：プレイヤー１ vs プレイヤー２";
            GameStart();
        }
        private void playerVsThreeMenuItem_Click(object sender, EventArgs e)
        {
            currentGameMode = GameMode.人Vs人Vs人;
            toolStripStatusLabel1.Text = "モード：三人対戦";
            GameStart();
        }
        //難易度変更
        private void UpdateDifficultyDisplay()
        {
            toolStripStatusLabel1.Text = $"難易度：{GetDifficultyName(currentDifficulty)}";

            switch (currentDifficulty)
            {
                case Difficulty.優しい:
                    toolStripStatusLabel1.ForeColor = Color.Green;
                    break;
                case Difficulty.普通:
                    toolStripStatusLabel1.ForeColor = Color.Orange;
                    break;
                case Difficulty.鬼:
                    toolStripStatusLabel1.ForeColor = Color.Red;
                    break;
                default:
                    toolStripStatusLabel1.ForeColor = Color.Black;
                    break;
            }
        }
        void GameStart()
        {
            UpdateDifficultyDisplay();

            // 盤面をクリア
            foreach (var stone in StonePosition)
                stone.StoneColor = StoneColor.None;

            if (currentGameMode == GameMode.人Vs人Vs人)
            {
                // 三人制初期配置（中央付近に三角形）
                StonePosition[3, 3].StoneColor = StoneColor.Black;

                StonePosition[4, 4].StoneColor = StoneColor.White;
                StonePosition[3, 5].StoneColor = StoneColor.White;

                StonePosition[3, 4].StoneColor = StoneColor.Red;
                StonePosition[4, 3].StoneColor = StoneColor.Red;
                StonePosition[4, 5].StoneColor = StoneColor.Red;
            }
            else
            {
                // 二人制は従来通り
                StonePosition[3, 3].StoneColor = StoneColor.Black;
                StonePosition[4, 4].StoneColor = StoneColor.Black;

                StonePosition[3, 4].StoneColor = StoneColor.White;
                StonePosition[4, 3].StoneColor = StoneColor.White;
            }

            // 先手は黒
            currentPlayer = StoneColor.Black;

            // ハイライト
            HighlightValidMoves(currentPlayer);

            // AI戦なら黒がプレイヤーの場合だけ有効
            isYour = (currentGameMode == GameMode.人VsAI && currentPlayer == StoneColor.Black);

            // ステータス表示
            if (currentGameMode == GameMode.人VsAI)
            {
                toolStripStatusLabel1.Text = $"難易度：{GetDifficultyName(currentDifficulty)}       {currentPlayer} の番です";
            }
            else
            {
                toolStripStatusLabel1.Text = $"{currentPlayer} の番です";
            }


        }
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameStart();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //なにかいれてもよし
        }

        //おせろのメイン処理
        private async void Box_PictureBoxExClick(int x, int y)
        {
            // AI戦でプレイヤー以外は操作不可
            if (currentGameMode == GameMode.人VsAI && !isYour)
                return;

            // すでに石がある場所には置けない
            if (StonePosition[x, y].StoneColor != StoneColor.None)
            {
                toolStripStatusLabel1.Text = "ここにはすでに石があります";
                return;
            }

            // 打てるか確認
            var stonesToFlip = GetReverseStones(x, y, currentPlayer);
            if (stonesToFlip.Count == 0)
            {
                toolStripStatusLabel1.Text = "ここには打てません";
                return;
            }

            // 石を置く
            StonePosition[x, y].StoneColor = currentPlayer;
            foreach (var s in stonesToFlip)
                s.StoneColor = currentPlayer;

            // 描画更新
            StonePosition[x, y].Invalidate();
            foreach (var s in stonesToFlip)
                s.Invalidate();
            Application.DoEvents();

            // 終局判定
            if (CheckGameSet())
                return;

            // 次の手番に切り替え
            turnTimer.Stop();
            currentPlayer = NextPlayer(currentPlayer);
            StartTurn();


            if (currentGameMode == GameMode.人VsAI)
            {
                isYour = false;
                await EnemyThink();
            }
            else
            {
                HighlightValidMoves(currentPlayer);
                if (currentGameMode == GameMode.人VsAI)
                {
                    toolStripStatusLabel1.Text = $"難易度：{GetDifficultyName(currentDifficulty)}       {currentPlayer} の番です";
                }
                else
                {
                    toolStripStatusLabel1.Text = $"{currentPlayer} の番です";
                }

                // パス判定
                int passCount = 0;
                while (!HasValidMove(currentPlayer))
                {
                    string ColorName(StoneColor c)
                    {
                        switch (c)
                        {
                            case StoneColor.Black:
                                return "黒";
                            case StoneColor.White:
                                return "白";
                            case StoneColor.Red:
                                return "赤";
                            default:
                                return "";
                        }
                    }
                    StoneColor passedPlayer = currentPlayer;
                    currentPlayer = NextPlayer(currentPlayer);

                    toolStripStatusLabel1.Text =
                        $"{ColorName(passedPlayer)} はパスです       {ColorName(currentPlayer)} の番です";

                    passCount++;
                    currentPlayer = NextPlayer(currentPlayer);

                    if (passCount >= (currentGameMode == GameMode.人Vs人Vs人 ? 3 : 2))
                    {
                        OnGameset();
                        return;
                    }
                    HighlightValidMoves(currentPlayer);
                }
            }
        }
        private void TurnTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;

            toolStripStatusLabel1.Text =
                $"{currentPlayer} の番です　残り {timeLeft} 秒";

            if (timeLeft <= 0)
            {
                turnTimer.Stop();
                TimeUp();
            }
        }
        void StartTurn()
        {
            int passCount = 0;

            while (!HasValidMove(currentPlayer))
            {
                toolStripStatusLabel1.Text =
                    $"{currentPlayer} は置ける場所がないためパスです";

                currentPlayer = NextPlayer(currentPlayer);
                passCount++;

                // ★ 終局判定（ここが超重要）
                if (passCount >= (currentGameMode == GameMode.人Vs人Vs人 ? 3 : 2))
                {
                    turnTimer.Stop();   // ★ 念のため
                    OnGameset();
                    return;             // ★ ここで完全終了
                }
            }

            // ★ ここまで来た = 終局していない
            timeLeft = 30;
            turnTimer.Start();
            HighlightValidMoves(currentPlayer);

            toolStripStatusLabel1.Text =
                $"{currentPlayer} の番です　残り {timeLeft} 秒";
        }

        void TimeUp()
        {
            turnTimer.Stop();
            // ★ まず終局判定
            if (CheckGameSet())
                return;

            toolStripStatusLabel1.Text =
                $"{currentPlayer} は時間切れでパスです";
            currentPlayer = NextPlayer(currentPlayer);

            if (currentGameMode == GameMode.人VsAI)
            {
                if (currentPlayer == StoneColor.Black)
                {
                    // プレイヤーの番
                    isYour = true;
                    StartTurn();
                }
                else
                {
                    // AI の番
                    isYour = false;
                    _ = EnemyThink(); // 非同期でAI開始
                }
            }
            else
            {
                // 人対人
                StartTurn();
            }
        }

        //AIの考え
        private async Task EnemyThink()
        {
            var aiColor = StoneColor.White;

            while (true)
            {
                // AI の合法手取得
                var compMoves = StonePosition.Cast<Stone>()
                    .Where(s => s.StoneColor == StoneColor.None &&
                                GetReverseStones(s.Colum, s.Row, aiColor).Any())
                    .ToList();

                if (compMoves.Count == 0)
                {
                    toolStripStatusLabel1.Text = "コンピュータはパスしました";
                    await Task.Delay(500);

                    // プレイヤーに合法手があれば交代
                    if (HasValidMove(StoneColor.Black))
                    {
                        currentPlayer = StoneColor.Black;
                        isYour = true;
                        HighlightValidMoves(currentPlayer);
                        toolStripStatusLabel1.Text = $"{currentPlayer} の番です";
                        return;
                    }
                    else
                    {
                        OnGameset();
                        return;
                    }
                }

                turnTimer.Stop();
                toolStripStatusLabel1.Text = "コンピュータが考えています…";
                await Task.Delay(200);

                // ★最強 AI 思考
                var board = CopyBoard();
                int x, y;

                if (currentDifficulty == Difficulty.優しい)
                {
                    var s = ThinkEasy(compMoves);
                    x = s.Colum;
                    y = s.Row;
                }
                else if (currentDifficulty == Difficulty.普通)
                {
                    var s = ThinkNormal(compMoves);
                    x = s.Colum;
                    y = s.Row;
                }
                else
                {
                    (x, y) = await Task.Run(() => AI.ThinkHard(board));
                }

                if (x == -1 || y == -1)
                {
                    // 打てる手がない場合パス
                    currentPlayer = StoneColor.Black;
                    isYour = true;
                    HighlightValidMoves(currentPlayer);
                    toolStripStatusLabel1.Text = $"{currentPlayer} の番です";
                    return;
                }

                // 石を置く
                StonePosition[x, y].StoneColor = aiColor;
                var flipList = GetReverseStones(x, y, aiColor);
                foreach (var s in flipList)
                    s.StoneColor = aiColor;

                // 描画更新
                StonePosition[x, y].Invalidate();
                foreach (var s in flipList)
                    s.Invalidate();
                Application.DoEvents();

                // 終局判定
                if (CheckGameSet())
                {
                    turnTimer.Stop();
                    return;
                }

                // 次の手がない場合、プレイヤーに交代
                if (HasValidMove(StoneColor.Black))
                {
                    currentPlayer = StoneColor.Black;
                    isYour = true;
                    HighlightValidMoves(currentPlayer);
                    toolStripStatusLabel1.Text = $"{currentPlayer} の番です";
                    StartTurn();
                    return;
                }

                // AI の次の手があればループ続行
            }
        }

        bool CheckGameSet()
        {
            bool blackHasMove = HasValidMove(StoneColor.Black);
            bool whiteHasMove = HasValidMove(StoneColor.White);
            bool redHasMove = currentGameMode == GameMode.人Vs人Vs人
                      ? HasValidMove(StoneColor.Red)
                      : true; // 二人制なら赤は無視

            // どちらの合法手もない → 終了
            if (!blackHasMove && !whiteHasMove && !redHasMove)
            {
                OnGameset();
                return true;
            }

            // 盤面が全部埋まっている場合も終局とする
            if (!StonePosition.Cast<Stone>().Any(s => s.StoneColor == StoneColor.None))
            {
                OnGameset();
                return true;
            }

            return false;
        }
        bool HasValidMove(StoneColor color)
        {
            // 三人制なら、黒・白・赤の合法手をそれぞれ確認する
            if (currentGameMode == GameMode.人Vs人Vs人)
            {
                if (color == StoneColor.Red || color == StoneColor.Black || color == StoneColor.White)
                {
                    return StonePosition.Cast<Stone>()
                        .Where(s => s.StoneColor == StoneColor.None)
                        .Any(s => GetReverseStones(s.Colum, s.Row, color).Any());
                }
            }
            else // 二人制または人VsAI
            {
                return StonePosition.Cast<Stone>()
                    .Where(s => s.StoneColor == StoneColor.None)
                    .Any(s => GetReverseStones(s.Colum, s.Row, color).Any());
            }

            return false;
        }
        //難易度ごとの強さ
        Stone ThinkEasy(List<Stone> moves)
        {
            return moves[rng.Next(moves.Count)];
        }
        Stone ThinkNormal(List<Stone> moves)
        {
            // ① 角
            var corner = moves.FirstOrDefault(s =>
                (s.Colum == 0 && s.Row == 0) ||
                (s.Colum == 7 && s.Row == 0) ||
                (s.Colum == 0 && s.Row == 7) ||
                (s.Colum == 7 && s.Row == 7));

            if (corner != null)
                return corner;

            // ② 危険マス回避
            var safeMoves = moves.Where(s =>
                !(
                    (s.Colum <= 1 && s.Row <= 1) ||
                    (s.Colum >= 6 && s.Row <= 1) ||
                    (s.Colum <= 1 && s.Row >= 6) ||
                    (s.Colum >= 6 && s.Row >= 6)
                )
            ).ToList();

            var target = safeMoves.Count > 0 ? safeMoves : moves;

            // ③ 裏返せる枚数最大
            return target
                .OrderByDescending(s =>
                    GetReverseStones(s.Colum, s.Row, StoneColor.White).Count)
                .First();
        }      
        //ハイライト消す
        void ClearHighlight()
        {
            foreach (var stone in StonePosition)
            {
                stone.IsHint = false;
                stone.Invalidate();
            }
        }
        //ゲーム終わった時の処理
        void OnGameset()
        {
            Console.WriteLine("=== OnGameset called ===");

            // 終局フラグを止める
            turnTimer?.Stop();
            isYour = false;

            var stones = StonePosition.Cast<Stone>();

            // 三人制対応
            int blackCount = stones.Count(s => s.StoneColor == StoneColor.Black);
            int whiteCount = stones.Count(s => s.StoneColor == StoneColor.White);
            int redCount = stones.Count(s => s.StoneColor == StoneColor.Red);

            string resultMessage;

            if (currentGameMode == GameMode.人Vs人Vs人)
            {
                // 勝者判定（三人制）
                int max = Math.Max(blackCount, Math.Max(whiteCount, redCount));
                List<string> winners = new List<string>();
                if (blackCount == max) winners.Add("黒");
                if (whiteCount == max) winners.Add("白");
                if (redCount == max) winners.Add("赤");

                if (winners.Count == 1)
                {
                    resultMessage = $"終局しました。黒 {blackCount} 対 白 {whiteCount} 対 赤 {redCount} で {winners[0]} の勝ちです。";
                }
                else
                {
                    resultMessage = $"終局しました。黒 {blackCount} 対 白 {whiteCount} 対 赤 {redCount} で 引き分けです。勝者: {string.Join(",", winners)}";
                }
            }
            else
            {
                // 二人制従来通り
                if (blackCount != whiteCount)
                {
                    string winner = blackCount > whiteCount ? "黒" : "白";
                    resultMessage = $"終局しました。黒 {blackCount} 対 白 {whiteCount} で {winner} の勝ちです。";
                }
                else
                {
                    resultMessage = $"終局しました。黒 {blackCount} 対 白 {whiteCount} で 引き分けです。";
                }
            }

            toolStripStatusLabel1.Text = resultMessage;
            statusStrip1.Invalidate();
            statusStrip1.Update();
        }
        //盤面を仮コピー
        StoneColor[,] CopyBoard()
        {
            var board = new StoneColor[8, 8];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    board[x, y] = StonePosition[x, y].StoneColor;
            return board;
        }
        private void ShowResultOverlay(int blackCount, int whiteCount)
        {
            throw new NotImplementedException();
        }
        //置ける場所探索
        List<Stone> GetValidMoves(StoneColor color)
        {
            return StonePosition.Cast<Stone>()
                .Where(s => s.StoneColor == StoneColor.None && GetReverseStones(s.Colum, s.Row, color).Any())
                .ToList();
        }
        //ハイライトの表示
        void HighlightValidMoves(StoneColor color)
        {
            foreach (var stone in StonePosition)
            {
                stone.IsHint = false;
                stone.Invalidate();
            }

            foreach (var stone in GetValidMoves(color))
            {
                stone.IsHint = true;
                stone.Invalidate();
            }
        }
        /// 次の手番を返す（モードに応じて切り替え）
        private StoneColor NextPlayer(StoneColor current)
        {
            if (currentGameMode == GameMode.人VsAI || currentGameMode == GameMode.人Vs人)
            {
                // 二人制
                return current == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            }
            else
            {
                // 三人制
                if (current == StoneColor.Black) return StoneColor.White;
                if (current == StoneColor.White) return StoneColor.Red;
                return StoneColor.Black; // Red の次は Black
            }
        }
        //裏返せる石を探す処理
        List<Stone> GetReverseStones(int x, int y, StoneColor color)
        {
            List<Stone> result = new List<Stone>();

            // 敵の判定
            List<StoneColor> enemies = new List<StoneColor>();
            if (currentGameMode == GameMode.人Vs人Vs人)
            {
                if (color == StoneColor.Black) enemies = new List<StoneColor> { StoneColor.White, StoneColor.Red };
                if (color == StoneColor.White) enemies = new List<StoneColor> { StoneColor.Black, StoneColor.Red };
                if (color == StoneColor.Red) enemies = new List<StoneColor> { StoneColor.Black, StoneColor.White };
            }
            else
            {
                enemies.Add(color == StoneColor.Black ? StoneColor.White : StoneColor.Black);
            }

            foreach (var (dx, dy) in Directions)
            {
                List<Stone> temp = new List<Stone>();
                int cx = x + dx;
                int cy = y + dy;

                while (cx >= 0 && cx < 8 && cy >= 0 && cy < 8)
                {
                    Stone current = StonePosition[cx, cy];

                    if (enemies.Contains(current.StoneColor))
                    {
                        temp.Add(current);
                    }
                    else if (current.StoneColor == color)
                    {
                        if (temp.Count > 0)
                            result.AddRange(temp);
                        break;
                    }
                    else // None
                    {
                        break;
                    }

                    cx += dx;
                    cy += dy;
                }
            }

            return result;
        }

        // 難易度変更ハンドラで呼ぶ
        private void easyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentDifficulty = Difficulty.優しい;
            UpdateDifficultyDisplay();
            GameStart();
        }
        private void normalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentDifficulty = Difficulty.普通;
            UpdateDifficultyDisplay();
            GameStart();
        }

        private void hardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentDifficulty = Difficulty.鬼;
            UpdateDifficultyDisplay();
            GameStart();
        }

        private void 難易度ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}