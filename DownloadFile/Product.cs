using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DownloadFile
{
    class Product
    {
        public delegate void download(string status);
        public event download downloadEvent;

        public string Name { get; private set; }
        public string type { get; private set; }
        public string tags { get; private set; }
        public string price { get; private set; }
        public string category { get; private set; }
        public string description { get; private set; }
        public string linkProduct { get; private set; }
        public string fileCode { get; private set; }
        public string wtm { get; private set; }
        private string user_agent;


        public Product(string name, string type, string tags,string price, string category, string description, string linkProduct, string fileCode, string wtm)
        {
            Name = name;
            this.type = type;
            this.tags = tags;
            this.price = price;
            this.description = description;
            this.linkProduct = linkProduct;
            this.category = category;
            this.fileCode = fileCode;
            this.wtm = wtm;
            user_agent = Form1.randomUseagent();
        }

        private List<string> getLinkDowload(string linkProduct)
        {
            xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest { KeepAlive = false, ConnectTimeout = 8000, IgnoreProtocolErrors = true, AllowAutoRedirect = true };
            httpRequest.AddHeader("cookie", Form1.cookie);
            httpRequest.AddHeader("user-agent", user_agent);
            List<string> linkDowload = new List<string>();

            try
            {
                var repon = httpRequest.Get(linkProduct).ToString();
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(repon);
                var tag = doc.DocumentNode.SelectNodes("//*[@id=\"js-show-modal-when-dissapear\"]/div/div/a");
                var tag2 = doc.DocumentNode.SelectNodes("//*[@id=\"js-show-modal-when-dissapear\"]/div/a");
                //var tag2 = doc.DocumentNode.SelectNodes("//*[@id=\"js-show-modal-when-dissapear\"]/div/a").ToList();
                if(tag2 != null)
                {
                    foreach (var item in tag2)
                    {
                        string linkTemp = item.Attributes["href"].Value;
                        linkDowload.Add(linkTemp);
                    }
                }
                if (tag != null)
                {
                    foreach (var item in tag)
                    {
                        string linkTemp = item.Attributes["href"].Value;
                        linkDowload.Add(linkTemp);
                    }
                }
            }
            catch (Exception)
            {
            }
            httpRequest.Close();
            return linkDowload;
        }

        public void downloadFileCreateFolder()
        {
            List<string> linkDownload = getLinkDowload(linkProduct);
            bool checkCreateFolder = false;

            for (int i =0 ; i< linkDownload.Count; i++)
            {
                try
                {
                    xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest();
                    httpRequest.AddHeader("cookie", Form1.cookie);
                    httpRequest.AddHeader("user-agent", user_agent);

                    var resp = httpRequest.Get(linkDownload[i]);
                    if (resp.ContentType.Equals("text/html"))
                    {
                        downloadEvent("Captcha");
                        return;
                    }
                    string folder = Form1.filePathFolderSave + @"\" + fileCode;
                    if (!checkCreateFolder && !Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                        checkCreateFolder = true;
                    }

                    string fileZip = Form1.filePathFolderSave + @"\" + fileCode + @"\" + fileCode +"_" + i + ".zip";
                    resp.ToFile(fileZip);

                    resp = httpRequest.Get(wtm);
                    resp.ToFile(Form1.filePathFolderSave + @"\" + fileCode + @"\" + fileCode + getDuoi(wtm));

                    httpRequest.Close();
                    if(i == linkDownload.Count-1)
                        downloadEvent("done");
                }
                catch (Exception)
                {
                    downloadEvent("false");
                }
            }            
        }

        public void downloadFileNoFolder()
        {
            List<string> linkDownload = getLinkDowload(linkProduct);
            bool checkCreateFolder = false;

            for (int i = 0; i < linkDownload.Count; i++)
            {
                try
                {
                    xNetStandard.HttpRequest httpRequest = new xNetStandard.HttpRequest();
                    httpRequest.AddHeader("cookie", Form1.cookie);
                    httpRequest.AddHeader("user-agent", user_agent);

                    var resp = httpRequest.Get(linkDownload[i]);
                    if (resp.ContentType.Equals("text/html"))
                    {
                        downloadEvent("Captcha");
                        return;
                    }
                    string folder = Form1.filePathFolderSave + @"\" + fileCode;
                    if (!checkCreateFolder && linkDownload.Count > 1)
                    {
                        Directory.CreateDirectory(folder);
                        checkCreateFolder = true;
                    }

                    string fileZip = "";
                    if (linkDownload.Count > 1)
                    {
                        fileZip = Form1.filePathFolderSave + @"\" + fileCode + @"\" + fileCode + "_" + i + ".zip";
                        
                    }else
                        fileZip = Form1.filePathFolderSave + @"\" + fileCode + "_" + i + ".zip";
                    resp.ToFile(fileZip);
                    httpRequest.Close();
                    if (i == linkDownload.Count - 1)
                        downloadEvent("done");
                }
                catch (Exception)
                {
                    downloadEvent("false");
                }
            }
        }

        private string getDuoi(string link)
        {
            return "."+link.Split('.')[3];
        }
    }
}
