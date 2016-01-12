using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Timers;

namespace SerialTools {

	class ComDeal {

		Form1 f = null;

		SerialPort sp = null;
		bool AutoSync = false;
		String[] ListViewBuf = null;//有不一样才更新 防止一直更新
		List<ComRequest> RequestList = null;
		Timer timer = null;

		void InitTimer() {
			timer = new Timer();
			timer.Elapsed += new ElapsedEventHandler(Run);
			timer.Interval = 1000;
			timer.Start();
			timer.Enabled = true;
		}

		void Run(object source, ElapsedEventArgs e) {
			f.ShowRecCnt(DataRecCnt + "times/s");
			DataRecCnt = 0;
		}

		//构造器
		public ComDeal(Form1 f) {
			this.f = f;
			ListViewBuf = new String[256];
			for (int i = 0; i < 256; i++) {  //添加10行数据 
				ListViewBuf[i] = "0x" + ((byte)i).ToString("X2");
			}
			InitTimer();
		}



		//比较是否和UI的不一样 不一样则更新
		void CompareAndUpdateListView(int addr, String Value) {
			if (ListViewBuf[addr].Equals(Value)) {

			} else {
				ListViewBuf[addr] = Value;
				f.UpdateListView(addr, Value);
			}
		}


		//用户修改数据
		public void ChangeDataByUser(String addr, String Value) {

			int[] bs = { Convert.ToByte(Value, 16) };
			int addrint = Convert.ToByte(addr, 10);
			ListViewBuf[addrint] = Value;

			RequestList.Add(new ComRequest(0, addrint, bs));
			if (AutoSync == false) {
				AddAllRequestAndSend();
			}
		}


		//添加获取全部的数据的请求
		void AddAllRequestAndSend() {
			RequestList.Add(new ComRequest(0, 0, 50));
			RequestList.Add(new ComRequest(0, 50, 50));
			RequestList.Add(new ComRequest(0, 100, 50));
			RequestList.Add(new ComRequest(0, 150, 50));
			RequestList.Add(new ComRequest(0, 200, 56));

			if (RequestList[0].status != ComRequest.STATUS_STANDBY) {
				RequestList[0].ReStart();
			}
			String s = RequestList[0].SendRequest(sp);
			if (s != null) {
				f.Print(s);
			}
		}

		//数据接收数量
		int DataRecCnt;

		// 串口数据接收
		private void SerialDataRecieve(object sender, SerialDataReceivedEventArgs e) {
			int n = sp.BytesToRead;
			byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据        
			sp.Read(buf, 0, n);//读取缓冲数据
			String s = "";

			//for (int i = 0; i < n; i++) {
			//    s += buf[i].ToString("X2") + "\t";
			//}
			//f.Print(s);
			//f.Print("n = " + n);

			s = RequestList[0].DealResponse(buf);

			if (s == ComRequest.RESPONSE_NOT_FINISHED) {
			} else {
				if (s == ComRequest.RESPONSE_SUCCESS) {//Success
					if (RequestList[0].isGet) {
						for (int i = 0; i < RequestList[0].length; i++) {//UpdateUI
							CompareAndUpdateListView(RequestList[0].addr + i, "0x" + RequestList[0].data[i].ToString("X2"));
						}

					}
					RequestList.RemoveAt(0);
				} else if (s == ComRequest.RESPONSE_FAIL) {//Fail
					RequestList[0].ReStart();
				}

				if (RequestList.Count > 0) {//有请求
					s = RequestList[0].SendRequest(sp);
					if (s != null) {
						f.Print(s);
					}
				} else { //无请求
					if (AutoSync) {
						AddAllRequestAndSend();
					}
				}

			}
			//f.Print(s);
			DataRecCnt++;
		}


		//自动同步
		public bool AutoSyncChange() {
			AutoSync = !AutoSync;
			if (AutoSync) {
				if (RequestList == null) {
					RequestList = new List<ComRequest>();
				}

				AddAllRequestAndSend();
				RequestList[0].ReStart();
			} else {
			}
			return AutoSync;
		}

		//连接&断开 串口
		public bool LinkSerialPort(String Com) {

			if (sp == null) {//Link

				try {

					if (Com == null) {
						f.Print("请选择串口");
						return false;
					}


					f.Print("开始连接 " + Com);
					sp = new SerialPort();
					sp.PortName = Com;
					sp.BaudRate = 115200;       //波特率  
					sp.DataBits = 8;       //数据位  
					sp.StopBits = StopBits.One;  //停止位  
					sp.Parity = Parity.None;    //校验位		
					sp.DataReceived += SerialDataRecieve;

					if (sp.IsOpen == true) {//如果打开状态，则先关闭一下  
						sp.Close();
					}

					sp.Open();     //打开串口  

					AutoSync = false;
					f.Print("已连接串口");
					return true;

				} catch (Exception ee) {
					sp = null;
					f.Print(ee.ToString());
					f.Print("连接串口失败");
					return false;
				}

			} else {//DisLink

				if (sp.IsOpen == true) {//如果打开状态，则先关闭一下  
					sp.Close();
				}
				sp = null;

				f.Print("已断开串口连接");
				return false;
			}

		}



	}




}
