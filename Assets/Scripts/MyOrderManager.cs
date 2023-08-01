using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SocketIO;
using System.IO;

public class MyOrderManager : MonoBehaviour
{
    public GameObject order_item;
    public GameObject order_parent;
    public Text total_price;
    public GameObject complete_popup;
    public Text price;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    GameObject[] m_orderList;
    float time = 0f;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
        StartCoroutine(LoadOrderList());
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

    public void onBack()
    {
        if (Global.lastScene != "")
        {
            StartCoroutine(GotoScene(Global.lastScene));
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("type", Global.is_market.ToString());
            WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
            StartCoroutine(GetCategorylistFromApi(www));
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

    //IEnumerator LoadImage(string menu_image, GameObject imgObj)
    //{
    //    Image img = imgObj.GetComponent<Image>();
    //    WWW www = new WWW(menu_image);
    //    yield return www;
    //    try
    //    {
    //        img.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
    //        //imgObj.GetComponent<ImageWithRoundedCorners>().radius = 30;
    //    }catch(Exception ex)
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

    IEnumerator LoadOrderList()
    {
        while (order_parent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(order_parent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {

            }
            yield return new WaitForSeconds(0.001f);
        }

        Global.myorderlist.Clear();
        price.text = "";
        m_orderList = null;

        //주문정보 가져오기 api
        WWWForm form = new WWWForm();
        form.AddField("table_id", Global.setInfo.table_no.id);
        WWW www = new WWW(Global.api_url + Global.get_myorderlist_api, form);
        StartCoroutine(GetMyorderlistFromApi(www));
    }

    IEnumerator GetMyorderlistFromApi(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode molist = JSON.Parse(jsonNode["myorderlist"].ToString()/*.Replace("\"", "")*/);
            float total_price = 0f;

            int totalCnt = 0;
            for (int i = 0; i < molist.Count; i++)
            {
                MyOrderInfo minfo = new MyOrderInfo();
                minfo.orderNo = molist[i]["orderSeq"].AsInt;
                minfo.total_price = molist[i]["total_price"].AsInt;
                total_price += molist[i]["total_price"].AsInt;
                JSONNode menulist = JSON.Parse(molist[i]["menulist"].ToString());
                List<OrderCartInfo> ocinfolist = new List<OrderCartInfo>();
                for (int j = 0; j < menulist.Count; j++)
                {
                    OrderCartInfo ocinfo = new OrderCartInfo();
                    ocinfo.amount = menulist[j]["quantity"].AsInt;
                    ocinfo.price = menulist[j]["price_unit"].AsInt;
                    ocinfo.menu_id = menulist[j]["menu_id"];
                    ocinfo.name = menulist[j]["name"];
                    ocinfo.order_time = menulist[j]["reg_datetime"];
                    ocinfo.status = menulist[j]["status"].AsInt;
                    if(menulist[j]["content"] == null)
                    {
                        ocinfo.desc = "";
                    }
                    else
                    {
                        ocinfo.desc = menulist[j]["content"];
                    }
                    ocinfo.image = menulist[j]["image"];
                    ocinfo.id = menulist[j]["order_id"];
                    ocinfolist.Add(ocinfo);

                    totalCnt++;
                }
                minfo.ordercartinfo = ocinfolist;
                Global.myorderlist.Add(minfo);
            }
            price.text = Global.GetPriceFormat(total_price);
            m_orderList = new GameObject[totalCnt];
            totalCnt = 0;
            for (int i = 0; i < molist.Count; i++)
            {
                JSONNode menulist = JSON.Parse(molist[i]["menulist"].ToString());
                for (int j = 0; j < menulist.Count; j++)
                {
                    //UI
                    m_orderList[totalCnt] = Instantiate(order_item);
                    m_orderList[totalCnt].transform.SetParent(order_parent.transform);

                    try
                    {
                        if(menulist[j]["image"] != "")
                        {
                            //StartCoroutine(LoadImage(menulist[j]["image"], m_orderList[totalCnt].transform.Find("Image").gameObject));
                            StartCoroutine(downloadImage(menulist[j]["image"], Global.imgPath + Path.GetFileName(menulist[j]["image"]), m_orderList[totalCnt].transform.Find("Image").gameObject));
                        }
                        m_orderList[totalCnt].transform.Find("no").GetComponent<Text>().text = menulist[j]["menu_id"];
                        m_orderList[totalCnt].transform.Find("title/name").GetComponent<Text>().text = menulist[j]["name"];
                        if(menulist[j]["content"] != null)
                        {
                            m_orderList[totalCnt].transform.Find("title/subinfo").GetComponent<Text>().text = Global.GetShowSubInfoFormat(menulist[j]["content"]);
                        }
                        m_orderList[totalCnt].transform.Find("amount").GetComponent<Text>().text = menulist[j]["quantity"].ToString();
                        m_orderList[totalCnt].transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(menulist[j]["price"]);
                        m_orderList[totalCnt].transform.Find("time").GetComponent<Text>().text = Global.GetOrderTimeFormat(menulist[j]["reg_datetime"]);
                        if (menulist[j]["status"] == 3)
                        {
                            m_orderList[totalCnt].transform.Find("check").GetComponent<Image>().sprite = Resources.Load<Sprite>("check");
                        }
                        else
                        {
                            m_orderList[totalCnt].transform.Find("check").GetComponent<Image>().sprite = Resources.Load<Sprite>("check1");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message);
                    }
                    totalCnt++;
                }
            }
        }
    }

    public void onConfirm()
    {
        Global.lastScene = "my_orderlist";
        WWWForm form = new WWWForm();
        form.AddField("type", Global.is_market.ToString());
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    public void onCompletePopup()
    {
        StartCoroutine(GotoScene("my_orderlist"));
        //complete_popup.SetActive(false);
    }

    public void OnApplicationQuit()
    {
        socket.Close();
        socket.OnDestroy();
        socket.OnApplicationQuit();
    }
}
