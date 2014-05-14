using System;
using System.Xml;
using Microsoft.Win32;
using System.Text;
using System.Security;
using System.Collections;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

class UpdateEnvironment {
	[DllImport("user32.dll")]
	public static extern short GetKeyState(int keyCode);
	private const int VK_SCROLL=0x91;

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SendMessageTimeout(
		IntPtr hWnd,
		int Msg,
		int wParam,
		string lParam,
		int fuFlags,
		int uTimeout,
		out int lpdwResult
	);

	public const int HWND_BROADCAST = 0xffff;
	public const int WM_SETTINGCHANGE = 0x001A;
	public const int SMTO_NORMAL = 0x0000;
	public const int SMTO_BLOCK = 0x0001;
	public const int SMTO_ABORTIFHUNG = 0x0002;
	public const int SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;

	public static string configFile = "config.xml";
	public static XmlDocument config;
	public static XmlNode Root;

	private static Hashtable ENV = new Hashtable();
	private static RegistryKey SYSTEM;
	static void Main(string[] args)
	{
		bool unInstall = Convert.ToBoolean(GetKeyState(VK_SCROLL)&1);
		if(unInstall){//卸载,程序启动时如果 ScrollLock 灯是亮着的
			uninstall();
			return;
		}
		SYSTEM = Registry.ClassesRoot.OpenSubKey(@".env", true);
		if(SYSTEM == null){
			string self = Application.ExecutablePath;
			RegistryKey rk = Registry.ClassesRoot.CreateSubKey(".env");
			rk.SetValue("", "UpdateEnvironment", RegistryValueKind.String);

			Registry.LocalMachine.CreateSubKey(@"SoftWare\UpdateEnvironment");

			rk = Registry.ClassesRoot.CreateSubKey("UpdateEnvironment");

			RegistryKey icon = rk.CreateSubKey("DefaultIcon");
			icon.SetValue("", string.Format("{0},0", self), RegistryValueKind.String);

			RegistryKey sk = rk.CreateSubKey("shell");

			RegistryKey open = sk.CreateSubKey("open");
			RegistryKey edit = sk.CreateSubKey("edit");

			RegistryKey cmd = open.CreateSubKey("command");
			cmd.SetValue("", string.Format(@"""{0}"" ""%1""", self), RegistryValueKind.String);

			cmd = edit.CreateSubKey("command");
			cmd.SetValue("", @"%SystemRoot%\system32\notepad.exe ""%1""", RegistryValueKind.ExpandString);
			//安装
			if(args.Length == 0){
				MessageBox.Show("环境变量自动配置程序安装成功!", "安装成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		if(args.Length == 0)return;
		string configFile = args[0];

		try {
			config = new XmlDocument();
			config.Load(configFile);
			Root = config.SelectSingleNode("Environment");
		} catch{
			MessageBox.Show("配置文件格式错误!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		if (Root == null){
			MessageBox.Show("配置文件格式错误!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		string M = md5(configFile);
		RegistryKey InstallInfo = Registry.LocalMachine.OpenSubKey(@"SoftWare\UpdateEnvironment", true);
		if(InstallInfo == null){
			MessageBox.Show("程序配置信息不正确,请卸载后重新安装!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		string D = (string)InstallInfo.GetValue(M, "", RegistryValueOptions.DoNotExpandEnvironmentNames);
		if(D != ""){
			DialogResult R = MessageBox.Show(string.Format("该环境变量配置文件您已于{0}安装过,请确认是否继续安装?", D), "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
			if(R == DialogResult.Cancel){
				return;
			}
		}

		ENV.Add("FOLDER", configFile.Substring(0, configFile.LastIndexOf('\\')));

		SYSTEM = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", true);

		if (SYSTEM == null){
			MessageBox.Show("打开注册表失败,请尝试以管理员身份重新运行本程序!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		XmlNodeList S = Root.SelectNodes("system/set");
		string k, v, vv, vvv;
		foreach (XmlNode env in S) {
			k = env.Attributes["name"].Value;
			v = env.Attributes["value"].Value;
			vv = (string)SYSTEM.GetValue(k, "", RegistryValueOptions.DoNotExpandEnvironmentNames);
			if(k.ToLower() == "path"){
				vvv = v.Replace("{PATH}", "");
				if(vv.IndexOf(vvv) != -1){//如果path中已经有要新加的内容,则直接跳过
					continue;
				}
			}
			v = ExpandString(v);
			if(vv != v){
				SYSTEM.SetValue(k, v, (v.IndexOf("%") != -1)?RegistryValueKind.ExpandString:RegistryValueKind.String);
			}
		}

		SYSTEM = Registry.CurrentUser.OpenSubKey(@"Environment", true);
		S = Root.SelectNodes("user/set");
		foreach (XmlNode env in S) {
			k = env.Attributes["name"].Value;
			v = ExpandString(env.Attributes["value"].Value);
			SYSTEM.SetValue(k, v, (v.IndexOf("%") != -1)?RegistryValueKind.ExpandString:RegistryValueKind.String);
		}

		InstallInfo.SetValue(M, DateTime.Now.ToString("F"), RegistryValueKind.String);

		//通知系统更新环境变量,以使新做的改动立即生效,否则对环境变量的修改只有在注销后或者重启电脑后才能生效
		BroadcastEnvironment();

		MessageBox.Show("环境变量设置完成!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	static void uninstall(){
		try{
			Registry.ClassesRoot.DeleteSubKeyTree(".env");
		}catch(SecurityException){
			MessageBox.Show("卸载失败!\n请尝试以管理员身份再次运行本程序!", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}catch{}
		try{
			Registry.ClassesRoot.DeleteSubKeyTree("UpdateEnvironment");
		}catch(SecurityException){
			MessageBox.Show("卸载失败!\n请尝试以管理员身份再次运行本程序!", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}catch{}
		MessageBox.Show("卸载成功!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	static string TranslateString(Match m){
		string k = m.Groups["KEY"].Value.ToString(), ret = null;

		if(ENV.Contains(k)){
			ret = (string)ENV[k];
		}else{
			ret = (string)SYSTEM.GetValue(k, "", RegistryValueOptions.DoNotExpandEnvironmentNames);
		}

		return ret;
	}

	static string ExpandString(string k){
		Regex r = new Regex(@"\{(?<KEY>[a-z]+)\}", RegexOptions.IgnoreCase);
		return (new Regex(";;+").Replace(r.Replace(k, new MatchEvaluator(TranslateString)), ";"));
	}

	static void BroadcastEnvironment(){
		int result;
		//SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment", SMTO_BLOCK | SMTO_ABORTIFHUNG | SMTO_NOTIMEOUTIFNOTHUNG, 1000, out result);
		SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment", SMTO_ABORTIFHUNG, 1000, out result);
	}

	static private string md5(string str)
	{
		MD5 m = new MD5CryptoServiceProvider();
		byte[] s = m.ComputeHash(UnicodeEncoding.UTF8.GetBytes(str));
		return BitConverter.ToString(s).Replace("-", "");
	}

}
