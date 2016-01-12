using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace SerialTools {
	/// <summary>
	/// 串口请求
	/// </summary>
	class ComRequest {
		public bool isGet;
		public int page;
		public int addr;
		public int length;
		public int[] data;
		public int status;

		public const int STATUS_STANDBY = 0;
		public const int STATUS_SENDED = 1;
		public const int STATUS_FINISH_OK = 2;
		public const int STATUS_FINISH_ERR = 3;

		/// <summary>
		/// Create Get Request
		/// </summary>
		/// <param name="page"></param>
		/// <param name="addr"></param>
		/// <param name="length"></param>
		public ComRequest(int page, int addr, int length) {
			this.isGet = true;
			this.page = page;
			this.addr = addr;
			this.length = length;
			this.status = STATUS_STANDBY;
		}

		/// <summary>
		/// Create Set Request
		/// </summary>
		/// <param name="page"></param>
		/// <param name="addr"></param>
		/// <param name="data"></param>
		public ComRequest(int page, int addr, int[] data) {
			this.isGet = false;
			this.page = page;
			this.addr = addr;
			this.data = data;
			this.status = STATUS_STANDBY;
		}

		/// <summary>
		/// 重新配置 以 重新发送
		/// </summary>
		public void ReStart() {
			this.RecBuf = null;
			this.status = STATUS_STANDBY;
		}


		const byte CMD_GET = 0x7A;
		const byte CMD_SET = 0x8B;
		const byte CMD_END = 0xA6;
		const byte CMD_ERR = 0xE1;
		const byte CMD_OK = 0x66;


		/// <summary>
		/// 发送Get请求
		/// </summary>
		/// <param name="page"></param>
		/// <param name="addr"></param>
		/// <param name="length"></param>
		/// <param name="ssp"></param>
		private void ComGet(int page, int addr, int length, SerialPort ssp) {
			byte[] bb = new byte[6];
			bb[0] = CMD_GET;
			bb[1] = (byte)page;
			bb[2] = (byte)addr;
			bb[3] = (byte)length;
			bb[4] = (byte)(CMD_GET + page + addr + length);
			bb[5] = CMD_END;
			ssp.Write(bb, 0, 6);
		}

		/// <summary>
		/// 发送Set请求
		/// </summary>
		/// <param name="page"></param>
		/// <param name="addr"></param>
		/// <param name="length"></param>
		/// <param name="ssp"></param>
		/// <returns></returns>
		private void ComSet(int page, int addr, int[] bs, SerialPort ssp) {
			int checksum = 0;

			byte[] bb = new byte[6 + bs.Length];
			bb[0] = CMD_SET;
			bb[1] = (byte)page;
			bb[2] = (byte)addr;
			bb[3] = (byte)bs.Length;
			checksum = CMD_SET + page + addr + bs.Length;

			for (int i = 0; i < bs.Length; i++) {
				bb[4 + i] = (byte)bs[i];
				checksum += bb[4 + i];
			}
			bb[bs.Length + 4] = (byte)checksum;
			bb[bs.Length + 5] = CMD_END;
			ssp.Write(bb, 0, bs.Length + 6);
		}

		/// <summary>
		/// 发送请求
		/// </summary>
		/// <param name="ssp"></param>
		/// <returns></returns>
		public String SendRequest(SerialPort ssp) {
			if (this.status != STATUS_STANDBY) {
				return "Send Again !?!?!?";
			}
			if (ssp == null) {
				return "SerialPort == null";
			} else {
				if (ssp.IsOpen) {
					if (this.isGet) {
						ComGet(this.page, this.addr, this.length, ssp);
					} else {
						ComSet(this.page, this.addr, this.data, ssp);
					}
					status = STATUS_SENDED;
					return null;
				} else {
					return "SerialPort isn't Open";
				}
			}
		}

		private byte[] RecBuf = null;

		public String DealResponse(byte[] buf) {

			if (RecBuf == null) {
				RecBuf = buf;
			} else {//连接数组
				byte[] newbuf = new byte[buf.Length + RecBuf.Length];
				for (int i = 0; i < RecBuf.Length; i++) {//复制
					newbuf[i] = RecBuf[i];
				}
				for (int i = 0; i < buf.Length; i++) {//添加
					newbuf[i + RecBuf.Length] = buf[i];
				}
				RecBuf = newbuf;
			}

			if (RecBuf.Length >= 4) {
				if (RecBuf[0] == CMD_GET) {//get
					if (RecBuf[RecBuf.Length - 1] == 0xA6) {
						int checksum = 0;
						for (int i = 0; i < (RecBuf.Length - 2); i++) {
							checksum += RecBuf[i];
						}
						if (((byte)checksum) == RecBuf[RecBuf.Length - 2]) {//CheckSum OK
							if (RecBuf.Length == 4) {
								this.status = STATUS_FINISH_ERR;
								return (RESPONSE_FAIL);
							}
							this.data = new int[this.length];
							for (int i = 0; i < this.length; i++) {
								data[i] = RecBuf[i + 2];
							}
							this.status = STATUS_FINISH_OK;
							return (RESPONSE_SUCCESS);
						} else {
							this.status = STATUS_FINISH_ERR;
							return (RESPONSE_FAIL);
						}
					} else {
						return (RESPONSE_NOT_FINISHED);
					}
				} else if (RecBuf[0] == CMD_SET) {//set 
					if (RecBuf[RecBuf.Length - 1] == 0xA6) {
						if (RecBuf[1] == CMD_OK) {
							this.status = STATUS_FINISH_OK;
							return (RESPONSE_SUCCESS);
						} else {
							this.status = STATUS_FINISH_ERR;
							return (RESPONSE_FAIL);
						}
					} else {
						return (RESPONSE_NOT_FINISHED);
					}
				} else {
					this.status = STATUS_FINISH_ERR;
					return (RESPONSE_FAIL);
				}
			} else {
				this.status = STATUS_FINISH_ERR;
				return (RESPONSE_FAIL);
			}
		}

		public const String RESPONSE_NOT_FINISHED = "RESPONSE_NOT_FINISHED";
		public const String RESPONSE_FAIL = "RESPONSE_FAIL";
		public const String RESPONSE_SUCCESS = "RESPONSE_SUCCESS";

	}
}
