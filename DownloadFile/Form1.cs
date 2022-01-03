using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.IO;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using xNetStandard;


namespace DownloadFile
{
    public partial class Form1 : Form
    {
        private static EventWaitHandle waitHandle = new ManualResetEvent(initialState: true);
        private static Thread calculateThread;
        private static string cookie = "";
        private static bool usingCookie = true;
        private static int coutProduct = 0;
        private static string filePathFolderSave = "";
        private static string fileCodes = "";
        private static string keyWord = "";
        private static string[] lsUseagent;
        private static object looks = new object();


        public Form1()
        {
            InitializeComponent();
        }

        private string randomUseagent(string[] lsUseagent)
        {
            Random rd = new Random();
            return lsUseagent[rd.Next(0, lsUseagent.Length - 1)];
        }

        #region Login
        private string getNonceLogin()
        {
            xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
            httpRequest.AddHeader("user-agent", randomUseagent(lsUseagent));

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
            httpRequest.AddHeader("user-agent", randomUseagent(lsUseagent));
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
                httpRequest.AddHeader("user-agent", randomUseagent(lsUseagent));

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

        private string getTypeQuery()
        {
            string temp1 = "";
            string temp2 = "";
            for (int i = 0; i < checkboxType.CheckedItems.Count; i++)
            {
                var item = checkboxType.CheckedItems[i];
                if (i != checkboxType.CheckedItems.Count - 1)
                    temp1 += "\"type:" + item + "\",";
                else
                    temp1 += "\"type:" + item + "\"";
            }
            for (int i = 0; i < checkBoxCategories.CheckedItems.Count; i++)
            {
                var item = checkBoxCategories.CheckedItems[i];
                if (i != checkBoxCategories.CheckedItems.Count - 1)
                    temp2 += "\"category:" + item + "\",";
                else
                    temp2 += "\"category:" + item + "\"";
            }
            return "["+ temp1 +"],["+temp2+"]";
        }

        private string getLinkDowload(string linkProduct, string userAgent)
        {
            xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
            httpRequest.AddHeader("cookie", cookie);
            httpRequest.AddHeader("user-agent", userAgent);
            string linkDowload = "";

            try
            {
                var repon = httpRequest.Get(linkProduct).ToString();
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(repon);
                var tag = doc.DocumentNode.SelectNodes("//*[@id=\"js-show-modal-when-dissapear\"]/div/div/a");
                if (tag != null)
                {
                    foreach (var item in tag)
                    {
                        linkDowload = item.Attributes["href"].Value;
                    }
                }
            }
            catch (Exception)
            {
            }
            httpRequest.Close();
            return linkDowload;
        }

        private bool dowloadFile(string linkProduct, string userAgent, string fileCode)
        {
            xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest();
            httpRequest.AddHeader("cookie", cookie);
            httpRequest.AddHeader("user-agent", userAgent);

            try
            {
                var resp = httpRequest.Get(linkProduct);                
                if (resp.ContentType.Equals("text/html"))
                {
                    MessageBox.Show("Yêu cầu xác minh capcha trên trang chủ rồi mới đóng thông báo này để tiếp tục!");
                }
                resp.ToFile(filePathFolderSave + @"\" + fileCode + ".zip");
                httpRequest.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void getProduct()
        {
            string typeQuery = Http.UrlEncode("[" + getTypeQuery() + "]");
            string Url = @"https://ko25i12xz3-2.algolianet.com/1/indexes/*/queries?x-algolia-agent=Algolia%20for%20JavaScript%20(3.33.0)%3B%20Browser%20(lite)%3B%20instantsearch.js%20(4.8.3)%3B%20JS%20Helper%20(3.2.2)&x-algolia-application-id=KO25I12XZ3&x-algolia-api-key=8f1c3789bd528e25c3e27ab835584af5";

            try
            {
                xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
                httpRequest.AddHeader("Origin", "https://www.creativefabrica.com");
                string body = "{\"requests\": [{\"indexName\": \"prod_Productsv2\",\"params\": \"query=" + keyWord + "&hitsPerPage=" + coutProduct + "&facets=%5B%22type%22%2C%22category%22%2C%22hasPod%22%5D&tagFilters=&facetFilters=" + typeQuery + "\"}]}";
                var response = httpRequest.Post(Url, body , "application/json").ToString();

                JObject rss = JObject.Parse(response);
                dynamic jsonNode = (JArray)rss["results"][0]["hits"];
                if (jsonNode.Count == 0)
                {
                    MessageBox.Show("Không có kết quả, thử từ khoá khác hoặc type khác!");
                    return;
                }
                if (jsonNode.Count < coutProduct)
                    coutProduct = jsonNode.Count;
                int iThread = 0;
                int num = 5;
                if (coutProduct < num)
                {
                    num = coutProduct;
                }
                progressBar1.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Maximum = coutProduct;
                });
                int num2 = 0;
                while (num2 < coutProduct)
                {
                    if (iThread < num)
                    {
                        waitHandle.WaitOne();
                        Interlocked.Increment(ref iThread);
                        int index = num2;
                        var item = jsonNode[index];
                        num2++;
                        progressBar1.Invoke((MethodInvoker)delegate
                        {
                            progressBar1.Value = num2;
                        });

                        string fileCode = fileCodes + "-" + index.ToString();
                        new Thread((ThreadStart)delegate
                        {
                            string tags = "";
                            string type = item.type;
                            string name = HttpUtility.HtmlDecode((string)item.name_en);
                            string price = item.price;
                            string category = ((dynamic)item.category).Count != 0 ? HttpUtility.HtmlDecode(HttpUtility.HtmlDecode((string)(dynamic)item.category[0])) : "";
                            string description = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode((string)item.description_en));
                            string linkProduct = item.url;
                            string userAgent = randomUseagent(lsUseagent);
                            string linkDowload = getLinkDowload(linkProduct, userAgent);

                            JArray jsonTag = (JArray)item.tags;
                            if (jsonTag != null)
                            {
                                foreach (string items in jsonTag)
                                {
                                    tags += items + ",";
                                }
                                if (tags.Length > 0)
                                    tags = tags.Remove(tags.Length - 1);
                            }

                            if(dowloadFile(linkDowload, userAgent, fileCode))
                            {
                                Product productTemp = new Product(name, type, tags, price, category, description, linkProduct, linkDowload);
                                lock (looks)
                                {
                                    addValueExcel(index + 2, fileCode, productTemp);
                                }
                            }                          

                            Interlocked.Decrement(ref iThread);
                        }).Start();
                    }
                    else
                    {
                        Application.DoEvents();
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }
                while (iThread > 0)
                {
                    Application.DoEvents();
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                progressBar1.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show("Hoàn thành");
                    btnStart.Enabled = true;
                });
            }
            catch (Exception e)
            {
                MessageBox.Show("Mạng có vấn đề" + e.Message);
            }
        }
        #endregion


        private void btnStart_Click(object sender, EventArgs e)
        {
            if(!File.Exists(filePathFolderSave + @"\DataWTM.xlsx"))
                createExcel();
            keyWord = txtKeyword.Text;
            calculateThread = new Thread(() => getProduct());
            calculateThread.IsBackground = true;
            calculateThread.Start();
            btnStart.Enabled = false;
        }

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
                btnStart.Invoke((MethodInvoker)delegate
                {
                    btnStart.Enabled = true;
                    MessageBox.Show("Đăng nhập thành công");
                });
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

        private void btnLogin_Click(object sender, EventArgs e)
        {
            backgroundWorker2.RunWorkerAsync();
            Properties.Settings.Default.Cookie = txtCookie.Text;
            Properties.Settings.Default.Save();
        }

        private void txtCount_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ErrorProvider error = new ErrorProvider();
            if (String.IsNullOrEmpty(txtCount.Text) || !int.TryParse(txtCount.Text, out coutProduct))
            {
                e.Cancel = true;
                txtCount.Focus();
                error.SetError(txtCount, "Yêu cầu nhập số lượng!");
            }
            else
            {
                e.Cancel = false;
                error.SetError(txtCount, null);
            }
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog open = new FolderBrowserDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                filePathFolderSave = open.SelectedPath;
                txtFolderPath.Text = filePathFolderSave;
            }
        }

