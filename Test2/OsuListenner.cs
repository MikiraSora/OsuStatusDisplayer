using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Test2
{
    class OsuListenner
    {
        TcpClient client = null;
        Thread runThread = null;

        double currentAcc, currentHp;
        int beatmapId, beatmapSetId,mods=0;
        string title = "", diffName = "";

        public OsuListenner()
        {
            runThread = new Thread(threadRun);
        }

        public void Connect(bool isReconnect=false,int port=7582)
        {
            client = new TcpClient();
            while (true)
            {
                try
                {
                    client.Connect(IPAddress.Parse("127.0.0.1"), port);
                    onConnect(isReconnect);
                    break;
                }
                catch
                {
                    Thread.Sleep(300);
                }
            }
        }

        public void Start()
        {
            Connect();
            runThread.Start();
        }

        public void Close()
        {
            client.Close();
        }

        private void threadRun(object state)
        {
            byte[] buffer = new byte[100];
            int size = -1;
            string message;

            while (true)
            {
                if(!client.Connected)
                {
                    onConnectLost();
                    Connect(true);
                }
                try
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    size = client.GetStream().Read(buffer, 0, 100);
                    message = Encoding.Default.GetString(buffer).Trim();
                    ProcessMessage(message);
                }
                catch(Exception e){
                    onConnectLost();
                }
            }

        }

        char[] splitChars = {' '};

        private void ProcessMessage(string message)
        {
            string[] parts = message.Split(splitChars);

            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim();

            double value;
            string ct = "";
            int id;

            foreach(string part in parts)
            {
                switch (part[0])
                {
                    case 'a'://acc
                        if(part.Length > 1 && Double.TryParse(part.Substring(1),out value))
                        {
                            if (/*Math.Abs(currentAcc - value) > 0.01&&*/(value<=100))
                            {
                                currentAcc = value;
                                onUpdateACC(currentAcc);
                            }
                        }
                        break;

                    case 'h'://hp
                        if (part.Length>1&&Double.TryParse(part.Substring(1), out value))
                        {
                            if (/*Math.Abs(currentHp - value) > 0.01 && */(value <= 200))
                            {
                                currentHp = value;
                                onUpdateHP(currentHp);
                            }
                        }
                        break;

                    case 'b'://beatmapId
                        if (part.Length > 1 && Int32.TryParse(part.Substring(1), out id))
                        {
                            if (beatmapId != id && (id>0))
                            {
                                beatmapId = id;
                                onChangeBeatmapId(id);
                                //暂时没事件
                            }
                        }
                        break;

                    case 's'://beatmapSetId
                        if (part.Length > 1 && Int32.TryParse(part.Substring(1), out id))
                        {
                            if (beatmapSetId != id && (id > 0))
                            {
                                beatmapSetId = id;
                                onChangeBeatmapSetId(id);
                                //暂时没事件
                            }
                        }
                        break;

                    case 't'://title
                        if (part.Length > 1)
                        {
                            ct = part.Substring(1).Trim();
                            if (ct!=title)
                            {
                                title = ct;
                                onUpdateTitle(title);
                            }
                        }
                        break;

                    case 'd'://diff
                        if (part.Length > 1)
                        {
                            ct = part.Substring(1).Trim();
                            if (ct != diffName)
                            {
                                diffName = ct;
                                onUpdateDiff(diffName);
                            }
                        }
                        break;

                    case 'c': //Combo
                        if (part.Length > 1 && Int32.TryParse(part.Substring(1), out id))
                        {
                            onUpdateCombo(id);
                        }
                        break;

                    case 'm': //Mods
                        if (part.Length > 1 && Int32.TryParse(part.Substring(1), out id))
                        {
                            if (mods != id)
                            {
                                onChangeMods(id);
                                mods = id;
                            }
                        }
                        break;

                    default:
                        Console.WriteLine("unknown part:{0}",part);
                        break;
                }
            }

            onUpdateFinish();
        }

        #region TriggerEvent
        public delegate void OnConnectEvt(bool isReconnect);
        public event OnConnectEvt onConnect;
        public delegate void OnConnectLostEvt();
        public event OnConnectLostEvt onConnectLost;
        public delegate void OnUpdateHPEvt(double hp);
        public event OnUpdateHPEvt onUpdateHP;
        public delegate void OnUpdateACCEvt(double acc);
        public event OnUpdateACCEvt onUpdateACC;
        public delegate void OnPlayerFailedEvt();
        public event OnPlayerFailedEvt onPlayerFailed;
        public delegate void OnUpdateTitleEvt(string title);
        public event OnUpdateTitleEvt onUpdateTitle;
        public delegate void OnUpdateDiffEvt(string diffName);
        public event OnUpdateDiffEvt onUpdateDiff;
        public delegate void OnPlayerPauseEvt();
        public event OnPlayerPauseEvt onPlayerPause;
        public delegate void OnChangeBeatmapId(int id);
        public event OnChangeBeatmapId onChangeBeatmapId;
        public delegate void OnChangeBeatmapSetId(int setId);
        public event OnChangeBeatmapSetId onChangeBeatmapSetId;
        public delegate void OnUpdateCombo(int combo);
        public event OnUpdateCombo onUpdateCombo;
        public delegate void OnChangeMods(int mods);
        public event OnChangeMods onChangeMods;
        public delegate void OnUpdateFinish();
        public event OnUpdateFinish onUpdateFinish;
        #endregion
    }
}
