using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SocketIO;

public class MenuListManager : MonoBehaviour
{
    float time = 0f;
    public GameObject menuItem;
    public GameObject categoryItem;
    public GameObject selectedItem;
    public GameObject menuItemParent;
    public GameObject categoryItemParent;
    public GameObject selectedItemParent;
    public GameObject client_popup;
    public Text totalprice;
    public GameObject complete_popup;

    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;
    public GameObject err_popup;
    public Text err_msg;

    GameObject[] m_MenuItem;
    GameObject[] m_categoryItem;
    List<GameObject> m_SelItem = new List<GameObject>();
    bool loading = false;
    bool is_socket_open = false;
    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
        if (!Global.setInfo.is_client_call)
        {
            GameObject.Find("top/call_client").SetActive(false);
        }
        else
        {
            GameObject.Find("top/call_client").SetActive(true);
        }
        LoadCategoryList();
        LoadCartList();
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
        yield return new WaitForFixedUpdate();
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

    void LoadCartList()
    {
        //이미 잇는 주문카트내역을 조회해서 로드.
        Global.ordercart_totalprice = 0;
        for (int i = 0; i < Global.mycartlist.Count; i++)
        {
            for (int j = 0; j < Global.mycartlist[i].amount; j++)
            {
                GameObject sItem = Instantiate(selectedItem);
                sItem.transform.SetParent(selectedItemParent.transform);
                try
                {
                    if (Global.mycartlist[i].image != "")
                    {
                        GameObject sobj = sItem.transform.Find("Image").gameObject;
                        StartCoroutine(downloadImage(Global.mycartlist[i].image, Global.imgPath + Path.GetFileName(Global.mycartlist[i].image), sobj));
                        //StartCoroutine(LoadImage(Global.mycartlist[i].image, sItem.transform.Find("Image").gameObject));
                    }
                    //sItem.transform.Find("id").GetComponent<Text>().text = Global.mycartlist[i].menuNo;
                    sItem.transform.Find("no").GetComponent<Text>().text = Global.mycartlist[i].menu_id.ToString();
                    string mNo = Global.mycartlist[i].menu_id;
                    sItem.transform.Find("minBtn").GetComponent<Button>().onClick.AddListener(delegate () { onRemoveItem(mNo); });
                    m_SelItem.Add(sItem);
                    Global.ordercart_totalprice += Global.mycartlist[i].price;
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
        }
        totalprice.text = Global.GetPriceFormat(Global.ordercart_totalprice);
    }

    void LoadCategoryList()
    {
        Debug.Log("load category list--" + Global.categorylist.Count);
        if (Global.curSelectedCategoryId == "" && Global.categorylist.Count > 0)
        {
            Global.curSelectedCategoryId = Global.categorylist[0].id;
        }
        Debug.Log("curSelectedNo = " + Global.curSelectedCategoryId);
        //UI에 추가
        m_categoryItem = new GameObject[Global.categorylist.Count];
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            m_categoryItem[i] = Instantiate(categoryItem);
            m_categoryItem[i].transform.SetParent(categoryItemParent.transform);
            try
            {
                //m_categoryItem[i].transform.Find("id").GetComponent<Text>().text = "";
                m_categoryItem[i].transform.Find("Name").GetComponent<Text>().text = Global.categorylist[i].name;
                m_categoryItem[i].transform.Find("no").GetComponent<Text>().text = Global.categorylist[i].id.ToString();
                string t_cateId = Global.categorylist[i].id;
                m_categoryItem[i].GetComponent<Button>().onClick.AddListener(delegate () { LoadMenuList(t_cateId); });
                if (Global.categorylist[i].id == Global.curSelectedCategoryId)
                {
                    List<MenuInfo> minfoList = new List<MenuInfo>();
                    m_categoryItem[i].transform.Find("Name").GetComponent<Text>().fontStyle = FontStyle.Bold;
                    minfoList = Global.categorylist[i].menulist;
                    int menuCnt = minfoList.Count;
                    Debug.Log("count = " + menuItemParent.transform.childCount);
                    m_MenuItem = new GameObject[menuCnt];
                    for (int j = 0; j < menuCnt; j++)
                    {
                        try
                        {
                            m_MenuItem[j] = Instantiate(menuItem);
                            m_MenuItem[j].transform.SetParent(menuItemParent.transform);
                            //m_MenuItem[i].transform.Find("id").GetComponent<Text>().text = "";
                            m_MenuItem[j].transform.Find("no").GetComponent<Text>().text = minfoList[j].id.ToString();
                            if (minfoList[j].image != "")
                            {
                                StartCoroutine(downloadImage(minfoList[j].image, Global.imgPath + Path.GetFileName(minfoList[j].image), m_MenuItem[j].transform.Find("Image").gameObject));
                                //StartCoroutine(LoadImage(menu_image, m_MenuItem[i].transform.Find("Image").gameObject));
                            }
                            m_MenuItem[j].transform.Find("name").GetComponent<Text>().text = minfoList[j].name;
                            m_MenuItem[j].transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(minfoList[j].price);

                            OrderCartInfo cinfo = new OrderCartInfo();
                            cinfo.name = minfoList[j].name;
                            cinfo.menu_id = minfoList[j].id;
                            cinfo.image = minfoList[j].image;
                            cinfo.desc = minfoList[j].desc;
                            cinfo.price = minfoList[j].price;
                            cinfo.amount = 1;
                            cinfo.status = 0;

                            if (Global.is_market == 1 && minfoList[j].is_takeout)
                            {
                                m_MenuItem[j].transform.Find("takeoutImg").gameObject.SetActive(true);
                            }
                            else if (minfoList[j].is_soldout)
                            {
                                m_MenuItem[j].transform.Find("soldoutImg").gameObject.SetActive(true);
                            }
                            else
                            {
                                m_MenuItem[j].transform.Find("plusBtn").GetComponent<Button>().onClick.AddListener(delegate () { addList(cinfo); });
                                m_MenuItem[j].GetComponent<Button>().onClick.AddListener(delegate () { onMenuDetail(cinfo); });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        Vector3 pos = categoryItemParent.transform.position;
        pos.y = Global.scroll_height;
        categoryItemParent.transform.position = pos;
        loading = true;
    }

    void LoadMenuList(string cateId)
    {
        if (!loading)
            return;
        Global.curSelectedCategoryId = cateId;
        Global.scroll_height = categoryItemParent.transform.position.y;
        StartCoroutine(GotoScene("menu_list"));
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
            StartCoroutine(GotoScene("auto_slide"));
        }
    }

    public void onOrderCart()
    {
        //주문카트
        StartCoroutine(GotoScene("order_cart"));
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
            if (Global.setInfo.is_client_call)
            {
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

    public void addList(OrderCartInfo cinfo)
    {
        //UI에 추가
        GameObject sItem = Instantiate(selectedItem);
        sItem.transform.SetParent(selectedItemParent.transform);
        try
        {
            if(cinfo.image != "")
            {
                //StartCoroutine(LoadImage(cinfo.image, sItem.transform.Find("Image").gameObject));
                StartCoroutine(downloadImage(cinfo.image, Global.imgPath + Path.GetFileName(cinfo.image), sItem.transform.Find("Image").gameObject));
            }
            sItem.transform.Find("no").GetComponent<Text>().text = cinfo.menu_id.ToString();
            sItem.transform.Find("minBtn").GetComponent<Button>().onClick.AddListener(delegate () { onRemoveItem(cinfo.menu_id); });
        }
        catch (Exception ex)
        {

        }
        m_SelItem.Add(sItem);

        //my cart list에 추가
        Global.addOneCartItem(cinfo);
        totalprice.text = Global.GetPriceFormat(Global.ordercart_totalprice);
    }

    public void onMenuDetail(OrderCartInfo cinfo)
    {
        Global.cur_selected_menu.image = cinfo.image;
        Global.cur_selected_menu.desc = cinfo.desc;
        Global.cur_selected_menu.id = cinfo.menu_id;
        Global.cur_selected_menu.name = cinfo.name;
        Global.cur_selected_menu.price = cinfo.price;
        Global.lastScene = "menu_list";
        StartCoroutine(GotoScene("menu_detail"));
    }

    public void onRemoveItem(string menuId)
    {
        //UI상에서 제거
        for (int i = 0; i < m_SelItem.Count; i++)
        {
            try
            {
                if (m_SelItem[i].transform.Find("no").GetComponent<Text>().text == menuId.ToString())
                {
                    DestroyImmediate(m_SelItem[i].gameObject);
                    m_SelItem.Remove(m_SelItem[i].gameObject);
                    break;
                }
            }
            catch (Exception ex)
            {

            }
        }
        //Global에서 제거
        Global.removeOneCartItem(menuId);
        totalprice.text = Global.GetPriceFormat(Global.ordercart_totalprice);
    }

    public void onCompletePopup()
    {
        complete_popup.SetActive(false);
    }

    public void OnConfirmErrPopup()
    {
        err_popup.SetActive(false);
    }

    IEnumerator downloadImage(string url, string pathToSaveImage, GameObject imgObj)
    {
        try
        {
            if (imgObj != null)
            {
                RawImage img = imgObj.GetComponent<RawImage>();
                if (File.Exists(pathToSaveImage))
                {
                    StartCoroutine(LoadPictureToTexture(pathToSaveImage, img));
                }
                else
                {
                    WWW www = new WWW(url);
                    StartCoroutine(_downloadImage(www, pathToSaveImage, img));
                }
            }
        } catch( Exception ex )
        {

        }
        yield return null;
    }

    private IEnumerator _downloadImage(WWW www, string savePath, RawImage img)
    {
        yield return www;
        if(img != null)
        {
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
    }

    void saveImage(string path, byte[] imageBytes, RawImage img)
    {
        try
        {
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllBytes(path, imageBytes);
            //Debug.Log("Download Image: " + path.Replace("/", "\\"));
            StartCoroutine(LoadPictureToTexture(path, img));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
            Debug.LogWarning("Error: " + e.Message);
        }
    }

    IEnumerator LoadPictureToTexture(string name, RawImage img)
    {
        //Texture2D t = new Texture2D(2, 2);
        //Debug.Log("load image = " + Global.prePath + name);
        WWW pictureWWW = new WWW(Global.prePath + name);
        //pictureWWW.LoadImageIntoTexture(t);
        //t.Compress(true);
        yield return pictureWWW;
        if(img != null)
        {
            //img.texture = t;
            img.texture = pictureWWW.texture;
            //img.sprite = Sprite.Create(pictureWWW.texture, new Rect(0, 0, pictureWWW.texture.width, pictureWWW.texture.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
        }
        yield return null;
    }

    public void OnApplicationQuit()
    {
        socket.Close();
        socket.OnDestroy();
        socket.OnApplicationQuit();
    }

    //byte[] loadImage(string path)
    //{
    //    byte[] dataByte = null;

    //    //Exit if Directory or File does not exist
    //    if (!Directory.Exists(Path.GetDirectoryName(path)))
    //    {
    //        Debug.LogWarning("Directory does not exist");
    //        return null;
    //    }

    //    if (!File.Exists(path))
    //    {
    //        Debug.Log("File does not exist");
    //        return null;
    //    }

    //    try
    //    {
    //        dataByte = File.ReadAllBytes(path);
    //        Debug.Log("Loaded Data from: " + path.Replace("/", "\\"));
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogWarning("Failed To Load Data from: " + path.Replace("/", "\\"));
    //        Debug.LogWarning("Error: " + e.Message);
    //    }

    //    return dataByte;
    //}

    //    public void LoadImage(string menu_image, Image img)
    //    {
    //        string savePath = "";
    //#if UNITY_IPHONE
    //		savePath = Application.persistentDataPath + "/" + "beer_order";
    //#elif UNITY_ANDROID
    //        savePath = "mnt/sdcard/DCIM/" + "Beer_Order/";   //"mnt/sdcard/DCIM/"
    //#else
    //		if( Application.isEditor == true ){ 
    //			savePath = "mnt/sdcard/DCIM/beer_order";
    //		} 
    //#endif
    //        downloadImage(menu_image, savePath);
    //        string img_name = savePath + Path.GetFileNameWithoutExtension(menu_image);
    //        Debug.Log(img_name);
    //        img.sprite = Resources.Load<Sprite>(img_name);
    //    }
}
