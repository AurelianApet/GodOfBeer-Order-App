using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SocketIO;
using UnityEngine.UI;
using SimpleJSON;
using System;

public class AutoSlideManager : MonoBehaviour
{
    public GameObject complete_popup;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;
    public GameObject imgSlidePrefab;
    public GameObject imgslideParent;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("recept_order_process", recept_order_process);
        socket.On("recept_call_process", recept_order_process);
        socket.On("error", socketError);
        socket.On("close", socketClose);

        try
        {
            if (Global.setInfo.slide_option == 0 && Global.setInfo.paths != null)
            {
                //저장된 이미지
                for (int i = 0; i < Global.setInfo.paths.Length; i++)
                {
                    GameObject slideobj = Instantiate(imgSlidePrefab);
                    slideobj.transform.SetParent(imgslideParent.transform);
                    Texture2D tex = NativeGallery.LoadImageAtPath(Global.setInfo.paths[i], 512); // image will be downscaled if its width or height is larger than 1024px
                    if (tex != null)
                    {
                        slideobj.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
                    }
                    slideobj.transform.localScale = Vector3.one;
                    slideobj.transform.localPosition = Vector3.zero;
                    if (Global.setInfo.paths.Length == 1)
                    {
                        slideobj.GetComponent<Image>().type = Image.Type.Simple;
                    }
                    else
                    {
                        slideobj.GetComponent<Image>().type = Image.Type.Sliced;
                    }
                }
            }
            else
            {
                //디폴트이미지
                for (int i = 1; i <= 3; i++)
                {
                    GameObject slideobj = Instantiate(imgSlidePrefab);
                    slideobj.transform.SetParent(imgslideParent.transform);
                    slideobj.GetComponent<Image>().sprite = Resources.Load<Sprite>("slide" + i);
                    slideobj.transform.localScale = Vector3.one;
                    slideobj.transform.localPosition = Vector3.zero;
                }
            }
        } catch(Exception ex)
        {

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
        if (Input.anyKey)
        {
            StartCoroutine(GotoScene("home"));
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
