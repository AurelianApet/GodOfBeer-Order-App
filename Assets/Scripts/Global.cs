using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;
using System.Linq;

public struct Table_Info
{
    public string id;
    public string name;
}

public struct Set_Info
{
    public string bus_id;
    public string soft_key;
    public Table_Info table_no;
    public bool is_client_call;
    public List<Table_Info> tablenolist;
    public int is_used_sftkey;//
    public string[] paths;//
    public int slide_option;//
}

public struct CategoryInfo
{
    public string id;
    public string name;
    public List<MenuInfo> menulist;
}

public struct MenuInfo
{
    public string image;
    public string name;
    public string desc;
    public int price;
    public string id;
    public bool is_soldout;
    public bool is_takeout;
    public int product_type;
}

public struct MyOrderInfo
{
    public List<OrderCartInfo> ordercartinfo;
    public int orderNo;
    public int total_price;
}

public struct OrderCartInfo
{
    public string id;
    public string menu_id;
    public string image;
    public string name;
    public string desc;
    public string priceUnit;
    public int price;
    public int amount;
    public int status;
    public int product_type;
    public string order_time;
}

public class CurrencyCodeMapper
{
    private static readonly Dictionary<string, string> SymbolsByCode;

    public static string GetSymbol(string code) { return SymbolsByCode[code]; }

    static CurrencyCodeMapper()
    {
        SymbolsByCode = new Dictionary<string, string>();

        var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                      .Select(x => new RegionInfo(x.LCID));

        foreach (var region in regions)
            if (!SymbolsByCode.ContainsKey(region.ISOCurrencySymbol))
                SymbolsByCode.Add(region.ISOCurrencySymbol, region.CurrencySymbol);
    }
}

public class Global
{
    //setting information
    public static Set_Info setInfo = new Set_Info();
    //screen manager
    public static float no_action_detection_time = 30f;
    public static string lastScene = "";

    //image download path
    public static string imgPath = "";
    public static string prePath = "";

    //price setting
    public static string priceUnit = CurrencyCodeMapper.GetSymbol("KRW");
    public static int ordercart_totalprice = 0;

    //menu list
    public static string curSelectedCategoryId = "";
    public static float scroll_height = 0f;

    //mycart information
    public static List<OrderCartInfo> mycartlist = new List<OrderCartInfo>();
    public static List<CategoryInfo> categorylist = new List<CategoryInfo>();
    public static List<MyOrderInfo> myorderlist = new List<MyOrderInfo>();

    //order list
    public static int is_market = 0;//
    public static MenuInfo cur_selected_menu = new MenuInfo();//

    //api
    public static string server_address = "";
    public static string api_server_port = "3006";
    public static string api_url = "";
    static string api_prefix = "m-api/user/";

    public static string set_info_api = api_prefix + "set-info";
    public static string check_softkey_api = api_prefix + "check-softkey";
    public static string verify_api = api_prefix + "verify";
    public static string get_categorylist_api = api_prefix + "get-categorylist";
    public static string order_api = api_prefix + "order";
    public static string get_myorderlist_api = api_prefix + "get-myorderlist";
    public static string call_client_api = api_prefix + "call-client";
    public static string check_tableinfo_api = api_prefix + "check-tableinfo";

    //socket server
    public static string socket_server = "";

    public static void updateOrderCartInfo(OrderCartInfo cinfo)
    {
        for (int i = 0; i < mycartlist.Count; i++)
        {
            if (mycartlist[i].menu_id == cinfo.menu_id)
                mycartlist[i] = cinfo;
        }
    }

    public static void removeOneCartItem(string menuId)
    {
        for (int i = 0; i < mycartlist.Count; i++)
        {
            if (mycartlist[i].menu_id == menuId)
            {
                OrderCartInfo cinfo = mycartlist[i];
                if (cinfo.amount > 1)
                {
                    cinfo.amount--;
                    mycartlist[i] = cinfo;
                }
                else
                {
                    mycartlist.Remove(mycartlist[i]);
                }
                ordercart_totalprice -= cinfo.price;
                break;
            }
        }
    }

    public static void trashCartItem(string menuId)
    {
        for (int i = 0; i < mycartlist.Count; i++)
        {
            if (mycartlist[i].menu_id == menuId)
            {
                ordercart_totalprice -= mycartlist[i].price * mycartlist[i].amount;
                mycartlist.Remove(mycartlist[i]);
                break;
            }
        }
    }

    public static void addOneCartItem(OrderCartInfo cinfo)
    {
        bool is_existing = false;
        for (int i = 0; i < mycartlist.Count; i++)
        {
            if (mycartlist[i].menu_id == cinfo.menu_id)
            {
                is_existing = true;
                cinfo.amount = mycartlist[i].amount + 1;
                mycartlist[i] = cinfo; break;
            }
        }
        if (!is_existing)
        {
            mycartlist.Add(cinfo);
        }
        ordercart_totalprice += cinfo.price;
    }

    public static void addCartItem(OrderCartInfo cinfo, int amount)
    {
        bool is_existing = false;
        for (int i = 0; i < mycartlist.Count; i++)
        {
            if (mycartlist[i].menu_id == cinfo.menu_id)
            {
                is_existing = true;
                cinfo.amount = mycartlist[i].amount + amount;
                mycartlist[i] = cinfo; break;
            }
        }
        if (!is_existing)
        {
            cinfo.amount = amount;
            mycartlist.Add(cinfo);
        }
        ordercart_totalprice += cinfo.price * amount;
    }

    public static string GetShowSubInfoFormat(string str)
    {
        if (str.Length > 11)
        {
            str = str.Substring(0, 10) + "...";
        }
        return str;
    }

    public static string GetShowPassFormat(string str)
    {
        string passStr = "";
        for (int i = 0; i < str.Length - 4; i++)
        {
            passStr += "*";
        }
        passStr += str.Substring(str.Length - 4);
        return passStr;
    }

    public static string GetPriceFormat(float price)
    {
        return priceUnit + string.Format("{0:N0}", price);
    }

    public static string GetOrderTimeFormat(string ordertime)
    {
        try
        {
            return string.Format("{0:D2}", Convert.ToDateTime(ordertime).Hour) + ":" + string.Format("{0:D2}", Convert.ToDateTime(ordertime).Minute);
        }
        catch (Exception ex)
        {
            return "";
        }
    }

    public static string GetONoFormat(int ono)
    {
        return string.Format("{0:D3}", ono);
    }
}


