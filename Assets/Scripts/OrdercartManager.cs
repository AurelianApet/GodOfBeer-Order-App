using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SimpleJSON;
using SocketIO;
using System.IO;

public class OrdercartManager : MonoBehaviour
{
    public GameObject cart_item;
    public GameObject cart_parent;
    public Text total_price;
    public GameObject order_popup;
    public GameObject complete_popup;
    public GameObject err_popup;
    public Text err_msg;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    GameObject[] m_CartList;
    float time = 0f;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
        StartCoroutine(LoadCartList());
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("recept_call_process", recept_order_process);
        socket.On("recept_order_process", recept_order_process);
        socket.On("error", socketError);
        socket.On("close", socketClose);
    }

    public void socketOpen(SocketIOEvent e)
    {
        if (is_socket_open)
            return;
        is_socket_open = true;
        int is_client_call = 0;
        if (Global.setInfo.is_client_call)
            is_client_call = 1;
        string pubid = "";
        pubid = "{\"table_id\":\"" + Global.setInfo.table_no.id + "\",\"is_client_call\":\"" + is_client_call + "\"}";
        Debug.Log(pubid);
        socket.Emit("userSetInfo", JSONObject.Create(pubid));
        Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
    }

    public void recept_order_process(SocketIOEvent e)
    {
        JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
        Debug.Log(jsonNode);
        int orderNo = int.Parse(jsonNode["orderNo"]);
        JSONNode mlist = JSON.Parse(jsonNode["menulist"].ToString());
        string menuname = "";
        string menuamount = "";
        for (int i = 0; i < mlist.Count; i++)
        {
            if (i != mlist.Count - 1)
            {
                menuname += mlist[i]["name"] + "\n";
                menuamount += mlist[i]["amount"] + "\n";
            }
            else
            {
                menuname += mlist[i]["name"];
                menuamount += mlist[i]["amount"];
            }
        }
        complete_popup.transform.Find("No").GetComponent<Text>().text = Global.GetONoFormat(orderNo);
        complete_popup.transform.Find("menu_name").GetComponent<Text>().text = menuname;
        complete_popup.transform.Find("menu_amount").GetComponent<Text>().text = menuamount;
        complete_popup.SetActive(true);
        GameObject.Find("Audio Source").GetComponent<AudioSource>().Play();
    }

    public void socketError(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    public void socketClose(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
        is_socket_open = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        if (!Input.anyKey)
        {
            time += Time.deltaTime;
        }
        else
        {
            if (time != 0f)
            {
                GameObject.Find("touch").GetComponent<AudioSource>().Play();
                time = 0f;
            }
        }

        if (!complete_popup.activeSelf && time >= Global.no_action_detection_time)
        {
            Global.mycartlist.Clear();
            Global.ordercart_totalprice = 0;
            Global.curSelectedCategoryId = "";
            Global.scroll_height = 0f;
            StartCoroutine(GotoScene("auto_slide"));
        }
    }

    IEnumerator GetCategorylistFromApi(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            Global.categorylist.Clear();
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode c_list = JSON.Parse(jsonNode["categorylist"].ToString()/*.Replace("\"", "")*/);
            int totalCategoryCnt = c_list.Count;
            for (int i = 0; i < totalCategoryCnt; i++)
            {
                CategoryInfo cateInfo = new CategoryInfo();
                try
                {
                    cateInfo.name = c_list[i]["name"];
                    cateInfo.id = c_list[i]["id"];
                }
                catch (Exception ex)
                {

                }
                cateInfo.menulist = new List<MenuInfo>();
                JSONNode m_list = JSON.Parse(c_list[i]["menulist"].ToString()/*.Replace("\"", "")*/);
                int menuCnt = m_list.Count;
                for (int j = 0; j < menuCnt; j++)
                {
                    MenuInfo minfo = new MenuInfo();
                    try
                    {
                        minfo.name = m_list[j]["name"];
                        minfo.image = m_list[j]["img_path"];
                        minfo.desc = m_list[j]["content"];
                        if (m_list[j]["content"] == null)
                        {
                            minfo.desc = "";
                        }
                        minfo.id = m_list[j]["id"];
                        minfo.price = m_list[j]["price"].AsInt;
                        if (m_list[j]["is_takeout"].AsInt == 1)
                        {
                            minfo.is_takeout = true;
                        }
                        else
                        {
                            minfo.is_takeout = false;
                        }

                        if (m_list[j]["is_soldout"].AsInt == 1)
                        {
                            minfo.is_soldout = true;
                        }
                        else
                        {
                            minfo.is_soldout = false;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    cateInfo.menulist.Add(minfo);
                }
                Global.categorylist.Add(cateInfo);
            }
            StartCoroutine(GotoScene("menu_list"));
        }
        else
        {
            Debug.Log(www.error);
            StartCoroutine(GotoScene("menu_list"));
        }
    }

    public void onBack()
    {
        WWWForm form = new WWWForm();
        form.AddField("type", Global.is_market.ToString());
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    public void allDelete()
    {
        Global.mycartlist.Clear();
        Global.ordercart_totalprice = 0;
        total_price.text = "₩0";
        StartCoroutine(ClearList());
    }

    IEnumerator ClearList()
    {
        Debug.Log("clear order cart list--");
        while (cart_parent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(cart_parent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {

            }
            yield return new WaitForSeconds(0.001f);
        }
    }

    IEnumerator LoadCartList()
    {
        while (cart_parent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(cart_parent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {

            }
            yield return new WaitForSeconds(0.001f);
        }
        Debug.Log("load order cart list.");
        m_CartList = new GameObject[Global.mycartlist.Count];
        for (int i = 0; i < Global.mycartlist.Count; i++)
        {
            m_CartList[i] = Instantiate(cart_item);
            m_CartList[i].transform.SetParent(cart_parent.transform);
            try
            {
                if(Global.mycartlist[i].image != "")
                {
                    //StartCoroutine(LoadImage(Global.mycartlist[i].image, m_CartList[i].transform.Find("Image").gameObject));
                    StartCoroutine(downloadImage(Global.mycartlist[i].image, Global.imgPath + Path.GetFileName(Global.mycartlist[i].image), m_CartList[i].transform.Find("Image").gameObject));
                }
                m_CartList[i].transform.Find("Image").GetComponent<ImageWithRoundedCorners>().radius = 30;
                m_CartList[i].transform.Find("no").GetComponent<Text>().text = Global.mycartlist[i].menu_id.ToString();
                Text amount = m_CartList[i].transform.Find("count/amount").GetComponent<Text>();
                amount.text = Global.mycartlist[i].amount.ToString();
                m_CartList[i].transform.Find("title/name").GetComponent<Text>().text = Global.mycartlist[i].name;
                if(Global.mycartlist[i].desc != null && Global.mycartlist[i].desc != "")
                {
                    m_CartList[i].transform.Find("title/subinfo").GetComponent<Text>().text = Global.GetShowSubInfoFormat(Global.mycartlist[i].desc);
                }
                Text price = m_CartList[i].transform.Find("price").GetComponent<Text>();
                Debug.Log("price = " + Global.mycartlist[i].price);
                Debug.Log("amount = " + Global.mycartlist[i].amount);
                price.text = Global.GetPriceFormat(Global.mycartlist[i].price * Global.mycartlist[i].amount);
                float unit_price = Global.mycartlist[i].price;
                OrderCartInfo cinfo = Global.mycartlist[i];
                m_CartList[i].transform.Find("count/minus").GetComponent<Button>().onClick.AddListener(delegate () { onMinusBtn(unit_price, price, amount, cinfo); });
                m_CartList[i].transform.Find("count/plus").GetComponent<Button>().onClick.AddListener(delegate () { onPlusBtn(unit_price, price, amount, cinfo); });
                m_CartList[i].transform.Find("trash").GetComponent<Button>().onClick.AddListener(delegate () { onTrash(cinfo.menu_id); });
            }
            catch (Exception ex)
            {

            }
        }
        total_price.text = Global.GetPriceFormat(Global.ordercart_totalprice);
    }

    //IEnumerator LoadImage(string menu_image, GameObject imgObj)
    //{
    //    Image img = imgObj.GetComponent<Image>();
    //    WWW www = new WWW(menu_image);
    //    yield return www;
    //    try
    //    {
    //        img.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
    //        //imgObj.GetComponent<ImageWithRoundedCorners>().radius = 30;
    //    }
    //    catch (Exception ex)
    //    {
    //        if (img != null)
    //        {
    //            img.sprite = null;
    //        }
    //    }
    //}

    IEnumerator downloadImage(string url, string pathToSaveImage, GameObject imgObj)
    {
        yield return new WaitForSeconds(0.1f);
        try
        {
            Image img = imgObj.GetComponent<Image>();
            if (File.Exists(pathToSaveImage))
            {
                Debug.Log(pathToSaveImage + " exists");
                StartCoroutine(LoadPictureToTexture(pathToSaveImage, img));
            }
            else
            {
                Debug.Log(pathToSaveImage + " downloading--");
                WWW www = new WWW(url);
                StartCoroutine(_downloadImage(www, pathToSaveImage, img));
            }
        }
        catch(Exception ex)
        {

        }
    }

    private IEnumerator _downloadImage(WWW www, string savePath, Image img)
    {
        yield return www;
        //Check if we failed to send
        if (string.IsNullOrEmpty(www.error))
        {
            saveImage(savePath, www.bytes, img);
        }
        else
        {
            UnityEngine.Debug.Log("Error: " + www.error);
        }
    }

    void saveImage(string path, byte[] imageBytes, Image img)
    {
        try
        {
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllBytes(path, imageBytes);
            Debug.Log("Saved Data to: " + path.Replace("/", "\\"));
            StartCoroutine(LoadPictureToTexture(path, img));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
            Debug.LogWarning("Error: " + e.Message);
        }
    }

    IEnumerator LoadPictureToTexture(string name, Image img)
    {
        Debug.Log("load image = " + Global.prePath + name);
        WWW pictureWWW = new WWW(Global.prePath + name);
        yield return pictureWWW;

        if (img != null)
        {
            img.sprite = Sprite.Create(pictureWWW.texture, new Rect(0, 0, pictureWWW.texture.width, pictureWWW.texture.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
        }
    }


    public void onMinusBtn(float unit_price, Text price, Text amount, OrderCartInfo cinfo)
    {
        Debug.Log("minus count = " + cinfo.menu_id);
        int cnt = 1;
        try
        {
            cnt = int.Parse(amount.text);
        }
        catch (Exception ex)
        {
        }
        if (cnt > 1)
        {
            cnt--;
            Global.removeOneCartItem(cinfo.menu_id);
        }
        amount.text = cnt.ToString();
        price.text = Global.GetPriceFormat(unit_price * cnt);
        total_price.text = Global.GetPriceFormat(Global.ordercart_totalprice);
    }

    public void onPlusBtn(float unit_price, Text price, Text amount, OrderCartInfo cinfo)
    {
        Debug.Log("plus count = " + cinfo.menu_id);
        int cnt = 1;
        try
        {
            cnt = int.Parse(amount.text);
        }
        catch (Exception ex)
        {
        }
        cnt++;
        amount.text = cnt.ToString();
        Global.addOneCartItem(cinfo);
        price.text = Global.GetPriceFormat(unit_price * cnt);
        total_price.text = Global.GetPriceFormat(Global.ordercart_totalprice);
    }

    public void onTrash(string menuId)
    {
        //UI상에서 제거
        for (int i = 0; i < m_CartList.Length; i++)
        {
            try
            {
                if (m_CartList[i].transform.Find("no").GetComponent<Text>().text == menuId.ToString())
                {
                    DestroyImmediate(m_CartList[i].gameObject);
                    break;
                }
            }
            catch (Exception ex)
            {

            }
        }
        //Global에서 제거
        Global.trashCartItem(menuId);
        total_price.text = Global.GetPriceFormat(Global.ordercart_totalprice);
    }

    public void onOrder()
    {
        //주문 api
        WWWForm form = new WWWForm();
        form.AddField("table_id", Global.setInfo.table_no.id);
        form.AddField("table_name", Global.setInfo.table_no.name);
        form.AddField("order_type", Global.is_market);
        string oinfo = "[";
        for (int i = 0; i < Global.mycartlist.Count; i++)
        {
            if (i == 0)
            {
                oinfo += "{";
            }
            else
            {
                oinfo += ",{";
            }
            oinfo += "\"menu_id\":\"" + Global.mycartlist[i].menu_id + "\","
                + "\"menu_name\":\"" + Global.mycartlist[i].name + "\","
                + "\"price\":" + Global.mycartlist[i].price + ","
                + "\"quantity\":" + Global.mycartlist[i].amount + "}";
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("order_info", oinfo);
        WWW www = new WWW(Global.api_url + Global.order_api, form);
        StartCoroutine(ProcessOrder(www));

    }

    IEnumerator ProcessOrder(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                //성공이면 Global에 추가
                //MyOrderInfo myorderinfo = new MyOrderInfo();
                //myorderinfo.ordercartinfo = Global.mycartlist;
                //myorderinfo.total_price = Global.ordercart_totalprice;
                //Global.myorderlist.Add(myorderinfo);
                Global.mycartlist.Clear();
                order_popup.SetActive(true);
                yield return new WaitForSeconds(30f);
                Global.curSelectedCategoryId = "";
                Global.scroll_height = 0f;
                StartCoroutine(GotoScene("auto_slide"));
            }
            else
            {
                err_msg.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
    }

    IEnumerator GotoScene(string sceneName)
    {
        if(socket != null)
        {
            socket.Close();
            socket.OnDestroy();
            socket.OnApplicationQuit();
        }
        if(socketObj != null)
        {
            DestroyImmediate(socketObj);
        }
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(sceneName);
    }

    public void onConfirmPopup()
    {
        order_popup.SetActive(false);
        Global.mycartlist.Clear();
        Global.ordercart_totalprice = 0;
        Global.curSelectedCategoryId = "";
        Global.scroll_height = 0f;
        StartCoroutine(GotoScene("auto_slide"));
    }

    public void onConfirmCompletePopup()
    {
        complete_popup.SetActive(false);
    }

    public void onConfirmErrPopup()
    {
        err_popup.SetActive(false);
    }

    public void OnApplicationQuit()
    {
        socket.Close();
        socket.OnDestroy();
        socket.OnApplicationQuit();
    }
}