        private void createExcel()
        {
            var path = filePathFolderSave+ @"\DataWTM.xlsx";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Sheet 1");
                worksheet.Cells[1, 1].Value = "Filecode";
                worksheet.Cells[1, 2].Value = "Title";
                worksheet.Cells[1, 3].Value = "Main Tags";
                worksheet.Cells[1, 4].Value = "Price";
                worksheet.Cells[1, 5].Value = "Discount Price";
                worksheet.Cells[1, 6].Value = "Categories";
                worksheet.Cells[1, 7].Value = "Description";

                FileInfo fi = new FileInfo(path);
                excelPackage.SaveAs(fi);
                excelPackage.Dispose();
            }
        }

        private void addValueExcel(int indexCell,string FileCode, Product product)
        {
            var path = filePathFolderSave + @"\DataWTM.xlsx";

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage excelPackage = new ExcelPackage(path))
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

                worksheet.Cells[indexCell, 1].Value = FileCode;
                worksheet.Cells[indexCell, 2].Value = product.getName();
                worksheet.Cells[indexCell, 3].Value = product.getTags();
                worksheet.Cells[indexCell, 4].Value = product.getPrice();
                worksheet.Cells[indexCell, 5].Value = "";
                worksheet.Cells[indexCell, 6].Value = product.getCategory();
                worksheet.Cells[indexCell, 7].Value = product.getDescription();

                excelPackage.Save();
                excelPackage.Dispose();
            }
        }
        private void txtFileCode_TextChanged(object sender, EventArgs e)
        {
            fileCodes = txtFileCode.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtCookie.Text = Properties.Settings.Default.Cookie;
            lsUseagent = File.ReadAllLines(@"Useragent.txt");
        }        

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (waitHandle.WaitOne(0))
            {
                waitHandle.Reset();
                btnStop.Text = "Resume";
            }
            else
            {
                waitHandle.Set();
                btnStop.Text = "Stop";
            }
        }
    }

}
