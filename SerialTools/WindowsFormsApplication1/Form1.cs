using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO.Ports;

namespace WindowsFormsApplication1 {

	public partial class Form1 : Form {
		public Form1() {
			InitializeComponent();

			NetInit();
			Socketstatus(false);
			SerialPortStatus(false);
		}



		//此委托允许异步的调用为Listbox添加Item  
		delegate void AddItemCallback(string text);
		delegate void StatusCallback(bool link);

		private void Print(string text) {
			if (this.textBox2.InvokeRequired) {
				AddItemCallback d = new AddItemCallback(Print);
				this.Invoke(d, new object[] { text });
			} else {
				this.textBox2.Text += DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " " + text + "\r\n";
			}
		}
		private void Clear(string text) {
			if (this.textBox2.InvokeRequired) {
				AddItemCallback d = new AddItemCallback(Clear);
				this.Invoke(d, new object[] { text });
			} else {
				this.textBox2.Text = "";
			}
		}
		private void Socketstatus(bool linked) {
			if (this.textBox2.InvokeRequired) {
				StatusCallback d = new StatusCallback(Socketstatus);
				this.Invoke(d, new object[] { linked });
			} else {
				if (linked) {
					this.label3.Text = "已连接";
					this.label3.ForeColor = Color.Blue;
					this.button1.Text = "断开";
				} else {
					this.label3.Text = "未连接";
					this.label3.ForeColor = Color.Red;
					this.button1.Text = "连接";
				}
			}
		}
		private void SerialPortStatus(bool linked) {
			if (this.textBox2.InvokeRequired) {
				StatusCallback d = new StatusCallback(SerialPortStatus);
				this.Invoke(d, new object[] { linked });
			} else {
				if (linked) {
					this.label4.Text = "已连接";
					this.label4.ForeColor = Color.Blue;
					this.button2.Text = "断开";
				} else {
					this.label4.Text = "未连接";
					this.label4.ForeColor = Color.Red;
					this.button2.Text = "连接";
				}
			}
		}





		/************************************ Net ************************************/





		Socket s;
		int port = 9876;

		private void button1_Click(object sender, EventArgs e) {//连接传感器


			if (s != null) {
				if (s.Connected) {//已连接
					s.Close();//断开
				}
				s = null;
				Socketstatus(false);
			} else {


				IPEndPoint ipe = null;

				try {

					if (this.textBox1.Text == "") {
						Print("空IP");
						return;
					}

					IPAddress ip = IPAddress.Parse(this.textBox1.Text);
					ipe = new IPEndPoint(ip, port);//把ip和端口转化为IPEndpoint实例

				} catch (Exception e1) {
					Print(e1.ToString());
					Print("ip地址错误");
					return;
				}


				try {

					/**/
					///创建socket并连接到服务器
					s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建Socket
					s.Connect(ipe);//连接到服务器



					Print("已连接传感器");


				} catch (Exception e1) {
					Print(e1.ToString());
					Print("连接传感器失败");
					return;
				}

			}


		}


		/// <summary>
		/// 开线程
		/// </summary>
		void NetInit() {
			thread = new Thread(new ThreadStart(run));
			thread.IsBackground = true;
			thread.Start();
		}

		Thread thread;


		void run() {

			for (; ; ) {

				try {

					for (; ; ) {//等待
						Thread.Sleep(50);
						if (s != null) {
							if (s.Connected) {
								Socketstatus(true);
								break;
							}
						}
					}


					for (; ; ) {

						string recvStr = "";
						byte[] recvBytes = new byte[1024];
						int bytes;
						bytes = s.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
						if (bytes == 0) {//连接已断开
							break;
						}
						recvStr += Encoding.ASCII.GetString(recvBytes, 0, bytes);
						Print("收到传感器信息：" + recvStr);//显示服务器返回信息

						if (sp != null) {
							if (sp.IsOpen == true) {
								Print("已转发到串口");

								String[] ss = recvStr.Split(' ');

								String[] sss = new String[2];
								int j = 0;
								for (int i = 0; i < ss.Length; i++) {
									if (ss[i] == null || ss[i] == "" || ss[i] == " ") {
									} else {
										if (j < 2) {
											sss[j] = ss[i];
										}
										j++;
									}
								}

								if (j == 2) {
									float fx = float.Parse(sss[0]);
									float fy = float.Parse(sss[1]);

									byte[] bs = DataConverse(0x10f6, (UInt16)(fx * 10), (UInt16)(fy * 10));
									sp.Write(bs, 0, bs.Length);

								} else {
									Print(j + "");
									Print("长度不正确");
								}

							}
						}

					}

				} catch (Exception e1) {
					Print(e1.ToString());
				}

				Print("已断开传感器");
				///一定记着用完socket后要关闭
				if (s != null) {
					s.Close();
				}
				s = null;
				Socketstatus(false);

			}


		}












