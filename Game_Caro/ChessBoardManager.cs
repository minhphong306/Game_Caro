using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Game_Caro.Properties;

namespace Game_Caro {
    public class ChessBoardManager {
        #region Properties
        public Panel PnlChessBoard { get; set; }
        public TextBox txtPlayerName { get; set; }
        public TextBox TxtEnemyName { get; set; }
        public RichTextBox RtbChatArea { get; set; }
        public Label LbTurn { get; set; }
        public List<Player> Players { get; set; }
        public List<List<Button>> Matrix { get; set; }
        private Stack<TurnInfo> turnStack;
        private SocketManager gameSocket;
        public int CurrentPlayer { get; set; }
        public bool IsMyTurn { get; set; }
        public int GAME_MODE { get; set; }
        private event EventHandler playerMarkEvent;
        private event EventHandler endGameEvent;
        public event EventHandler PlayerMarkEvent {
            add {
                playerMarkEvent += value;
            }
            remove {
                playerMarkEvent -= value;
            }
        }
        public event EventHandler EndGameEvent {
            add {
                endGameEvent += value;
            }
            remove {
                endGameEvent -= value;
            }
        }

        #endregion

        #region Constructor
        public ChessBoardManager() {
            Players = new List<Player>()
            {
                new Player()
                {
                    Name = "Server",
                    Mark = Resources.P1
                },
                new Player()
                {
                    Name = "Client",
                    Mark = Resources.P2
                }
            };
            turnStack = new Stack<TurnInfo>();
            gameSocket = new SocketManager();
        }

        #endregion

        #region Methods

        public void DrawChessBoard() {
            CurrentPlayer = 0;
            PnlChessBoard.Controls.Clear();
            turnStack = new Stack<TurnInfo>();
            Matrix = new List<List<Button>>();
            Button oldButton = new Button() {
                Width = 0,
                Location = new Point(0, 0)
            };

            for (int i = 0; i < CaroConstant.CHESS_BOARD_HEIGH; i++) {
                Matrix.Add(new List<Button>());
                for (int j = 0; j < CaroConstant.CHESS_BOARD_WIDTH; j++) {
                    Button btn = new Button() {
                        Width = CaroConstant.CHESS_WIDTH,
                        Height = CaroConstant.CHESS_HEIGHT,
                        Location = new Point(oldButton.Location.X + oldButton.Width,
                                            oldButton.Location.Y),
                        BackgroundImageLayout = ImageLayout.Stretch,
                        Tag = i.ToString()
                    };
                    btn.Click += ClickEvent;
                    PnlChessBoard.Controls.Add(btn);
                    Matrix[i].Add(btn);
                    oldButton = btn;
                }
                oldButton.Location = new Point(0, oldButton.Location.Y + CaroConstant.CHESS_HEIGHT);
                oldButton.Width = 0;
                oldButton.Height = 0;
            }
            PnlChessBoard.Enabled = false;
        }

        public void StartGame() {
            PnlChessBoard.Enabled = true;
            // Server start first
            CurrentPlayer = 0;
            if (GAME_MODE == CaroConstant.GAME_MODE_NORMAL) {
                LbTurn.Text = CaroConstant.STRING_TURN_NORMAL_PLAYER1;
            }
            else if (GAME_MODE == CaroConstant.GAME_MODE_LAN) {
                LbTurn.Text = gameSocket.isServer ? CaroConstant.STRING_TURN_LAN_MY_TURN : CaroConstant.STRING_TURN_LAN_ENEMY_TURN;
                IsMyTurn = gameSocket.isServer;
            }
        }

        private void EndGame() {
            if (endGameEvent != null) {
                endGameEvent(this, new EventArgs());
            }
        }

        public bool Undo() {
            // if stack empty, can't undo
            if (turnStack.Count < 1) {
                return false;
            }
            // remove background image
            TurnInfo lastTurn = turnStack.Pop();
            Point lastCoordinate = lastTurn.Coordinate;
            Matrix[lastCoordinate.X][lastCoordinate.Y].BackgroundImage = null;

            CurrentPlayer = lastTurn.CurrentPlayer;
            ChangePlayer();

            return true;
        }

        private bool IsEndGame(Button btn) {
            return IsEndHorizontal(btn) || IsEndVertical(btn) ||
                   IsEndPrimaryDiagonal(btn) || IsEndSubDiagonal(btn);
        }

        private Point GetChessPoint(Button btn) {
            Point point = new Point();
            int horizontal = Convert.ToInt32(btn.Tag);
            int vertical = Matrix[horizontal].IndexOf(btn);
            point.X = horizontal;
            point.Y = vertical;
            return point;
        }

        public void MarkOther(Point point) {
            MarkASquare(Matrix[point.X][point.Y]);
            ChangePlayer();
        }

