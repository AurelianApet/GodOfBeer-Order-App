using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SocketIO;
using SimpleJSON;
using System.IO;

public class MenuDetailManager : MonoBehaviour
{
    float time = 0f;
    public GameObject client_popup;
    public Image menuImg;
    public Text price;
    public Text menuTitle;
    public Text menuDesc;
    public Text priceUnit;
    public Text amount;
    public GameObject complete_popup;
    public GameObject socketPrefab;
    public GameObject err_popup;
    public Text err_msg;
    GameObject socketObj;
    SocketIOComponent socket;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
        if (!Global.setInfo.is_client_call)
        {
            GameObject.Find("call").SetActive(false);
        }
        else
        {
            GameObject.Find("call").SetActive(true);
        }
        LoadInfo();
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("recept_call_process", recept_order_process);
        socket.On("recept_order_process", recept_order_process);
        socket.On("error", socketError);
        socket.On("close", socketClose);
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
        is_socket_open = false;
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
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

    void LoadInfo()
    {
        if(Global.cur_selected_menu.image != "")
        {
            StartCoroutine(downloadImage(Global.cur_selected_menu.image, Global.imgPath + Path.GetFileName(Global.cur_selected_menu.image), menuImg.gameObject));
            //StartCoroutine(LoadImage(Global.cur_selected_menu.image, menuImg.gameObject));
        }
        menuTitle.text = Global.cur_selected_menu.name;
        menuDesc.text = Global.cur_selected_menu.desc;
        price.text = Global.GetPriceFormat(Global.cur_selected_menu.price);
    }

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

    //IEnumerator LoadImage(string menu_image, GameObject imageobj)
    //{
    //    Image img = imageobj.GetComponent<Image>();
    //    Debug.Log(menu_image);
    //    WWW www = new WWW(menu_image);
    //    yield return www;
    //    try
    //    {
    //        img.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
    //        //imageobj.GetComponent<ImageWithRoundedCorners>().radius = 30;
    //    }
    //    catch (Exception ex)
    //    {
    //        if (img != null)
    //        {
    //            img.sprite = null;
    //        }
    //    }
    //}

    public void onminusBtn()
    {
        try
        {
            int cnt = int.Parse(amount.text);
            if(cnt > 1)
            {
                amount.text = (cnt - 1).ToString();
            }

        }catch(Exception ex)
        {
            amount.text = "1";
        }
    }

    public void onplusBtn()
    {
        try
        {
            int cnt = int.Parse(amount.text);
            amount.text = (cnt + 1).ToString();
        }
        catch (Exception ex)
        {
            amount.text = "1";
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

    public void onBackBtn()
    {
        WWWForm form = new WWWForm();
        form.AddField("type", Global.is_market);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    public void onMyorder()
    {
        //주문내역
        StartCoroutine(GotoScene("my_orderlist"));
    }

    public void onHome()
    {
        Global.mycartlist.Clear();
        Global.ordercart_totalprice = 0;
        Global.curSelectedCategoryId = "";
        Global.scroll_height = 0f;
        StartCoroutine(GotoScene("auto_slide"));
    }

    public void onCallClient()
    {
        if (Global.setInfo.is_client_call)
        {
            //주방앱에 호출신호
            WWWForm form = new WWWForm();
            form.AddField("table_name", Global.setInfo.table_no.name);
            WWW www = new WWW(Global.api_url + Global.call_client_api, form);
            StartCoroutine(ProcessCallClient(www));
        }
        else
        {
            err_msg.text = "직원호출기능이 설정되지 않았습니다.";
            err_popup.SetActive(true);
        }
    }

    IEnumerator ProcessCallClient(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            client_popup.SetActive(true);
        }
    }

    public void onConfirmClientPopup()
    {
        client_popup.SetActive(false);
    }

    public void justOrder()
    {
        onaddCart();
        StartCoroutine(GotoScene("order_cart"));
    }

    public void addCart()
    {
        onaddCart();
        WWWForm form = new WWWForm();
        form.AddField("type", Global.is_market.ToString());
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    public void onaddCart()
    {
        int cnt = 1;
        try
        {
            cnt = int.Parse(amount.text);
        }
        catch (Exception ex)
        {

        }
        OrderCartInfo cinfo = new OrderCartInfo();
        cinfo.menu_id = Global.cur_selected_menu.id;
        cinfo.name = Global.cur_selected_menu.name;
        cinfo.price = Global.cur_selected_menu.price;
        cinfo.desc = Global.cur_selected_menu.desc;
        cinfo.image = Global.cur_selected_menu.image;
        cinfo.status = 0;//주문전
        Global.addCartItem(cinfo, cnt);
    }

    public void onCompletePopup()
    {
        complete_popup.SetActive(false);
    }

    public void OnConfirmErrPopup()
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
