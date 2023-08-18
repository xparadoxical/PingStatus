using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;

namespace PingStatus
{
    public partial class Form1 : Form
    {
        private static Thread ping;
        private static NotifyIcon icon;
        private ContextMenu menu;
        private MenuItem closemenu, ipmenu;
        static bool loop = true;

        public Form1()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            InitializeComponent();

            menu = new ContextMenu();

            closemenu = new MenuItem("Zakończ", OnExit);
            ipmenu = new MenuItem("Zmień adres...", OnChangeIP);

            menu.MenuItems.Add(0, closemenu);
            menu.MenuItems.Add(1, ipmenu);

            icon = new NotifyIcon();
            icon.Text = "Ładowanie...";
            icon.Icon = new Icon(SystemIcons.Question, 16, 16);

            icon.ContextMenu = menu;
            icon.Visible = true;

            icon.BalloonTipClicked += Icon_BalloonTipClicked;
            
            StartPingTask();
        }

        private static void StartPingTask()
        {
            ping = new Thread(new ThreadStart(UpdatePing));
            ping.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            icon.Visible = false;
            loop = false;
            ping.Abort();
            Thread.Sleep(1000);

            avg.Clear();
            icon.Dispose();
            Application.Exit();
        }

        private void OnChangeIP(object sender, EventArgs e)
        {
            textBox1.Text = ip;
            Show();
        }

        static string ip = "www.google.com";
        private void button1_Click(object sender, EventArgs e)
        {
            Hide();
            if (!string.IsNullOrWhiteSpace(textBox1.Text) || (textBox1.Text != ip && ping.IsAlive))
            {
                msgShown = false;
                if (t.IsAlive)
                    t.Abort();
                avg.Clear();
                i = 0;
                ip = textBox1.Text;
                if (!ping.IsAlive)
                    StartPingTask();
            }
            else
                textBox1.Text = ip;
        }

        static List<long> avg = new List<long>(120);
        static int i;
        private static void UpdatePing()
        {
            avg.Clear();
            i = 0;
            long ms = 0;
            icon.Text = "Ping (" + (ip.Length > 24 ? "zbyt długi adres" : ip) + "): -" + "\n"
                                + "Średni ping: -";

            using (Ping p = new Ping())
            {
                while (loop)
                {
                    if (ping.ThreadState == ThreadState.Running)
                    {
                        try
                        {
                            ms = p.Send(ip).RoundtripTime;
                            if (ms == 0)
                                icon.Text = "Ping (" + (ip.Length > 24 ? "zbyt długi adres" : ip) + "): -" + "\n"
                                    + "Średni ping: -";
                            else
                                msgShown = false;

                            if (i >= avg.Capacity - 1)
                                i = 0;
                            if (avg.Count == avg.Capacity)
                                avg[i] = ms;
                            else
                                avg.Add(ms);
                            i++;

                            icon.Text = "Ping (" + (ip.Length > 24 ? "zbyt długi adres" : ip) + "): " + ms.ToString() + "ms" + "\n"
                                + "Średni ping: " + (avg.Sum() / avg.Count) + "ms";
                            if (ms >= 0 && ms < 30)
                                icon.Icon = new Icon(SystemIcons.Information, 16, 16);
                            else if (ms >= 30 && ms < 120)
                                icon.Icon = new Icon(SystemIcons.Warning, 16, 16);
                            else if (ms >= 120)
                                icon.Icon = new Icon(SystemIcons.Error, 16, 16);
                            Thread.Sleep(1000);
                        }
                        catch (PingException)
                        {
                            icon.Text = "Ping (sprawdź adres): -" + "\n" + "Średni ping: -";
                            if (!msgShown)
                                ErrorMsg();
                            break;
                        }
                    }
                    else
                        break;
                }
            }
        }

        static Thread t = new Thread(new ThreadStart(CheckInternet));
        static bool msgShown = false;
        private static void ErrorMsg()
        {
            msgShown = true;
            icon.Icon = new Icon(SystemIcons.Question, 16, 16);
            icon.BalloonTipIcon = ToolTipIcon.Error;
            icon.BalloonTipTitle = "Wystąpił błąd";
            icon.BalloonTipText = "Wystąpił błąd podczas próby wykonania pomiaru. Sprawdź, czy masz połączenie z internetem, czy podany adres został wpisany poprawnie i czy nie zawiera on niedozwolonych znaków. Kliknij tutaj, aby otworzyć okienko wyboru nowego adresu.";
            icon.ShowBalloonTip(10000);
            if (!t.IsAlive)
                t.Start();
        }

        private static void CheckInternet()
        {
            while (true)
            {
                try
                {
                    new Ping().Send("www.google.com");
                    break;
                }
                catch
                {
                    
                }
            }

            icon.BalloonTipIcon = ToolTipIcon.Info;
            icon.BalloonTipTitle = "Połączono";
            icon.BalloonTipText = "Odzyskano połaczenie z internetem. Wykonywanie pomiarów zostało wznowione.";
            icon.ShowBalloonTip(5000);
            StartPingTask();
        }

        private void Icon_BalloonTipClicked(object sender, EventArgs e)
        {
            textBox1.Text = ip;
            Show();
        }
    }
}