        private void MarkASquare(Button btn) {
            btn.BackgroundImage = Players[CurrentPlayer].Mark;
        }

        private void ChangePlayer() {
            if (GAME_MODE == CaroConstant.GAME_MODE_NORMAL) {
                LbTurn.Text = CurrentPlayer == 0 ? CaroConstant.STRING_TURN_NORMAL_PLAYER1 : CaroConstant.STRING_TURN_NORMAL_PLAYER2;
            }
            else if (GAME_MODE == CaroConstant.GAME_MODE_LAN) {
                IsMyTurn = !IsMyTurn;
                LbTurn.Text = IsMyTurn ? CaroConstant.STRING_TURN_LAN_MY_TURN : CaroConstant.STRING_TURN_LAN_ENEMY_TURN;
            }
            else {

            }
            CurrentPlayer = CurrentPlayer == 0 ? 1 : 0;
        }

        public void CreateServer() {
            // Create server
            gameSocket.CreateServer();

            // Send name to client
            SocketData initData = new SocketData();
            initData.Command = (int)SocketCommand.SEND_NAME;
            initData.Message = txtPlayerName.Text;
            SendData(initData);
        }

        public bool CreateClient(string serverIP) {
            if (gameSocket.ConnectServer(serverIP)) {
                SocketData initData = new SocketData();
                initData.Command = (int)SocketCommand.SEND_NAME;
                initData.Message = txtPlayerName.Text;
                SendData(initData);
                return true;
            }

            return false;
        }

        public void SendData(SocketData data) {
            gameSocket.Send(data);
        }

        public object ReceiveData() {
            return gameSocket.Receive();
        }
        #endregion

        #region Game Event

        private void ClickEvent(object sender, EventArgs e) {
            if (IsMyTurn)
            {
                Button btn = sender as Button;
                if (btn.BackgroundImage != null)
                    return;

                TurnInfo nextTurn = new TurnInfo()
                {
                    CurrentPlayer = CurrentPlayer,
                    Coordinate = GetChessPoint(btn)
                };
                turnStack.Push(nextTurn);

                SocketData positonData = new SocketData();
                positonData.Command = (int) SocketCommand.SEND_COORDINATE;
                positonData.Point = GetChessPoint(btn);
                gameSocket.Send(positonData);

                MarkASquare(btn);
                ChangePlayer();
                if (playerMarkEvent != null)
                {
                    playerMarkEvent(this, new EventArgs());
                }

                if (IsEndGame(btn))
                {
                    EndGame();
                    if (GAME_MODE == CaroConstant.GAME_MODE_LAN)
                    {
                        SocketData gameOverData = new SocketData();
                        gameOverData.Command = (int) SocketCommand.END_GAME;
                        SendData(gameOverData);
                    }
                }

            }
            else
            {
                MessageBox.Show("Chưa tới lượt của bạn");
            }
        }

        #endregion

        #region Game Rule

        private bool IsEndHorizontal(Button btn) {
            Point point = GetChessPoint(btn);
            int count = 0;
            for (int i = point.Y + 1; i < CaroConstant.CHESS_BOARD_WIDTH; i++) {
                if (Matrix[point.X][i].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }

            for (int i = point.Y; i >= 0; i--) {
                if (Matrix[point.X][i].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }
            return count > 4;
        }
        private bool IsEndVertical(Button btn) {
            Point point = GetChessPoint(btn);
            int count = 0;
            for (int i = point.X + 1; i < CaroConstant.CHESS_BOARD_HEIGH; i++) {
                if (Matrix[i][point.Y].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }

            for (int i = point.X; i >= 0; i--) {
                if (Matrix[i][point.Y].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }
            return count > 4;
        }
        private bool IsEndPrimaryDiagonal(Button btn) {
            Point point = GetChessPoint(btn);
            int count = 0;
            for (int i = point.X, j = point.Y; i < CaroConstant.CHESS_BOARD_HEIGH && i < CaroConstant.CHESS_BOARD_WIDTH; i++, j++) {
                if (Matrix[i][j].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }

            for (int i = point.X - 1, j = point.Y - 1; i >= 0 && j >= 0; i--, j--) {
                if (Matrix[i][j].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }
            return count > 4;
        }
        private bool IsEndSubDiagonal(Button btn) {
            Point point = GetChessPoint(btn);
            int count = 0;
            for (int i = point.X, j = point.Y; i >= 0 && j < CaroConstant.CHESS_BOARD_WIDTH; i--, j++) {
                if (Matrix[i][j].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }
            for (int i = point.X + 1, j = point.Y - 1; i < CaroConstant.CHESS_BOARD_HEIGH && j >= 0; i++, j--) {
                if (Matrix[i][j].BackgroundImage == btn.BackgroundImage) {
                    count++;
                }
                else {
                    break;
                }
            }
            return count > 4;
        }

        #endregion

    }
}
