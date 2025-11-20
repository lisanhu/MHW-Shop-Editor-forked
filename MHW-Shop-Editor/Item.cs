using System;

namespace MHWShopEditor
{
    public class Item
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public int Hex 
        { 
            get 
            {
                if (Key.Length > 4)
                    return Convert.ToInt32(this.Key.Substring(4), 16); 
                return 0;
            } 
        }
        public override string ToString()
        {
            return Value;
        }
    }
}
