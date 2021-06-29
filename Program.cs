using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using Terminal.Gui;

namespace TCPGUI
{

    class Program
    {
		StatusItem si;
		ListView lv;
		Thread ct;
		int count = 0;
		bool running = true;
		bool connected = false;

		ConcurrentQueue<string> statusMessages = new ConcurrentQueue<string>();
		NetworkStream ns;
		ConcurrentQueue<byte> rxdBytes = new ConcurrentQueue<byte>();

		List<string> messageList = new List<string>();

		private void ClientThread(string hostname, int port) {
			byte[] buf = new byte[1024];
			int bRead;
			TcpClient tc = new TcpClient();

			while (running) {
				try {
					statusMessages.Enqueue(String.Format("Connecting to {0}:{1}...", hostname, port));
					tc.Connect(hostname, port);
					ns = tc.GetStream();
					connected = true;
					statusMessages.Enqueue(String.Format("CONNECTED to {0}:{1}.", hostname, port));

					while (tc.Connected) {
						if (ns.DataAvailable) {
							bRead = ns.Read(buf,0,buf.Length);
							for (int i = 0; i < bRead; i++) {
								rxdBytes.Enqueue(buf[i]);
							}
						}
					}
				} catch (Exception e) {
					connected = false;
					statusMessages.Enqueue(String.Format("DISCONNECTED. {0}.", e.Message));
					statusMessages.Enqueue("Retrying in 10 seconds...");
					Thread.Sleep(10000);
				}
			}
		}

		private void CountThread()
		{
			messageList.Add("GUI handler thread initialised.");
			string msg = "";

			while (running) 
			{
				//si.Title = String.Format("Elapsed time: {0} seconds.", count);
				if (!statusMessages.IsEmpty) {
					while (!statusMessages.IsEmpty) {
						if (statusMessages.TryDequeue(out msg))
						messageList.Add(msg);
						lv.MoveEnd();
					}
					lv.MoveUp();
					lv.MoveUp();
					lv.MoveUp();
					Application.Refresh();
				}
				Thread.Sleep(100);
			}
		}	

		private void run(string hostname, int port) 
		{
			Application.Init();

			Toplevel top = Application.Top;

			Window win = new Window ("QUTMS Telemetry Example"){
				X = 0,
				Y = 1,
				Width = Dim.Fill(),
				Height = Dim.Fill()-8
			};
			Window messages = new Window ("Messages"){
				X = 0,
				Y = 22,
				Width = Dim.Fill(),
				Height = Dim.Fill()-1
			};

			MenuBar menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Exit", "", () => { running = false; top.Running = false; })
				})
			});

			si = new StatusItem(Key.Null, "Init...", null);

			StatusBar status = new StatusBar(new StatusItem[] {
				si
			});

			lv = new ListView(messageList) {
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
				Height = Dim.Fill()
			};
			messages.Add(lv);

			top.Add (menu, win, messages, status);

			ct = new Thread(CountThread);
			ct.Start();

			Thread clientThread = new Thread(() => ClientThread(hostname, port));
			clientThread.Start();

			si.Title = "DISCONNECTED";
			Application.Run();
		}

        static void Main(string[] args)
        {
			Program tpo = new Program();
			tpo.run(args[0], int.Parse(args[1]));
        }
    }
}
