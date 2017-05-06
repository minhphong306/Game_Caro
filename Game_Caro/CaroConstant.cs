using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Caro {
    public class CaroConstant {
        public static int CHESS_WIDTH = 30;
        public static int CHESS_HEIGHT = 30;

        public static int CHESS_BOARD_WIDTH = 20;
        public static int CHESS_BOARD_HEIGH = 17;

        public static int COOL_DOWN_STEP = 100;
        public static int COOL_DOWN_TOTAL = 10000;
        public static int COOL_DOWN_INTERVAL = 100;

        public static int GAME_MODE_NORMAL = 0;
        public static int GAME_MODE_LAN = 1;
        public static int GAME_MODE_AI = 2;

        public static string STRING_GAME_MODE_NORMAL = "Chế độ 2 người chơi";
        public static string STRING_GAME_MODE_LAN = "Chế độ chơi qua LAN";
        public static string STRING_GAME_MODE_AI = "Bạn đang chơi với máy";
        
        public static string STRING_TURN_LAN_MY_TURN = "Lượt của bạn";
        public static string STRING_TURN_LAN_ENEMY_TURN = "Lượt đối thủ";
        public static string STRING_TURN_NORMAL_PLAYER1 = "Lượt người 1 ";
        public static string STRING_TURN_NORMAL_PLAYER2 = "Lượt người 2";
        public static string STRING_TURN_AI_YOUR_TURN = "Lượt của bạn";
        public static string STRING_TURN_AI_COMPUTER_TURN = "Lượt của máy";
    }
}
