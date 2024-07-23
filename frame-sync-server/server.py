import asyncio
import websockets
import time

g_socket_map = {}
g_wid = 0
g_object_map = {} #g_wid = [name]
g_frame = 0
g_cmd_list = []
g_couter = 0
g_status = 0
g_lasttime = 0
g_interval = 40
g_max_player = 2


def getms():
    return int(time.perf_counter() * 1000)

async def dispath(websocket, message):
    global g_socket_map
    global g_wid
    global g_object_map
    global g_status
    l = message.split('&')
    if 'enter' == l[0]:
        g_socket_map[websocket] = g_wid
        g_object_map[g_wid] = l[1]
        g_wid = g_wid + 1

        if len(g_object_map) == g_max_player: # 人满
            info = 'start'
            for i in range(g_max_player):
                info = info + '&{0}&{1}'.format(i, g_object_map[i])

            for key, value in g_socket_map.items():
                await key.send(info)
                print(f"send start to {0}", key)
            g_status = 1
    elif 'cmd' == l[0]:
        g_cmd_list.append([g_socket_map[websocket], l[1]])
        #print("cmd list :{0}".format(g_cmd_list))

    # print(websocket, message)
    # print(g_socket_map)
    # print(g_object_map)

async def handle_message(websocket, path):
    async for message in websocket:
        print(f"Received message: {message}")
        # 在这里处理收到的消息，并生成新内容
        new_content = f"Server received: {message}"

        await dispath(websocket, message)

        # await websocket.send(new_content)

async def stepUpdate():
    global g_frame
    global g_object_map
    global g_cmd_list

    #print("current frame {0} at frame".format(g_frame))

    if g_status != 1:
        return;

    ln = len(g_cmd_list)
    msg = {}
    for key, value in g_object_map.items():
        msg[key] = [key, '', g_frame]

    #print("current msg {0} at frame".format(msg))

    for l in g_cmd_list:
        msg[l[0]] = [l[0], l[1], g_frame]

    #print("again msg {0} at frame".format(msg))

    g_cmd_list = []

    content = 'cmd'
    for key, value in msg.items():
        content = content +'&{0}&{1}&{2}'.format(value[0], value[1], value[2])

    print("current content {0} at frame".format(content))

    if ln > 0:
        for key, value in g_socket_map.items():
            await key.send(content)
        print("broadcast cmd {0} at frame {1}".format(content, g_frame))

async def update(dt):
    global g_frame
    global g_status
    global g_couter
    global g_interval

    if g_status == 1:
        g_couter += dt
        if g_couter > g_interval:
            g_frame = g_frame + 1
            await stepUpdate()
            g_couter -= g_interval

async def periodic_task():
    global g_lasttime
    while True:
        # 每帧调用一次的函数
        if g_lasttime == 0:
            g_lasttime = getms()
            continue

        now = getms()
        await update(now - g_lasttime)
        g_lasttime = now

        #print("Periodic task executed")
        await asyncio.sleep(0.001)

async def main():
    # 创建 WebSocket 服务器
    server = await websockets.serve(handle_message, "0.0.0.0", 8080)
    print("WebSocket server started on ws://0.0.0.0:8080, max player: {0}".format(g_max_player))

    # 创建并发任务
    periodic_task_coro = periodic_task()
    periodic_task_task = asyncio.create_task(periodic_task_coro)

    # 等待 WebSocket 服务器关闭
    await server.wait_closed()

    # 等待定时任务结束（实际上不会到这里，因为 server.wait_closed() 会一直等待）
    await periodic_task_task

# 运行 WebSocket 服务器
if __name__ == "__main__":
    asyncio.run(main())
