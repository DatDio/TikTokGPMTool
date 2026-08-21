using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikTokGPMTool.Data.Enitities
{
	public class Account
	{
		public int AccountIDKey { get; set; }
		public string FullAccount { get; set; } = "";
		public string PhoneNumber { get; set; } = "";
		public string Email { get; set; } = "";
		public string PassEmail { get; set; } = "";
		public string C_2FA { get; set; } = "";
		public string Cookie { get; set; } = "";
		public string UserAgent { get; set; } = "";
		public string GPMID { get; set; } = "";
		public string Proxy { get; set; } = "";
		public string Status { get; set; } = "";
	}
}
