using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SocketIO;

public class HomeManager : MonoBehaviour
{
    float time = 0f;
    public GameObject left;
    public GameObject left1;
    public GameObject popup;
    public GameObject err_popup;
    public Text err_msg;
    public InputField manager_pwd;
    public GameObject client_popup;
    public GameObject complete_popup;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
        Debug.Log(Global.setInfo.table_no.id);
        if (Global.server_address == "" || Global.server_address == null || Global.setInfo.soft_key == null || Global.setInfo.soft_key == "")
        {
            Debug.Log("보관된 셋팅정보가 없습니다.");
            SceneManager.LoadScene("setting");
        }
        else if (Global.setInfo.table_no.id == "" || Global.setInfo.table_no.id == null)
        {
            WWWForm form = new WWWForm();
            form.AddField("softkey", Global.setInfo.soft_key);
            WWW www = new WWW(Global.api_url + Global.check_softkey_api, form);
            StartCoroutine(ProcessCheckSoftkey1(www));
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("softkey", Global.setInfo.soft_key);
            WWW www = new WWW(Global.api_url + Global.check_softkey_api, form);
            StartCoroutine(ProcessCheckSoftkey(www));
        }
    }
    IEnumerator ProcessCheckSoftkey1(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Debug.Log("테이블정보가 보관되지 않았습니다.");
                Global.setInfo.is_used_sftkey = 0;//유효
                SceneManager.LoadScene("setting");
            }
            else
            {
                Debug.Log("Software Key 값을 확인하세요.");
                Global.setInfo.is_used_sftkey = 1;//무효
                SceneManager.LoadScene("setting");
            }
        }
        else
        {
            Debug.Log("Software Key 값을 확인하세요.");
            Global.setInfo.is_used_sftkey = 1;//무효
            SceneManager.LoadScene("setting");
        }
    }

    IEnumerator ProcessCheckSoftkey(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if (!Global.setInfo.is_client_call)
                {
                    GameObject.Find("right/call").SetActive(false);
                }
                else
                {
                    GameObject.Find("right/call").SetActive(true);
                }
                Global.lastScene = "home";
                socketObj = Instantiate(socketPrefab);
                //socketObj = GameObject.Find("SocketIO");
                socket = socketObj.GetComponent<SocketIOComponent>();
                socket.On("open", socketOpen);
                socket.On("recept_order_process", recept_order_process);
                socket.On("recept_call_process", recept_order_process);
                socket.On("error", socketError);
                socket.On("close", socketClose);
            }
            else
            {
                Debug.Log("Software Key 값을 확인하세요.");
                Global.setInfo.is_used_sftkey = 1;//무효
                SceneManager.LoadScene("setting");
            }
        }
        else
        {
            Debug.Log("Software Key 값을 확인하세요.");
            Global.setInfo.is_used_sftkey = 1;//무효
            SceneManager.LoadScene("setting");
        }
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
        for(int i = 0; i < mlist.Count; i++)
        {
            if(i != mlist.Count - 1)
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
            if(time != 0f)
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

    IEnumerator GotoScene(string sceneName)
    {
        if(socket != null)
        {
            socket.Close();
            socket.OnDestroy();
            socket.OnApplicationQuit();
        }
        if (socketObj != null)
        {
            DestroyImmediate(socketObj);
        }
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(sceneName);
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
                        minfo.product_type = c_list[i]["product_type"].AsInt;
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

    public void OnSelMarket()
    {
        Global.is_market = 0;
        WWWForm form = new WWWForm();
        form.AddField("type", Global.is_market);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    public void OnSelPozhang()
    {
        Global.is_market = 1;
        WWWForm form = new WWWForm();
        form.AddField("type", Global.is_market);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    public void GoSetting()
    {
        manager_pwd.text = "";
        popup.SetActive(true);
    }

    public void OnCancelPopup()
    {
        popup.SetActive(false);
    }

    public void OnConfirmPopup()
    {
        //관리자비밀번호 인증
        //칸이 비엿으면
        if (manager_pwd.text == "")
        {
            popup.SetActive(false);
            err_msg.text = "관리자비밀번호를 입력하세요.";
            err_popup.SetActive(true);
        }
        else
        {
            //verify api
            WWWForm form = new WWWForm();
            form.AddField("password", manager_pwd.text);
            WWW www = new WWW(Global.api_url + Global.verify_api, form);
            StartCoroutine(ProcessVerifyInfo(www));
        }
    }

    IEnumerator ProcessVerifyInfo(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                StartCoroutine(GotoScene("setting"));
            }
            else
            {
                popup.SetActive(false);
                err_msg.text = jsonNode["msg"]/*.Replace("\"", "")*/;
                err_popup.SetActive(true);
            }
        }
        else
        {
            popup.SetActive(false);
            err_msg.text = "관리자비밀번호인증에 실패하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void OnConfirmErrPopup()
    {
        err_popup.SetActive(false);
        popup.SetActive(true);
    }

    public void onConfirmClientPopup()
    {
        client_popup.SetActive(false);
    }

    public void onCompletePopup()
    {
        complete_popup.SetActive(false);
    }

    public void OnApplicationQuit()
    {
        socket.Close();
        socket.OnDestroy();
        socket.OnApplicationQuit();
    }
}
