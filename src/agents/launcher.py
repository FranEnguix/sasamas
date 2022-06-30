import json
import time
import os

from entity import EntityAgent
from queue import Queue
from threading import Thread

def create_default_config_file(filename:str) -> dict:
    json_string = """
    {
        "simulator":
        {
            "address": "127.0.0.1",
            "commandPort": 6066,
            "imagePort": 6067
        },
        "agents": [
            {
                "name": "agente1",
                "at": "localhost",
                "password": "xmppserver",
                "imageBufferSize": 3,
                "imageFolderName": "captures",
                "enableAgentCollision": true,
                "prefabName": "Tractor",
                "position": {
                    "x": -2.6,
                    "y": 0.0,
                    "z": 0.0
                }
            },
            {
                "name": "agente2",
                "at": "localhost",
                "password": "xmppserver",
                "imageBufferSize": 3,
                "imageFolderName": "captures",
                "enableAgentCollision": true,
                "prefabName": "Tractor",
                "position": "Spawner 1"
            }
        ]
    }
    """
    json_object = json.loads(json_string)
    write_json_file(filename, json_object)
    return json_object

def read_json_file(filename:str) -> dict:
    with open(filename, "r") as config_file:
        return json.load(config_file)

def write_json_file(filename:str, json_object:dict):
    with open(filename, "w") as config_file:
        json.dump(json_object, config_file, indent=4)

def load_config(filename:str) -> dict:
    if os.path.isfile(filename):
        return read_json_file(filename)
    else:
        return create_default_config_file(filename)

def setup_thread_agents(agents:list, simulator:dict, threads:list):
    entities = Queue() 
    for agent in agents:
        t = Thread(target=launch_agent, args=(agent, simulator, entities,))
        t.daemon = True
        t.name = agent['name']
        threads.append(t)
        t.start()
    return list(entities.queue)

def launch_agent(agent:dict, config:dict, entities:Queue):
    command_socket_info = (config['address'], config['commandPort'])
    image_socket_info = (config['address'], config['imagePort'])
    entity = EntityAgent(
        f"{agent['name']}@{agent['at']}", 
        agent['password'],
        command_socket_info,
        image_socket_info,
        agent['imageBufferSize'],
        agent['imageFolderName'],
        agent['enableAgentCollision'],
        agent['prefabName'],
        agent['position'],
    )
    future = entity.start()
    future.result()
    entities.put(entity)

def wait_for_agents(entities:list):
    alive = True
    while alive:
        for entity in entities:
            if entity.is_alive():
                alive = True
                break
            alive = False
        time.sleep(1)

if __name__ == "__main__":
    configuration = load_config("configuration.json")
    simulator_config = configuration['simulator']
    agents_config = configuration['agents']

    threads = []
    entities = setup_thread_agents(agents_config, simulator_config, threads)
    try:
        wait_for_agents(entities)
    except KeyboardInterrupt:
        for entity in entities:
            entity.stop()
    for thread in threads:
        thread.join()