		/************************************ SerialPort ************************************/



		SerialPort sp = null;//


		private void button2_Click(object sender, EventArgs e) {//连接串口


			if (sp == null) {//Link

				try {

					if (this.comboBox1.SelectedItem == null) {
						Print("请选择串口");
						return;
					} else {
						String ComName = this.comboBox1.SelectedItem.ToString();
						Print("开始连接 " + ComName);
						sp = new SerialPort();
						sp.PortName = ComName;
						sp.BaudRate = 9600;       //波特率  
						//sp.DataBits = 8;       //数据位  
						//sp.StopBits = StopBits.One;  //停止位  
						//sp.Parity = Parity.None;    //校验位		
						sp.DataBits = 7;       //数据位  
						sp.StopBits = StopBits.One;  //停止位  
						sp.Parity = Parity.Even;    //校验位		

						if (sp.IsOpen == true) {//如果打开状态，则先关闭一下  
							sp.Close();
						}
						sp.Open();     //打开串口  

					}


					SerialPortStatus(true);
					Print("已连接串口");

				} catch (Exception ee) {
					Print(ee.ToString());
					Print("连接串口失败");
				}

			} else {//DisLink

				if (sp.IsOpen == true) {//如果打开状态，则先关闭一下  
					sp.Close();
				}
				sp = null;

				SerialPortStatus(false);
				Print("已断开串口连接");
			}


		}

		private void comboBox1_Click(object sender, EventArgs e) {//点击 ----- 选择串口
			string[] str = SerialPort.GetPortNames();

			if (str == null) {
				Print("没有串口");
			}

			this.comboBox1.Items.Clear();
			foreach (String s in str) {
				this.comboBox1.Items.Add(s);
			}
		}

		private void button3_Click(object sender, EventArgs e) {
			Clear("");
		}

		private void button4_Click(object sender, EventArgs e) {

			if (sp != null) {
				if (sp.IsOpen == true) {

					try {
						UInt16 testX = (UInt16)int.Parse(textBox3.Text);
						UInt16 testY = (UInt16)int.Parse(textBox4.Text);
						byte[] bs = DataConverse(0x10F6, testX, testY);
						sp.Write(bs, 0, bs.Length);
						Print("Test Finish");
					} catch (Exception ee) {
						Print(ee.ToString());
						Print("请输出数字");
					}

				} else {
					Print("IsClose");
				}
			} else {
				Print("NUll");
			}

		}

		byte[] DataConverse(UInt16 addr, UInt16 x, UInt16 y) {

			byte[] bs = new byte[19];

			bs[0] = 0x02;
			bs[1] = 0x31;



			UInt16 temp = addr;
			bs[2] = Converse2Ascii((UInt16)(temp >> 12));//Address Ascii
			bs[3] = Converse2Ascii((UInt16)(temp >> 8));
			bs[4] = Converse2Ascii((UInt16)(temp >> 4));
			bs[5] = Converse2Ascii((UInt16)(temp));

			bs[6] = 0x30;//Length Acsii
			bs[7] = 0x34;

			temp = x;
			bs[8] = Converse2Ascii((UInt16)(temp >> 4));//X Ascii
			bs[9] = Converse2Ascii((UInt16)(temp));
			bs[10] = Converse2Ascii((UInt16)(temp >> 12));
			bs[11] = Converse2Ascii((UInt16)(temp >> 8));

			temp = y;
			bs[12] = Converse2Ascii((UInt16)(temp >> 4));//Y Ascii
			bs[13] = Converse2Ascii((UInt16)(temp));
			bs[14] = Converse2Ascii((UInt16)(temp >> 12));
			bs[15] = Converse2Ascii((UInt16)(temp >> 8));

			bs[16] = 0x03;//ETX

			UInt16 sum = 0;
			for (int i = 1; i < 17; i++) {
				sum += bs[i];
			}

			bs[17] = Converse2Ascii((UInt16)(sum >> 4));//Sum
			bs[18] = Converse2Ascii((UInt16)(sum));


			return bs;
		}


		byte Converse2Ascii(UInt16 b) {
			UInt16 bb = (UInt16)(b & 0x0f);
			if (bb > 0xf) {
				return (byte)'?';
			} else {
				if (bb < 0xa) {
					return (byte)(bb + '0');
				} else {
					return (byte)('A' + bb - 0xA);
				}

			}
		}














	}
}
