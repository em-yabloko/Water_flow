using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Data.SqlClient;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace Bars1
{    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();

            // X
            eks.MouseEnter += (s, a) =>
            { eks.ForeColor = Color.Red; };

            eks.MouseLeave += (s, a) =>
            { eks.ForeColor = Color.Indigo; };

            eks.MouseClick += (s, a) =>
            {
                this.Close();
                this.Dispose();
            };

            // minus
            menus.MouseEnter += (s, a) =>
            { menus.ForeColor = Color.LightSkyBlue; };

            menus.MouseLeave += (s, a) =>
            { menus.ForeColor = Color.Indigo; };

            menus.MouseClick += (s, a) =>
            { this.WindowState = FormWindowState.Minimized; };

        }

        // строка подключения
        //const string cs = @"server=localhost;port=3306;username=root;password=12345;database=new_asu";
        const string cs = @"server=192.168.0.104;port=3306;username=asu;password=@27750a;database=new_asu";

        // операции с таблицей sql
        private string init_sql = @"CREATE TABLE squagy ( Id INTEGER NOT NULL PRIMARY KEY, start_value INT UNSIGNED NULL DEFAULT 0, old_value INT UNSIGNED NULL DEFAULT 0, vrem INT UNSIGNED NULL DEFAULT 0, start_value2 INT UNSIGNED NULL DEFAULT 0, old_value2 INT UNSIGNED NULL DEFAULT 0)";
        MySqlConnection con = new MySqlConnection(cs);
        MySqlConnection coni = new MySqlConnection(cs);
        MySqlConnection cone = new MySqlConnection(cs);
        MySqlConnection cono = new MySqlConnection(cs);
        MySqlConnection cona = new MySqlConnection(cs);
        MySqlConnection cony = new MySqlConnection(cs);
        MySqlConnection conu = new MySqlConnection(cs);
        MySqlConnection cong = new MySqlConnection(cs);

        // moving form by header-panel
        bool drag = false;
        Point start_point = new Point(0, 0);
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            drag = true;
            start_point = new Point(e.X, e.Y);
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                Point p = PointToScreen(e.Location);
                this.Location = new Point(p.X - start_point.X, p.Y - start_point.Y);
            }
        }
        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        // массив заполняемый из БД
        bool connect = false;
        private async void Form2_Load(object sender, EventArgs e)
        {            
            Invoke((Action)(() =>
            {
                alarmo.Hide();
                connect = true;
            }));

            // проверка наличия таблицы
            bool is_exist = false;
                string exist = $"SELECT * FROM new_asu.squagy;";
            con.Open();
            MySqlCommand cmd = new MySqlCommand(exist, con);
            try
            {
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        // если таблица есть, то true. иначе false
                        var wewe = dr.GetString(0);
                        if (wewe != null)
                            is_exist = true;
                    }
                }
            }
            catch { is_exist = false; }
            con.Close();

            // если таблицы нет, создаю
            if (!is_exist)
            {
                try
                {
                    con.Open();
                    MySqlCommand init = new MySqlCommand(init_sql, con);                    
                        init.ExecuteNonQuery();
                        is_exist = true;                    
                    con.Close();
                }
                catch { is_exist = true; }
            }

            // если таблица есть, но она пуста то забивка нулями
            bool nulls = !Rows_null();  // пусто, только nulls
            if (is_exist && nulls)
            {
                Zero();         // забивка нулями
            }

            // запуск потока чтения из БД (без предварительного включения)
            New_thread();

            // проверка связи с сервером
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Connect_to_06();
                    }
                    catch
                    {
                        Invoke((Action)(() =>
                        {
                            alarmo.Show();
                            connect = false;
                        }));
                    }
                }
            });
        }

        // проверка отсутствия занулений
        private bool Rows_null()
        {
            bool gusto = false;
            try
            {
                con.Open();                
                string nul = $"SELECT * FROM squagy ORDER BY Id";
                MySqlCommand if_null = new MySqlCommand(nul, con);
                MySqlDataReader nu = if_null.ExecuteReader();
                gusto = nu.Read();      // true = что-то есть
                con.Close();
            }
            catch 
            {
                string nul = $"SELECT * FROM squagy ORDER BY Id";
                MySqlCommand if_null = new MySqlCommand(nul, con);
                MySqlDataReader nu = if_null.ExecuteReader();
                gusto = nu.Read();      // true = что-то есть
            }
            return gusto;            
        }

        // создание модбас соединения с сервером. порт 503, регистры 10 и 12
        string q1 = "...";
        string q2 = "...";
        bool sc = false;    // sc - успешное соединение
        private async void Connect_to_06()
        {
            const string ip = "192.168.0.105";
            const int port = 503;

            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var data  = new byte[] { 0, 0, 0, 0, 0, 6, 4, 3, 0, 1, 0, 8 };

            try
            {
                tcpSocket.Connect(tcpEndPoint);
                sc = true;
            }
            catch
            {
                if (sc)
                {
                    sc = false;
                    Invoke((Action)(() =>
                    {
                        alarmo.Show();
                        connect = false;
                    }));
                }
            }

            if (tcpSocket.Connected)
            {
                tcpSocket.Send(data);

                var answer = new StringBuilder();
                var size = 0;
                byte[] bag = new byte[256];

                await Task.Run(() =>
                {
                    do
                    {
                        try
                        {
                            size = tcpSocket.Receive(bag);
                            answer.Append(Encoding.UTF8.GetString(bag, 0, size));
                            if (bag[5] > 1)
                            {
                                Invoke((Action)(() =>
                                {
                                    alarmo.Hide();
                                    connect = true;
                                }));
                            }
                        }
                        catch
                        {
                            Invoke((Action)(() =>
                            {
                                alarmo.Show();
                                connect = false;
                            }));
                        }
                    } while (tcpSocket.Available > 0);
                });

                tcpSocket.Shutdown(SocketShutdown.Both);
                tcpSocket.Close();

                byte[] bufic = new byte[4];  // buffer
                bufic[0] = bag[12];
                bufic[1] = bag[11];
                bufic[2] = bag[10];
                bufic[3] = bag[9];
                var full_int1 = BitConverter.ToUInt32(bufic, 0);
                q1 = Convert.ToString(full_int1);
                Invoke((Action)(() =>
                {
                    za_smenu.Text = q1;
                }));

                bufic[0] = bag[16];
                bufic[1] = bag[15];
                bufic[2] = bag[14];
                bufic[3] = bag[13];
                var full_int2 = BitConverter.ToUInt32(bufic, 0);
                q2 = Convert.ToString(full_int2);
                Invoke((Action)(() =>
                {
                    za_vse.Text = q2;
                }));

                // а это уже накопленные объемы
                bufic[0] = bag[20];
                bufic[1] = bag[19];
                bufic[2] = bag[18];
                bufic[3] = bag[17];
                v1 = BitConverter.ToUInt32(bufic, 0);

                bufic[0] = bag[24];
                bufic[1] = bag[23];
                bufic[2] = bag[22];
                bufic[3] = bag[21];
                v2 = BitConverter.ToUInt32(bufic, 0);

                can = true;     // разрешаю чтение из БД
            }            
        }

        uint v1 = 0;
        uint v2 = 0;

        // новый поток
        bool can = false;
        bool stop = false;
        private void New_thread()
        {
            Thread nt = new Thread(() =>
            {
                Invoke((Action)(async () =>
                {
                    await Task.Run(async () =>
                    {
                        while (true)
                        {
                            if (can)            // если разрешил, читаю
                            {
                                bool its_time = Budilo(7, 45) || Budilo(19, 45);
                                                                
                                if (its_time && connect && !stop)
                                {
                                    uint r1 = Razn(1, v1);
                                    uint r2 = Razn(4, v2);
                                    Ty(r1, r2); // запись того, что от прежней смeны
                                    V_now(v1, v2);      // запись считанных объемов
                                    stop = true;
                                }
                                else if (its_time && !connect && !stop)
                                {
                                    await Task.Run(() =>
                                    {
                                        if (!connect)
                                        while (!connect) { }

                                        Ty(Razn(1, v1), Razn(4, v2)); // запись того, что от прежней смeны
                                        V_now(v1, v2);      // запись считанных объемов
                                    });
                                    stop = true;
                                }
                                else if (!its_time)
                                {
                                    if (connect && ((v1 == Razn(1, v1)) || (v2 == Razn(4, v2))))     // если в приборе что-то есть, а в БД еще нет
                                    {
                                        V_now(v1, v2);      // запись считанных объемов
                                    }
                                    stop = false;
                                }
                                else if (connect && ((v1==Razn(1, v1)) ||(v2== Razn(4, v2))))     // если в приборе что-то есть, а в БД еще нет
                                {
                                    V_now(v1, v2);      // запись считанных объемов
                                }

                                if (connect)
                                {
                                    // вывод старого
                                    Invoke((Action)(() =>
                                    {
                                        ranee.Text = Convert.ToString(Razn(1, v1));
                                        ranee2.Text = Convert.ToString(Razn(4, v2));
                                        ty1.Text = Convert.ToString(Prev()[0]);
                                        ty2.Text = Convert.ToString(Prev()[1]);
                                    }));
                                }
                                else
                                {
                                    // вывод старого
                                    Invoke((Action)(() =>
                                    {
                                        ranee.Text = Convert.ToString("нет связи");
                                        ranee2.Text = Convert.ToString("нет связи");
                                        ty1.Text = Convert.ToString("нет связи");
                                        ty2.Text = Convert.ToString("нет связи");
                                    }));
                                }
                            }
                        }
                    });
                }));
            });
            nt.Start();
        }

        // чтение того, что было в прежней смене
        private uint[] Prev()
        {
            string to_prev = $"SELECT * FROM squagy";
            uint[] uints = new uint[2];

            try
            {
                cong.Open();
            }
            catch { }
            MySqlCommand v_prev = new MySqlCommand(to_prev, cong);
            MySqlDataReader pv = v_prev.ExecuteReader();
            {
                while (pv.Read())
                {
                    uints[0] = Convert.ToUInt32(pv[2]);
                    uints[1] = Convert.ToUInt32(pv[5]);
                }
            }
            try
            {
                cong.Close();
            }
            catch { }

            return uints;
        }

        // запись старых значений, мол, за ту смену
        private void Ty(uint tyv1, uint tyv2)
        {
            try
            {
                conu.Open();
                string change_hour = $"UPDATE squagy SET old_value={tyv1} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, conu);
                chhr.ExecuteNonQuery();
                conu.Close();
            }
            catch
            {
                string change_hour = $"UPDATE squagy SET old_value={tyv1} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, conu);
                chhr.ExecuteNonQuery();
            }

            try
            {
                conu.Open();
                string change_hour = $"UPDATE squagy SET old_value2={tyv2} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, conu);
                chhr.ExecuteNonQuery();
                conu.Close();
            }
            catch
            {
                string change_hour = $"UPDATE squagy SET old_value2={tyv2} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, conu);
                chhr.ExecuteNonQuery();
            }
        }

        // метод чтения из БД и разница с текущим
        private uint Razn(int index_sql, uint V_cur)
        {
            uint V_val = 0;
            string hist_v = $"SELECT * FROM squagy";

            try
            {
                cony.Open();
            }
            catch { }
            MySqlCommand history_v = new MySqlCommand(hist_v, cony);
            MySqlDataReader hv = history_v.ExecuteReader();
            {
                while (hv.Read())
                {
                    V_val = Convert.ToUInt32(hv[index_sql]);
                }
            }
            try
            {
                cony.Close();
            }
            catch { }

            V_val = V_cur - V_val;
            return V_val;
        }

        // подготовка накопленных объемов перед выводом
        private void V_now(uint v1, uint v2)
        {
            try
            {
                cona.Open();
                string change_hour = $"UPDATE squagy SET start_value={v1} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, cona);
                chhr.ExecuteNonQuery();
                cona.Close();
            }
            catch
            {
                string change_hour = $"UPDATE squagy SET start_value={v1} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, cona);
                chhr.ExecuteNonQuery();
            }

            try
            {
                cona.Open();
                string change_hour = $"UPDATE squagy SET start_value2={v2} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, cona);
                chhr.ExecuteNonQuery();
                cona.Close();
            }
            catch
            {
                string change_hour = $"UPDATE squagy SET start_value2={v2} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, cona);
                chhr.ExecuteNonQuery();
            }
        }

        // получение времени
        private int Current_hour()
        {
            // игра со временем
            string time_now = Convert.ToString(DateTime.Now);    // текущее время ПК
            char[] tn_chars = time_now.ToCharArray();                // разбив значения на символы
            char[] chars_hourA = new char[1] { tn_chars[11] };
            char[] chars_hourB = new char[2] { tn_chars[11], tn_chars[12] };
            char[] chars_hourC = new char[1] { tn_chars[10] };
            char[] chars_hourD = new char[2] { tn_chars[10], tn_chars[11] };
            string current_hour = "33";
            char dot = Convert.ToChar(".");

            if ((tn_chars.Length == 18) && (tn_chars[2] == dot))
            { current_hour = new string(chars_hourA); }        // если час односимвольный
            else if ((tn_chars.Length == 19) && (tn_chars[2] == dot))
            { current_hour = new string(chars_hourB); }
            else if ((tn_chars.Length == 17) && (tn_chars[1] == dot))
            { current_hour = new string(chars_hourC); }
            else if ((tn_chars.Length == 18) && (tn_chars[1] == dot))
            { current_hour = new string(chars_hourD); }

            int hour = Convert.ToInt32(current_hour);           // результат текущего часа

            return hour;
        }

        // получение минут
        private int Current_minute()
        {
            // игра со временем
            string time_now = Convert.ToString(DateTime.Now);    // текущее время ПК
            char[] tn_chars = time_now.ToCharArray();                // разбив значения на символы
            char[] chars_mA = new char[1] { tn_chars[11] };
            char[] chars_mB = new char[2] { tn_chars[14], tn_chars[15] };
            char[] chars_mC = new char[1] { tn_chars[10] };
            char[] chars_mD = new char[2] { tn_chars[13], tn_chars[14] };
            string current_m = "61";
            char dot = Convert.ToChar(".");

            if ((tn_chars.Length == 18) && (tn_chars[2] == dot))
            { current_m = new string(chars_mA); }        // если час односимвольный
            else if ((tn_chars.Length == 19) && (tn_chars[2] == dot))
            { current_m = new string(chars_mB); }
            else if ((tn_chars.Length == 17) && (tn_chars[1] == dot))
            { current_m = new string(chars_mC); }
            else if ((tn_chars.Length == 18) && (tn_chars[1] == dot))
            { current_m = new string(chars_mD); }

            int minut = Convert.ToInt32(current_m);           // результат текущих минут
            return minut;
        }

        // истина по достижении определенного времени
        private bool Budilo(int hr, int mn)
        {
            if ((Current_hour() == hr) && (Current_minute() == mn))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // команда в БД на инициализацию нулевыми значениями
        private void Zero()
        {
            try
            {
                coni.Open();
                string insertation_0 = $"INSERT INTO squagy (Id, start_value, old_value, vrem, start_value2, old_value2) VALUES ('{0}','{0}','{0}','{0}','{0}','{0}')";
                MySqlCommand zero = new MySqlCommand(insertation_0, coni);
                zero.ExecuteNonQuery();
                coni.Close();
            }
            catch 
            {
                string insertation_0 = $"INSERT INTO squagy (Id, start_value, old_value, vrem, start_value2, old_value2) VALUES ('{0}','{0}','{0}','{0}','{0}','{0}')";
                MySqlCommand zero = new MySqlCommand(insertation_0, coni);
                zero.ExecuteNonQuery();
            }
        }
    }
}
       