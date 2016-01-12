using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Collections;

namespace SerialTools {
	public partial class Form1 : Form {
		public Form1() {
			InitializeComponent();
			UIinitListView();

			cd = new ComDeal(this);
		}


		delegate void AddItemCallback(string text);
		delegate void StatusCallback(bool link);
		delegate void UpdataListCallback(int addr, String value);

		public void Print(string text) {
			if (this.textBox1.InvokeRequired) {
				AddItemCallback d = new AddItemCallback(Print);
				this.Invoke(d, new object[] { text });
			} else {
				this.textBox1.Text += DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " : " + text + "\r\n";
				this.textBox1.Select(this.textBox1.Text.Length, 0);
				this.textBox1.ScrollToCaret();
			}
		}
		public void ShowRecCnt(string text) {
			if (this.textBox1.InvokeRequired) {
				AddItemCallback d = new AddItemCallback(ShowRecCnt);
				this.Invoke(d, new object[] { text });
			} else {
				this.label1.Text = text;
			}
		}
		public void SerialPortStatus(bool linked) {
			if (this.button2.InvokeRequired) {
				StatusCallback d = new StatusCallback(SerialPortStatus);
				this.Invoke(d, new object[] { linked });
			} else {
				if (linked) {
					this.button2.Text = "断开";
					this.button2.ForeColor = Color.Blue;
				} else {
					this.button2.Text = "连接";
					this.button2.ForeColor = Color.Black;
				}
			}
		}
		public void SetAutoSync(bool linked) {
			if (this.button1.InvokeRequired) {
				StatusCallback d = new StatusCallback(SetAutoSync);
				this.Invoke(d, new object[] { linked });
			} else {
				if (linked) {
					this.button1.ForeColor = Color.Blue;
				} else {
					this.button1.ForeColor = Color.Black;
				}
			}
		}
		public void UpdateListView(int addr, String value) {
			try {
				if (this.listView1.InvokeRequired) {
					UpdataListCallback d = new UpdataListCallback(UpdateListView);
					this.Invoke(d, new object[] { addr, value });
				} else {
					this.listView1.Items[addr].SubItems[0].Text = value;
					//Print("Update UI   Addr = " + addr + " , value = " + value);
				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			}
		}





		ComDeal cd = null;







		//点击选择串口
		private void comboBox1_Click(object sender, EventArgs e) {//点击 ----- 枚举串口
			string[] str = SerialPort.GetPortNames();

			if (str == null) {
				Print("没有串口");
			}

			this.comboBox1.Items.Clear();
			foreach (String s in str) {
				this.comboBox1.Items.Add(s);
			}
		}


		//连接串口
		private void button2_Click(object sender, EventArgs e) {
			bool result = cd.LinkSerialPort((String)comboBox1.SelectedItem);

			SerialPortStatus(result);
		}







		//测试发送Get
		private void button1_Click(object sender, EventArgs e) {
			SetAutoSync(cd.AutoSyncChange());
		}


		//初始化UI
		void UIinitListView() {
			this.listView1.BeginUpdate();   //数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度 

			this.listView1.Columns.Add("Value", 50, HorizontalAlignment.Center); //一步添加  
			this.listView1.Columns.Add("Addr", 50, HorizontalAlignment.Center); //一步添加  


			for (int i = 0; i < 256; i++) {  //添加10行数据 
				ListViewItem lvi = new ListViewItem();
				lvi.ImageIndex = i;     //通过与imageList绑定，显示imageList中第i项图标 
				lvi.Text = "0x" + ((byte)i).ToString("X2");
				lvi.SubItems.Add("" + i);
				this.listView1.Items.Add(lvi);
			}
			this.listView1.EndUpdate();  //结束数据处理，UI界面一次性绘制。  
		}


		//修改变量
		private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e) {//改变变量
			if (e.Label == null) {//没有改变
				return;
			} else {//e.Label:修改后的String
				String Changed = e.Label.Trim();
				if (e.Label.Equals(((ListView)sender).Items[e.Item].SubItems[0].Text)) {
					return;
				}
				char[] cs = Changed.ToArray();

				if (cs.Length <= 2 && cs.Length >= 5) {
					Print("不符合格式要求,0x00--0xff--0xFF");
					return;
				} else {
					if (cs[0] == '0' && cs[1] == 'x') {
						Print("修改地址为" + ((ListView)sender).Items[e.Item].SubItems[1].Text +
							"变量的值为:" + Changed);

						cd.ChangeDataByUser(((ListView)sender).Items[e.Item].SubItems[1].Text, Changed);

					} else {
						Print("不符合格式要求,0x00--0xff--0xFF");
						return;
					}
				}

			}
		}



		//测试更新列表
		private void button3_Click(object sender, EventArgs e) {
			for (int i = 0; i < 255; i++) {
				UpdateListView(i, i + "");
			}
		}











	}
}
