using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game_Caro {
    public partial class Form1 : Form {
        #region Properties
        private ChessBoardManager chessBoardManager;
        private SocketManager socket;
        #endregion

        public Form1() {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            chessBoardManager = new ChessBoardManager() {
                PnlChessBoard = pnlChessboard,
                LbTurn = lbTurn,
                RtbChatArea = rtbChatArea,
                txtPlayerName = txtPlayerName,
                TxtEnemyName = txtEnemyName
            };
            socket = new SocketManager();
            chessBoardManager.PlayerMarkEvent += ChessBoard_PlayerMarkEvent;
            chessBoardManager.EndGameEvent += ChessBoard_EndGameEvent;
            chessBoardManager.DrawChessBoard();
        }

        private void ChessBoard_EndGameEvent(object sender, EventArgs e) {
            EndGame();
        }

        private void ChessBoard_PlayerMarkEvent(object sender, EventArgs e) {

        }


        void EndGame() {
            pnlChessboard.Enabled = false;
            undoToolStripMenuItem.Enabled = false;
            if (chessBoardManager.IsMyTurn) {
                MessageBox.Show("Đối thủ đã thắng");
            }
        }

        void NewTwoPlayerGame() {
            undoToolStripMenuItem.Enabled = true;
            chessBoardManager.DrawChessBoard();
            chessBoardManager.GAME_MODE = CaroConstant.GAME_MODE_NORMAL;
            chessBoardManager.StartGame();
        }

        void NewLanGame() {
            undoToolStripMenuItem.Enabled = true;
            chessBoardManager.DrawChessBoard();
            chessBoardManager.GAME_MODE = CaroConstant.GAME_MODE_LAN;
            MessageBox.Show("Vui lòng nhập địa chỉ IP của máy server vào ô bên,\nhoặc bấm làm máy chủ nếu bạn là máy chủ", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        void Undo() {
            if (!chessBoardManager.Undo()) {
                MessageBox.Show("Không thể undo!");
            }
        }

        void Quit() {
            if (MessageBox.Show("Bạn có chắc muốn thoát?", "Thoát trò chơi", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                Application.Exit();
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e) {
            NewTwoPlayerGame();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
            Undo();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e) {
            Quit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (MessageBox.Show("Bạn có chắc muốn thoát?", "Thoát trò chơi", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) {
                e.Cancel = true;
            }

        }

        private void btnConnect_Click(object sender, EventArgs e) {
            string serverIP = txtIPServer.Text;
            if (String.IsNullOrEmpty(serverIP)) {
                MessageBox.Show("Vui lòng nhập vào IP Server", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try {
                if (!chessBoardManager.CreateClient(serverIP)) {
                    MessageBox.Show("Không thể kết nối đến server ở địa chỉ " + serverIP, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                chessBoardManager.StartGame();
                chessBoardManager.GAME_MODE = CaroConstant.GAME_MODE_LAN;
                //chessBoardManager.CurrentPlayer = 
                lbStatus.Text = "Bạn đang là máy khách";
                Listen();
            }
            catch (Exception) {
                throw;
            }
        }

        private void Form1_Shown(object sender, EventArgs e) {
            string IPAddress = socket.GetLocalIPv4(NetworkInterfaceType.Wireless80211);

            if (string.IsNullOrEmpty(IPAddress)) {
                IPAddress = socket.GetLocalIPv4(NetworkInterfaceType.Ethernet);
            }

            txtMyIP.Text = IPAddress;
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e) {
            About frmAbout = new About();
            frmAbout.StartPosition = FormStartPosition.CenterParent;
            frmAbout.ShowDialog();
        }

        private void playViaLANToolStripMenu_Click(object sender, EventArgs e) {
            NewLanGame();
            chessBoardManager.StartGame();
        }

        private void btnCreateServer_Click(object sender, EventArgs e) {
            chessBoardManager.GAME_MODE = CaroConstant.GAME_MODE_LAN;

            Thread listenThread = new Thread(() => {
                chessBoardManager.CreateServer();
                chessBoardManager.StartGame();
                Listen();

            });
            listenThread.IsBackground = true;
            listenThread.Start();
            lbStatus.Text = "Bạn đang là máy chủ";
        }

        private void Listen() {
            Thread listenThread = new Thread(() => {
                while (true)
                {
                    try {
                        SocketData data = (SocketData)chessBoardManager.ReceiveData();
                        ProcessData(data);
                    }
                    catch (Exception e) {
                        MessageBox.Show("Đối thủ đã thoát");
                    }
                }
                
            });
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        private void ProcessData(SocketData data) {
            switch (data.Command) {
                case (int)SocketCommand.SEND_NAME:
                    txtEnemyName.Text = data.Message;
                    MessageBox.Show(data.Message + " đã kết nối");
                    break;
                //case (int)SocketCommand.NEW_GAME:
                //    this.Invoke((MethodInvoker)(() => {
                //        NewGame();
                //        pnlChessBoard.Enabled = false;
                //    }));
                //    break;
                case (int)SocketCommand.SEND_COORDINATE:
                    chessBoardManager.MarkOther(data.Point);
                    break;
                //case (int)SocketCommand.UNDO:
                //    Undo();
                //    prcbCoolDown.Value = 0;
                //    break;
                case (int)SocketCommand.END_GAME:
                    if (chessBoardManager.IsMyTurn) {
                        MessageBox.Show("Đối thủ đã thắng");
                    }
                    else {
                        MessageBox.Show("Bạn đã thắng cuộc");
                    }
                    break;
                case (int)SocketCommand.SEND_MESSAGE:
                    string msg = data.Message;
                    string agent = txtEnemyName.Text;
                    rtbChatArea.AppendText(agent + ": " + msg + "\n");
                    break;
                    //case (int)SocketCommand.QUIT:
                    //    tmCoolDown.Stop();
                    //    MessageBox.Show("Người chơi đã thoát");
                    //    break;
                    //default:
                    //    break;
            }
        }

        private void btnChat_Click(object sender, EventArgs e) {
            // Send to enemy
            string msg = txtChatInput.Text;
            SocketData chatData = new SocketData();
            chatData.Command = (int)SocketCommand.SEND_MESSAGE;
            chatData.Message = msg;
            chessBoardManager.SendData(chatData);
            txtChatInput.Text = "";

            // Display on my chat area
            rtbChatArea.Text += txtPlayerName.Text + ": " + msg + "\n";
        }
    }
}
