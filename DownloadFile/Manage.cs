using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace DownloadFile
{
    class Manage
    {
        public delegate void thongBao(string status, string mes);
        public event thongBao thongBaoEvent;
        public delegate void countDone(int count);
        public event countDone countDoneEvent;

        private static EventWaitHandle waitHandle = new ManualResetEvent(initialState: true);
        private static readonly object _lockA = new object();

        public int indexPtoduct = 0;
        public int maxProduct { get; private set; }
        public int countDones = 0;
        private bool checkThongBao = false;

        private static List<Product> lsPrduct;

        private void reset()
        {
            indexPtoduct = 0;
            countDones = 0;
            if (lsPrduct != null)
                lsPrduct.Clear();
        }

        private string queryGetResponse(string typeQuery, int page, int hitsPerPage)
        {
            string response;
            string Url = @"https://ko25i12xz3-2.algolianet.com/1/indexes/*/queries?x-algolia-agent=Algolia%20for%20JavaScript%20(3.33.0)%3B%20Browser%20(lite)%3B%20instantsearch.js%20(4.8.3)%3B%20JS%20Helper%20(3.2.2)&x-algolia-application-id=KO25I12XZ3&x-algolia-api-key=8f1c3789bd528e25c3e27ab835584af5";
            try
            {
                xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
                httpRequest.AddHeader("Origin", "https://www.creativefabrica.com");
                string body = "{\"requests\":[{\"indexName\": \"prod_Productsv2\",\"params\": \"highlightPreTag=__ais-highlight__&highlightPostTag=__%2Fais-highlight__&page=" + page + "&query=" + Form1.keyWord + "&hitsPerPage=" + hitsPerPage + "&facets=%5B%22type%22%2C%22category%22%2C%22hasPod%22%5D&tagFilters=&facetFilters=" + typeQuery + "\"}]}";
                response = httpRequest.Post(Url, body, "application/json").ToString();
            }
            catch (Exception e)
            {
                thongBaoEvent("mang", e.Message);
                response = "";
            }
            return response;
        }

        private List<Product> getProductWithResponse(string response)
        {
            List<Product> list = new List<Product>();
            indexPtoduct = 0;
            try
            {
                JObject rss = JObject.Parse(response);
                dynamic jsonNode = (JArray)rss["results"][0]["hits"];
                int count = jsonNode.Count;
                if (count == 0)
                {
                    return list;
                }
                int num2 = 0;
                while (num2 < count)
                {
                    var item = jsonNode[num2];
                    num2++;

                    string fileCode = Form1.fileCodes + "-" + indexPtoduct.ToString();
                    indexPtoduct++;
                    string tags = "";
                    string type = item.type;
                    string name = HttpUtility.HtmlDecode((string)item.name_en);
                    string price = item.price;
                    string category = ((dynamic)item.category).Count != 0 ? HttpUtility.HtmlDecode(HttpUtility.HtmlDecode((string)(dynamic)item.category[0])) : "";
                    string description = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode((string)item.description_en));
                    string linkProduct = item.url;
                    string wtm = item.image;

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

                    Product productTemp = new Product(name, type, tags, price, category, description, linkProduct, fileCode, wtm);
                    productTemp.downloadEvent += ProductTemp_downloadEvent;
                    list.Add(productTemp);
                }

                return list;
            }
            catch (Exception)
            {
                return list;
            }
        }

        private void ProductTemp_downloadEvent(string status)
        {
            if (status.Equals("done"))
            {
                countDones++;
                countDoneEvent(countDones);
            }
            else if (status.Equals("Captcha"))
            {
                waitHandle.Reset();
                if (!checkThongBao)
                {
                    checkThongBao = true;
                    thongBaoEvent("captcha", "Vui lòng xác nhận captcha để tiếp tục tải!");
                }
            }
        }

        public void stopAndResume(bool check)
        {
            if (waitHandle.WaitOne(0) && check)
            {
                waitHandle.Reset();
            }
            else
            {
                waitHandle.Set();
                if (checkThongBao)
                    checkThongBao = false;
            }
        }

        public JObject loadSP(string typeQuery)
        {
            string response = queryGetResponse(typeQuery, 0, 0);
            JObject rss = JObject.Parse(response);
            maxProduct = (int)rss["results"][0]["nbHits"];
            reset();
            return rss;
        }

        public JObject loadSP()
        {
            string response = queryGetResponse("", 0, 0);
            JObject rss = JObject.Parse(response);
            maxProduct = (int)rss["results"][0]["nbHits"];
            reset();
            return rss;
        }

        public void getAllProduct(string typeQuery)
        {
            lsPrduct = new List<Product>();
            new Thread((ThreadStart)delegate
            {
                if (((Form1.coutProduct <= maxProduct) || (Form1.coutProduct > maxProduct)) && maxProduct <= 1000)
                {
                    string response = queryGetResponse(typeQuery, 0, Form1.coutProduct);
                    lsPrduct.AddRange(getProductWithResponse(response));
                }
                else
                {
                    for (int i = 0; i <= Form1.coutProduct / 1000; i++)
                    {
                        if (i == Form1.coutProduct / 1000)
                        {
                            string response = queryGetResponse(typeQuery, i, Form1.coutProduct - (1000 * (Form1.coutProduct / 1000)));
                            lsPrduct.AddRange(getProductWithResponse(response));
                        }
                        else
                        {
                            string response = queryGetResponse(typeQuery, i, 1000);
                            lsPrduct.AddRange(getProductWithResponse(response));
                        }
                    }
                }
                thongBaoEvent("getAll", "Lấy sản phẩm thành công");
            })
            { IsBackground = true }.Start();
        }

        public void downLoadAllProduct(bool checkCreateFolder)
        {
            int num2 = 0;
            int iThread = 0;
            while (num2 < lsPrduct.Count)
            {

                if (iThread < 5)
                {
                    waitHandle.WaitOne();
                    Interlocked.Increment(ref iThread);
                    int index = num2;
                    var item = lsPrduct[index];
                    num2++;
                    new Thread((ThreadStart)delegate
                    {
                        if(!checkCreateFolder)
                            item.downloadFileNoFolder();
                        else
                            item.downloadFileCreateFolder();
                        lock (_lockA)
                        {
                            addValueExcel(index + 2, item.fileCode, item);
                            Monitor.Pulse(_lockA);
                        }
                        Interlocked.Decrement(ref iThread);
                    }).Start();
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
            while (iThread > 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            thongBaoEvent("taiDone", "Đã tải xong");
            reset();
        }

        public void createExcel()
        {
            var path = Form1.filePathFolderSave + @"\DataWTM.xlsx";
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

        private void addValueExcel(int indexCell, string FileCode, Product product)
        {
            var path = Form1.filePathFolderSave + @"\DataWTM.xlsx";

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage excelPackage = new ExcelPackage(path))
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

                worksheet.Cells[indexCell, 1].Value = FileCode;
                worksheet.Cells[indexCell, 2].Value = product.Name;
                worksheet.Cells[indexCell, 3].Value = product.tags;
                worksheet.Cells[indexCell, 4].Value = product.price;
                worksheet.Cells[indexCell, 5].Value = "";
                worksheet.Cells[indexCell, 6].Value = product.category;
                worksheet.Cells[indexCell, 7].Value = product.description;

                excelPackage.Save();
                excelPackage.Dispose();
            }
        }
    }
}
