using UnityEngine;
using System.Collections;
using TMPro;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public struct CMD
{
    public CMD(int i,  string c, int f)
    {
        id = i;
        cmd = c;
        frame = f;
    }

    public int id;
    public string cmd;
    public int frame;
}

public struct CMDList
{
    public List<CMD> cmds;
    public long time;
}

public class DelayedSpawner : MonoBehaviour
{
    public GameObject squareRolePrefab; // 方块物体的预制件
    public GameObject nameLabelPrefab; // 名字标签的预制件

    private ClientWebSocket _webSocket;
    private bool isWebSocketConnected = false;
    private List<string> names = new List<string> { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Hobby", "Oliver", "Emma", "Liam", "Ava", "Noah" }; // 可用的名字列表
    private List<SquareRoleController> RoleList = new();
    private List<CMDList> commands = new List<CMDList>();
    private readonly int g_interval = 40;
    private long g_counter = 0;
    private int g_frame = 0;
    private long g_lasttime = 0;
    private int g_status = 0;
    private Stopwatch stopwatch = new Stopwatch();
    private long g_lastpress = 0;


    private async void Start()
    {
        stopwatch.Start();

        // 启动 WebSocket 客户端
        await SetupWebSocket();

        // 延迟创建方块物体和名字标签
        StartCoroutine(SpawnAfterDelay(2f));


    }

    private void Spawn(string name)
    {
        // 创建方块物体
        GameObject squareRole = Instantiate(squareRolePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        squareRole.name = name + "_" + RoleList.Count.ToString();

        // 创建名字标签
        GameObject nameLabel = Instantiate(nameLabelPrefab);
        //nameLabel.transform.SetParent(GameObject.Find("Canvas").transform, false); // 确保UI文本在Canvas下
        nameLabel.transform.position = Camera.main.WorldToScreenPoint(squareRole.transform.position);
        FollowObject controller0 = nameLabel.GetComponent<FollowObject>();
        controller0.objectToFollow = squareRole.transform;

        // 配置方块物体和名字标签
        SquareRoleController controller = squareRole.GetComponent<SquareRoleController>();
        controller.squareRole = squareRole.transform;
        controller.nameLabel = nameLabel.transform.Find("NameLable").GetComponent<RectTransform>();
        controller.squareRenderer = squareRole.GetComponent<SpriteRenderer>();
        controller.nameText = nameLabel.transform.Find("NameLable").GetComponent<TextMeshProUGUI>(); // 使用TextMeshProUGUI
        controller.SetName(name);
        controller.RandomColor();

        RoleList.Add(controller);
    }

    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        //// 创建方块物体
        //GameObject squareRole = Instantiate(squareRolePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        //squareRole.name = "SquareRole";

        //// 创建名字标签
        //GameObject nameLabel = Instantiate(nameLabelPrefab);
        ////nameLabel.transform.SetParent(GameObject.Find("Canvas").transform, false); // 确保UI文本在Canvas下
        //nameLabel.transform.position = Camera.main.WorldToScreenPoint(squareRole.transform.position);
        //FollowObject controller0 = nameLabel.GetComponent<FollowObject>();
        //controller0.objectToFollow = squareRole.transform;

        //// 配置方块物体和名字标签
        //SquareRoleController controller = squareRole.GetComponent<SquareRoleController>();
        //controller.squareRole = squareRole.transform;
        //controller.nameLabel = nameLabel.transform.Find("NameLable").GetComponent<RectTransform>();
        //controller.squareRenderer = squareRole.GetComponent<SpriteRenderer>();
        //controller.nameText = nameLabel.transform.Find("NameLable").GetComponent<TextMeshProUGUI>(); // 使用TextMeshProUGUI
        //controller.RandomName();
        //controller.RandomColor();

        // 发送消息到 WebSocket 服务器
        if (isWebSocketConnected)
        {
            string randomName = names[UnityEngine.Random.Range(0, names.Count)];

            Task sendMsgTask = SendMsg("enter&" + randomName);
            yield return new WaitUntil(() => sendMsgTask.IsCompleted);
            //await SendMsg("SquareRole created with a random name and color.");

            //Spawn("1234");
            //Spawn("5678");
        }
    }

    // 设置 WebSocket 连接
    private async Task SetupWebSocket()
    {
        _webSocket = new ClientWebSocket();
        Uri serverUri = new Uri("ws://10.0.10.37:8080");

        try
        {
            await _webSocket.ConnectAsync(serverUri, CancellationToken.None);
            UnityEngine.Debug.Log("Connected to WebSocket server");
            isWebSocketConnected = true;

            // 启动接收消息的任务
            _ = ReceiveMessagesAsync();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Error connecting to WebSocket server: " + ex.Message);
        }
    }

    // 发送消息到 WebSocket 服务器
    private async Task SendMsg(string msg)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
            UnityEngine.Debug.Log("Sent msg: " + msg);
        }
        else
        {
            UnityEngine.Debug.LogError("WebSocket is not connected");
        }
    }

    private void Dispath(string msg)
    {
        string[] parts = msg.Split('&');

        if (parts.Length <= 0)
            return;

        if ("start" == parts[0])
        {
            for (int i = 2; i < parts.Length; i += 2)
            {
                Spawn(parts[i]);
            }

            g_status = 1;
        }
        else if ("cmd" == parts[0])
        {
            CMDList list = new CMDList();
            list.cmds = new();

            list.time = g_interval;
            for (int i = 1; i < parts.Length; i += 3)
            {
                CMD cmd = new(int.Parse(parts[i]), parts[i+1], int.Parse(parts[i+2]));
                list.cmds.Add(cmd);
            }

            commands.Add(list);

            UnityEngine.Debug.Log("Received cmd: " + msg);
        }
    }

    // 接收 WebSocket 消息
    private async Task ReceiveMessagesAsync()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                else
                {
                    string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log("Received msg: " + response);
                    Dispath(response);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error receiving message: " + ex.Message);
        }
    }

    // 处理应用程序退出
    private async void OnApplicationQuit()
    {
        if (_webSocket != null)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            _webSocket.Dispose();
        }
    }

    private void StepUpdate()
    {
        g_frame++;

    }
    private void UpdateM(long t)
    {
        if (g_status == 0)
            return;

        if (true)
        {
            g_counter += t;
            if (g_counter > g_interval)
            {
                StepUpdate();
                g_counter -= g_interval;
            }
        }

        if (commands.Count <= 0)
            return;

        var cmdlist = commands[0];
        var ms = t;
        if (ms > commands[0].time)
        {
            ms = commands[0].time;
        }

        foreach (var cmd in cmdlist.cmds)
        {
            RoleList[cmd.id].Move(ms, cmd.cmd);
        }

        cmdlist.time -= ms;

        if (cmdlist.time == 0)
        {
            commands.RemoveAt(0);
        }
        else
        {
            commands[0] = cmdlist;
        }
    }

    private void CheckKeyInput()
    {
        long now = stopwatch.ElapsedMilliseconds;
        if (now - g_lastpress < 15)
            return;

        g_lastpress = now;

        string key = "";
        // 检测按键
        if (Input.GetKey(KeyCode.UpArrow))
        {
            key = "UP";
            Debug.Log("Up Arrow key pressed");
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            key = "DOWN";
            Debug.Log("Down Arrow key pressed");
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            key = "LEFT";
            Debug.Log("Left Arrow key pressed");
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            key = "RIGHT";
            Debug.Log("Right Arrow key pressed");
        }

        //Debug.Log("Update method called"); // 添加调试日志

        if (key.Length > 0)
        {
            SendMsg("cmd&" + key);
            //Task sendMsgTask = SendMsg("cmd&" + key);
            //yield return new WaitUntil(() => sendMsgTask.IsCompleted);
        }
    }

    private void Update()
    {
        CheckKeyInput();

        if (0 == g_lasttime)
        {
            g_lasttime = stopwatch.ElapsedMilliseconds;
            return;
        }

        long now = stopwatch.ElapsedMilliseconds;
        UpdateM(now - g_lasttime);
        g_lasttime = now;
    }
}
