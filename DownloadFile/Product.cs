using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadFile
{
    class Product
    {
        private string Name;
        private string type;
        private string tags;
        private string price;
        private string category;
        private string description;
        private string linkProduct;
        private string linkDowload;

        public string getName() { return Name; }
        public string getType() { return type; }
        public string getTags() { return tags; }
        public string getCategory() { return category ; }
        public string getDescription() { return description; }
        public string getLinkProduct() { return linkProduct; }
        public string getLinkDowload() { return linkDowload; }
        public string getPrice() { return price; }

        public Product(string name, string type, string tags,string price, string category, string description, string linkProduct, string linkDowload)
        {
            Name = name;
            this.type = type;
            this.tags = tags;
            this.price = price;
            this.description = description;
            this.linkProduct = linkProduct;
            this.linkDowload = linkDowload;
            this.category = category;
        }
    }
}
