using DownloadFile.Properties;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using xNetStandard;


namespace DownloadFile
{
    public partial class Form1 : Form
    {
        private static Manage manage;

        private static string[] lsUseagent;
        public static string cookie = "";
        private static bool loginOK = false;
        private static bool usingCookie = true;
        private static bool stop = false;
        private static bool searchDesigner = false;
        private static int idDesigner = 0;

        public static string filePathFolderSave = "";
        public static string fileCodes = "";
        public static string keyWord = "";

        public static int coutProduct = 0;

        public Form1()
        {
            InitializeComponent();
        }

        public static string randomUseagent()
        {
            Random rd = new Random();
            return lsUseagent[rd.Next(0, lsUseagent.Length - 1)];
        }

        #region Login
        private string getNonceLogin()
        {
            xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
            httpRequest.AddHeader("user-agent", randomUseagent());

            try
            {
                var repone = httpRequest.Get("https://www.creativefabrica.com/login/").ToString();

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(repone);
                httpRequest.Close();
                return doc.DocumentNode.SelectSingleNode("//*[@id=\"woocommerce-login-nonce\"]").Attributes["value"].Value;
            }
            catch (Exception)
            {
                return null;
            }            
        }

        private void login(string user, string pass)
        {
            string nonce;
            xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
            httpRequest.AddHeader("user-agent", randomUseagent());
            httpRequest.AddHeader("referer", "https://www.creativefabrica.com/my-account/");

            nonce = getNonceLogin();
            try
            {
                cookie = httpRequest.Post("https://www.creativefabrica.com/cfsecure/login/", $"username={user}&password={pass}&rememberme=forever&recaptcha_response=&woocommerce-login-nonce={nonce}", "application/x-www-form-urlencoded; charset=UTF-8").Cookies.ToString();
            }
            catch (Exception)
            {
            }
        }

        private bool checkLiveCookie(string cookies)
        {
            xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
            
            try
            {
                httpRequest.AddHeader("cookie", cookies);
                httpRequest.AddHeader("user-agent", randomUseagent());

                var reporn = httpRequest.Get("https://www.creativefabrica.com/my-account/").ToString();

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(reporn);
                var tag = doc.DocumentNode.SelectSingleNode("//*[@id=\"referral_url_value\"]");
                httpRequest.Close();

                if (tag != null)
                    return true;
                return false;
            }
            catch (Exception)
            {
                return false;
            }            
        }
        
        #endregion

        #region Dowload File

        private int getIdDesigner(string name)
        {
            int id = 0;
            name = name.Replace(" ", "-");
            string Url = "https://www.creativefabrica.com/designer/"+ name;
            HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(Url);
            var tag = doc.DocumentNode.SelectSingleNode("//button[contains(@data-text-on, 'Following Designer')]");
            id = int.Parse(tag.Attributes["data-user-id"].Value);
            return id;
        }

        private string getTypeQuery()
        {
            string temp1 = "";
            string temp2 = "";
            string temp3 = $"\"designer.designerId:\",\"type: -Classes\"";
            if (searchDesigner)
            {
                idDesigner = getIdDesigner(txtKeyword.Text);
                temp3 = $"\"designer.designerId:{idDesigner}\",\"type: -Classes\"";
            }
            for (int i = 0; i < checkboxType.CheckedItems.Count; i++)
            {
                string item = (string)checkboxType.CheckedItems[i];
                string p = Regex.Replace(item, @"\d", "");
                string q = Regex.Replace(p, @"[^a-zA-Z ]", "");
                if (i != checkboxType.CheckedItems.Count - 1)
                {
                    temp1 += "\"type:" + q + "\",";
                }                    
                else
                    temp1 += "\"type:" + q + "\"";
                //maxProduct += int.Parse(Regex.Match(item, @"\d+").Value);
            }
            for (int i = 0; i < checkBoxCategories.CheckedItems.Count; i++)
            {
                string item = (string)checkBoxCategories.CheckedItems[i];
                string p = Regex.Replace(item, @"\d", "");
                string q = Regex.Replace(p, @"[^a-zA-Z ]", "");
                if (i != checkBoxCategories.CheckedItems.Count - 1)
                {
                    temp2 += "\"category:" + q + "\",";
                }
                else
                    temp2 += "\"category:" + q + "\"";
                //maxProduct += int.Parse(Regex.Match(item, @"\d+").Value;
            }
            return Http.UrlEncode("[" + temp3 + ",[" + temp1 + "],[" + temp2 + "]]").Replace("+","");
        }

        #endregion

        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (!usingCookie)
            {
                string user = "", pass = "";
                Invoke((MethodInvoker)delegate
                {
                    user = txtUsername.Text;
                    pass = txtPassword.Text;
                });
                login(user, pass);
            }
            else
            {
                Invoke((MethodInvoker)delegate
                {
                    cookie = txtCookie.Text;
                });
            }
            if (checkLiveCookie(cookie))
            {
                MessageBox.Show("Đăng nhập thành công");
                loginOK = true;
            }
            else
                MessageBox.Show("Tài khoản không thể đăng nhập, Kiểm tra lại kết nối internet hoặc thông tin tài khoản");
        }

        private void rdTaiKhoan_CheckedChanged(object sender, EventArgs e)
        {
            if (rdTaiKhoan.Checked)
            {
                txtUsername.Visible = true;
                txtPassword.Visible = true;
                txtCookie.Visible = false;
                usingCookie = false;
            }
        }

