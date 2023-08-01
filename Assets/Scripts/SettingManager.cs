using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LitJson;
using SimpleJSON;
using System;
using SocketIO;
using System.IO;

public class SettingManager : MonoBehaviour
{
    bool type = false;//save, true-save1
    bool is_set_client_call = false; //is client call setting
    public InputField bus_id;
    public InputField sftkey;
    public InputField pip;

    public Dropdown tableno;
    public Toggle is_call_client;

    public GameObject popup1;
    public GameObject popup2;
    public GameObject error_popup;
    public Text error_string;

    public GameObject tableBtn;
    public Text title;
    public GameObject selSlideForm;
    public GameObject imgField;
    public GameObject slideParent;
    public GameObject imgSlidePrefab;

    public GameObject use_call_option;
    public GameObject unuse_call_option;
    public GameObject default_option;
    public GameObject select_slide_option;

    public GameObject complete_popup;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    float time = 0f;
    bool is_completed_save = false;
    bool is_saved_firstinfo = false;
    bool is_socket_open = false;
    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
        if (Global.server_address != "")
        {
            //이미 보관된 정보가 있다면
            Debug.Log("보관된 셋팅정보로부터 UI-테이블리스트 만들기.");
            if(Global.setInfo.soft_key != null && Global.setInfo.soft_key != "")
            {
                sftkey.text = Global.GetShowPassFormat(Global.setInfo.soft_key);
            }
            //테이블번호 리스트 생성
            tableno.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            int index = -1;
            for (int i = 0; i < Global.setInfo.tablenolist.Count; i++)
            {
                Dropdown.OptionData item = new Dropdown.OptionData();
                item.text = Global.setInfo.tablenolist[i].name;
                if (Global.setInfo.tablenolist[i].name == Global.setInfo.table_no.name)
                {
                    index = i;
                }
                options.Add(item);
            }
            tableno.AddOptions(options);
            tableno.value = index;
            if (Global.setInfo.is_client_call)
            {
                use_call_option.GetComponent<Toggle>().isOn = true;
                unuse_call_option.GetComponent<Toggle>().isOn = false;
            }
            else
            {
                use_call_option.GetComponent<Toggle>().isOn = false;
                unuse_call_option.GetComponent<Toggle>().isOn = true;
            }

            Debug.Log(Global.setInfo.slide_option);

            if (Global.setInfo.slide_option == 0)
            {
                select_slide_option.GetComponent<Toggle>().isOn = true;
                default_option.GetComponent<Toggle>().isOn = false;
                selSlideForm.SetActive(true);
            }
            else
            {
                default_option.GetComponent<Toggle>().isOn = true;
                select_slide_option.GetComponent<Toggle>().isOn = false;
            }
            socketObj = Instantiate(socketPrefab);
            socket = socketObj.GetComponent<SocketIOComponent>();
            socket.On("open", socketOpen);
            socket.On("recept_order_process", recept_order_process);
            socket.On("recept_call_process", recept_order_process);
            socket.On("error", socketError);
            socket.On("close", socketClose);
        }
        popup1.SetActive(true);
        popup2.SetActive(false);
        tableBtn.SetActive(true);
        title.text = "관리자설정";
    }

    IEnumerator GotoScene(string sceneName)
    {
        if (socket != null)
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
        if (popup2.transform.Find("imgslide_option/select").GetComponent<Toggle>().isOn)
        {
            selSlideForm.SetActive(true);
        }
        else
        {
            selSlideForm.SetActive(false);
        }
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
    }

    public void onExit()
    {
        Application.Quit();
    }

    public void onBack()
    {
        if (Global.lastScene != "" && !is_saved_firstinfo)
        {
            StartCoroutine(GotoScene(Global.lastScene));
        }
        else
        {
            if (!type)
            {
                if (!is_set_client_call)
                {
                    error_string.text = @"테이블번호를 입력하세요.";
                    error_popup.SetActive(true);
                }
                else
                {
                    StartCoroutine(GotoScene("home"));
                }
            }
            else
            {
                popup1.SetActive(true);
                popup2.SetActive(false);
                tableBtn.SetActive(true);
                title.text = "관리자설정";
                type = false;
            }
        }
    }

    public void Save()
    {
        if (!type)
        {
            if (bus_id.text == "")
            {
                error_string.text = @"사업자등록번호를 입력하세요.";
                error_popup.SetActive(true);
                return;
            }
            else if (sftkey.text == "")
            {
                error_string.text = @"Software Key값을 입력하세요.";
                error_popup.SetActive(true);
                return;
            }
            if (pip.text == "")
            {
                error_string.text = @"IP를 입력하세요.";
                error_popup.SetActive(true);
                return;
            }
            //set info api
            WWWForm form = new WWWForm();
            form.AddField("bus_id", bus_id.text);
            form.AddField("sft_key", sftkey.text);
            if(Global.setInfo.bus_id != "" && Global.setInfo.bus_id != null)
            {
                form.AddField("old_busid", Global.setInfo.bus_id);
                form.AddField("old_sftkey", Global.setInfo.soft_key);
            }
            Global.server_address = pip.text;
            Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
            Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
            WWW www = new WWW(Global.api_url + Global.set_info_api, form);
            StartCoroutine(ProcessSetInfo(www));
        }
        else
        {
            if (tableno.options[tableno.value].text == "")
            {
                error_string.text = @"테이블번호를 선택하세요.";
                error_popup.SetActive(true);
            }
            else
            {
                string table_id = "";
                for (int i = 0; i < Global.setInfo.tablenolist.Count; i++)
                {
                    if (Global.setInfo.tablenolist[i].name == tableno.options[tableno.value].text)
                    {
                        table_id = Global.setInfo.tablenolist[i].id; break;
                    }
                }
                Table_Info tinfo = new Table_Info();
                tinfo.name = tableno.options[tableno.value].text;
                tinfo.id = table_id;
                int slide_option = 0;
                if (popup2.transform.Find("imgslide_option/default").GetComponent<Toggle>().isOn)
                {
                    slide_option = 1;
                }
                Global.setInfo.table_no = tinfo;
                Global.setInfo.is_client_call = is_call_client;
                Global.setInfo.slide_option = slide_option;
                Global.setInfo.paths = slideImgs;
                string sImgs = "";
                if (slideImgs != null)
                {
                    for (int i = 0; i < slideImgs.Length; i++)
                    {
                        if (i != slideImgs.Length - 1)
                        {
                            sImgs += slideImgs[i] + ",";
                        }
                        else
                        {
                            sImgs += slideImgs[i];
                        }
                    }
                }
                PlayerPrefs.SetString("tableid", table_id);
                Global.setInfo.is_client_call = is_call_client.isOn;
                PlayerPrefs.SetInt("is_client_call", is_call_client.isOn ? 1 : 0);
                PlayerPrefs.SetInt("slide_option", slide_option);
                PlayerPrefs.SetString("slideImgs", sImgs);
                Debug.Log(Global.setInfo.slide_option);
                socketObj = Instantiate(socketPrefab);
                socket = socketObj.GetComponent<SocketIOComponent>();
                socket.On("open", socketOpen);
                socket.On("recept_order_process", recept_order_process);
                socket.On("recept_call_process", recept_order_process);
                socket.On("error", socketError);
                socket.On("close", socketClose);
                error_string.text = @"성공적으로 저장되었습니다.";
                is_set_client_call = true;
                is_completed_save = true;
                error_popup.SetActive(true);
            }
        }
    }

    IEnumerator ProcessSetInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.setInfo.bus_id = bus_id.text;
                Global.setInfo.soft_key = sftkey.text;
                popup1.SetActive(false);
                //www로부터 테이블리스트정보 얻어서 list<string>에 넣기
                JSONNode t_list = JSON.Parse(jsonNode["tablenolist"].ToString()/*.Replace("\"", "")*/);
                Global.setInfo.tablenolist = new List<Table_Info>();
                for (int i = 0; i < t_list.Count; i++)
                {
                    try
                    {
                        string tno = t_list[i]["id"];
                        string tname = t_list[i]["name"];
                        Table_Info tinfo = new Table_Info();
                        tinfo.id = tno;
                        tinfo.name = tname;
                        Global.setInfo.tablenolist.Add(tinfo);
                    } catch (Exception ex)
                    {

                    }
                }
                PlayerPrefs.SetString("bus_id", bus_id.text);
                PlayerPrefs.SetString("sft_key", sftkey.text);
                PlayerPrefs.SetString("ip", Global.server_address);
                Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
                Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
                Global.setInfo.is_used_sftkey = 0;
                if (socket != null)
                {
                    socket.Close();
                    socket.OnDestroy();
                    socket.OnApplicationQuit();
                }
                if (socketObj != null)
                {
                    DestroyImmediate(socketObj);
                }
                //테이블번호 리스트 생성
                tableno.ClearOptions();
                List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
                for (int i = 0; i < Global.setInfo.tablenolist.Count; i++)
                {
                    try
                    {
                        Dropdown.OptionData item = new Dropdown.OptionData();
                        item.text = Global.setInfo.tablenolist[i].name;
                        options.Add(item);
                    } catch (Exception ex)
                    {

                    }
                }
                tableno.AddOptions(options);
                is_saved_firstinfo = true;
                error_string.text = @"성공적으로 저장되었습니다.";
                error_popup.SetActive(true);
            }
            else
            {
                error_string.text = jsonNode["msg"]/*.Replace("\"", "")*/;
                error_popup.SetActive(true);
            }
        }
        else
        {
            error_string.text = @"저장에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    public void onTableInput()
    {
        //get tableno list api
        if (Global.setInfo.is_used_sftkey == 1)
        {
            error_string.text = @"먼저 정보를 저장하세요.";
            error_popup.SetActive(true);
        }
        else
        {
            type = true;
            popup1.SetActive(false);
            popup2.SetActive(true);
            tableBtn.SetActive(false);
            title.text = "추가옵션설정";

            if(Global.setInfo.slide_option == 0)
            {
                //저장된 슬라이드방식
                StartCoroutine(ClearSlides());
                slideImgs = Global.setInfo.paths;
                if(slideImgs != null)
                {
                    imgSlideItem = new GameObject[Global.setInfo.paths.Length];

                    for (int i = 0; i < Global.setInfo.paths.Length; i++)
                    {
                        if (Global.setInfo.paths[i] != null && Global.setInfo.paths[i] != "")
                        {
                            imgSlideItem[i] = Instantiate(imgSlidePrefab);
                            imgSlideItem[i].transform.SetParent(slideParent.transform);
                            //StartCoroutine(downloadImage(Global.setInfo.paths[i], Global.imgPath + Path.GetFileName(Global.setInfo.paths[i]), imgSlideItem[i]));
                            Texture2D tex = NativeGallery.LoadImageAtPath(Global.setInfo.paths[i], 512); // image will be downscaled if its width or height is larger than 1024px
                            if (tex != null)
                            {
                                imgSlideItem[i].GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
                            }
                        }
                    }
                }
            }
        }
    }

    public void onConfirmError()
    {
        if (is_completed_save)
        {
            if (Global.lastScene == "")
            {
                StartCoroutine(GotoScene("home"));
            }
            else
            {
                StartCoroutine(GotoScene(Global.lastScene));
            }
        }
        error_popup.SetActive(false);
        if (!type)
        {
            popup1.SetActive(true);
            tableBtn.SetActive(true);
        }
        else
        {
            popup2.SetActive(true);
            tableBtn.SetActive(false);
            title.text = "추가옵션설정";
        }
    }

    public void onCompletePopup()
    {
        complete_popup.SetActive(false);
    }

    GameObject[] imgSlideItem;

    IEnumerator ClearSlides()
    {
        while (slideParent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(slideParent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {

            }
            yield return new WaitForSeconds(0.001f);
        }
    }

    string[] slideImgs;
    public void openGallery()
    {
        Debug.Log("open gallery");
        StartCoroutine(ClearSlides());
        if (NativeGallery.CanSelectMultipleFilesFromGallery())
        {
            NativeGallery.GetImagesFromGallery((paths) =>
            {
                if (paths != null)
                {
                    slideImgs = paths;
                    imgSlideItem = new GameObject[paths.Length];
                    for (int i = 0; i < paths.Length; i ++)
                    {
                        imgSlideItem[i] = Instantiate(imgSlidePrefab);
                        imgSlideItem[i].transform.SetParent(slideParent.transform);
                        //StartCoroutine(downloadImage(slideImgs[i], Global.imgPath + Path.GetFileName(slideImgs[i]), imgSlideItem[i]));
                        Texture2D tex = NativeGallery.LoadImageAtPath(paths[i], 512); // image will be downscaled if its width or height is larger than 1024px
                        if (tex != null)
                        {
                            imgSlideItem[i].GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
                        }
                    }
                }
            }, title: "슬라이드이미지 선택", mime: "image/*");
        }

    }

    IEnumerator downloadImage(string url, string pathToSaveImage, GameObject imgObj)
    {
        yield return new WaitForSeconds(0.001f);
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
            //Debug.Log("Download Image: " + path.Replace("/", "\\"));
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
        //Debug.Log("load image = " + Global.prePath + name);
        WWW pictureWWW = new WWW(Global.prePath + name);
        yield return pictureWWW;

        if (img != null)
        {
            img.sprite = Sprite.Create(pictureWWW.texture, new Rect(0, 0, pictureWWW.texture.width, pictureWWW.texture.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
        }
    }

    public void OnApplicationQuit()
    {
        socket.Close();
        socket.OnDestroy();
        socket.OnApplicationQuit();
    }
}