        private void rdCookie_CheckedChanged(object sender, EventArgs e)
        {
            if (rdCookie.Checked)
            {
                txtUsername.Visible = false;
                txtPassword.Visible = false;
                txtCookie.Visible = true;
                usingCookie = true;
            }
        }

        private void txtCount_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (String.IsNullOrEmpty(txtCount.Text) || !int.TryParse(txtCount.Text, out coutProduct) || int.Parse(txtCount.Text) > 1000)
            {
                e.Cancel = true;
                txtCount.Focus();
                errorProvider1.SetError(txtCount, "Yêu cầu nhập số lượng nhỏ hơn hoặc bằng 1000");
            }
            else
            {
                e.Cancel = false;
                errorProvider1.SetError(txtCount, null);
            }
        }

        private void txtFileCode_TextChanged(object sender, EventArgs e)
        {
            fileCodes = txtFileCode.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtCookie.Text = Settings.Default.Cookie;
            lsUseagent = File.ReadAllLines(@"Useragent.txt");
            manage = new Manage();
            manage.thongBaoEvent += Manage_thongBaoEvent;
        }

        private void Manage_thongBaoEvent(string status, string mes)
        {
            if (status.Equals("mang"))
                MessageBox.Show($"Lỗi mạng {mes}");
            if (status.Equals("getAll"))
            {
                Invoke((MethodInvoker)delegate
                {
                    progressBar1.Minimum = 0;
                    progressBar1.Maximum = manage.indexPtoduct;
                    progressBar1.Value = 0;
                });

                manage.countDoneEvent += Manage_countDoneEvent;
                DialogResult dialogResult = MessageBox.Show("Tải WTM chọn YES, không tải chọn NO", "Chọn tải WTM", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    manage.downLoadAllProduct(true);
                }
                else
                    manage.downLoadAllProduct(false);
            }
            if (status.Equals("captcha"))
            {
                DialogResult dialogResult = MessageBox.Show(mes, "Captcha", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    manage.stopAndResume(false);
                }
            }
            if (status.Equals("taiDone"))
            {
                MessageBox.Show($"{mes}");
                Invoke((MethodInvoker)delegate
                {
                    btnStart.Enabled = true;
                    btnLoadSP.Enabled = true;
                    btnSelectFolder.Enabled = true;
                });
            }
        }

        private void Manage_countDoneEvent(int count)
        {
            Invoke((MethodInvoker)delegate
            {
                progressBar1.Value = count;
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void txtKeyword_TextChanged(object sender, EventArgs e)
        {
            keyWord = txtKeyword.Text;
        }

        private void iconButton1_Click(object sender, EventArgs e)
        {
            int i = 0;
            int j = 0;
            JObject rss;
            if (searchDesigner)
            {
                string typeQuery = getTypeQuery();
                rss = manage.loadSP(typeQuery);
            }else
                rss = manage.loadSP();
            var jsonType = rss["results"][0]["facets"]["type"];
            var jsonCategory = rss["results"][0]["facets"]["category"];

            countSP.Text = $"{keyWord} hiện có: {manage.maxProduct} sản phẩm";
            checkboxType.Items.Clear();
            checkBoxCategories.Items.Clear();
            if (jsonType != null && jsonCategory != null)
            {
                foreach (JProperty item in jsonType.Children())
                {
                    checkboxType.Items.Insert(i, item.Name + ":" + item.Value);
                    i++;
                }
                foreach (JProperty item in jsonCategory.Children())
                {
                    checkBoxCategories.Items.Insert(j, item.Name + ":" + item.Value);
                    j++;
                }
            }
            else
                countSP.Text = $"{keyWord} hiện không có sản phẩm nào";
        }

        private void iconButton1_Click_1(object sender, EventArgs e)
        {
            FolderBrowserDialog open = new FolderBrowserDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                filePathFolderSave = open.SelectedPath;
                txtFolderPath.Text = filePathFolderSave;
                if (loginOK)
                    btnStart.Enabled = true;
            }
        }

        private void btnLogin_Click_1(object sender, EventArgs e)
        {
            backgroundWorker2.RunWorkerAsync();
            Settings.Default.Cookie = txtCookie.Text;
            Settings.Default.Save();
        }

        private void iconButton1_Click_2(object sender, EventArgs e)
        {
            if (!File.Exists(filePathFolderSave + @"\dataWTM.xlsx"))
                manage.createExcel();

            string typeQuery = getTypeQuery();

            manage.getAllProduct(typeQuery);

            btnStart.Enabled = false;
            btnLoadSP.Enabled = false;
            btnSelectFolder.Enabled = false;
        }

        private void iconButton1_Click_3(object sender, EventArgs e)
        {
            if (!stop)
            {
                manage.stopAndResume(true);
                btnStop.Text = "Resume";
                btnStop.IconChar = FontAwesome.Sharp.IconChar.StepForward;
                stop = true;
            }
            else
            {
                stop = false;
                manage.stopAndResume(false);
                btnStop.Text = "Stop";
                btnStop.IconChar = FontAwesome.Sharp.IconChar.Stop;
            }
        }

        private void searchDesiger_CheckedChanged(object sender, EventArgs e)
        {
            if (searchDesigner)
            {
                searchDesigner = false;
                idDesigner = 0;
            }else
                searchDesigner = true;
        }
    }

}
