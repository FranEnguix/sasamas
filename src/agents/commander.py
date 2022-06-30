import json
import time

from enum import IntEnum
from socket import socket
from spade.agent import Agent

class Axis(IntEnum):
    X = 0
    Y = 1
    Z = 2

class ImageMode(IntEnum):
    DISABLE = -1
    INSTANT = 0

class Commander:
    def __init__(self, command_socket: socket):
        self.__command_socket = command_socket

    async def send_msg_to_server_and_wait(self, msg:str) -> str:
        """
        Send a message and waits for a response
        """
        encoded_msg = (msg).encode()
        self.__command_socket.sendall(bytearray(encoded_msg))
        return self.__command_socket.recv(128)

    async def send_command_to_server_and_wait(self, msg:dict) -> str:
        """
        Send a command and waits for a response
        """
        encoded_msg = json.dumps(msg).encode()
        self.__command_socket.sendall(bytearray(encoded_msg))
        return self.__command_socket.recv(128)

    def send_command_to_server(self, msg:dict):
        """
        Send a command 
        """
        encoded_msg = json.dumps(msg).encode()
        self.__command_socket.sendall(bytearray(encoded_msg))


    async def create_agent(self, agent: Agent) -> list:
        command = { 'commandName': 'create', 'data': [agent.name, agent.prefab_name] }
        position = agent.starter_position
        if isinstance(position, str):
            command['data'].append(position)
        else:
            command['data'].append(f"({position['x']} {position['y']} {position['z']})")
        command['data'].append(agent.agent_collision)
        agent_position = (await self.send_command_to_server_and_wait(command)).decode('utf-8')
        return [float(x) for x in (agent_position.split())[1:]]

    async def move_agent(self, position: list) -> list:
        command = { 'commandName': 'moveTo', 'data': [position] }
        msg = (await self.send_command_to_server_and_wait(command)).decode('utf-8')
        new_position = [float(x) for x in (msg.split())[1:]]
        return new_position

    async def fov_camera(self, camera_id: int, fov: float):
        data = [ f"{camera_id}", f"{fov}" ]
        cameraRotateCommand = { 'commandName': 'cameraFov', 'data': data }
        self.send_command_to_server(cameraRotateCommand)

    async def move_camera(self, camera_id: int, axis: Axis, relative_position: float):
        data = [ f"{camera_id}", f"{axis}", f"{relative_position}" ]
        cameraRotateCommand = { 'commandName': 'cameraMove', 'data': data }
        self.send_command_to_server(cameraRotateCommand)

    async def rotate_camera(self, camera_id: int, axis: Axis, degrees: float):
        data = [ f"{camera_id}", f"{axis}", f"{degrees}" ]
        cameraRotateCommand = { 'commandName': 'cameraRotate', 'data': data }
        self.send_command_to_server(cameraRotateCommand)

    async def take_image(self, camera_id: int, image_mode: float):
        command = { 'commandName': 'image', 'data': [ f"{camera_id}", f"{image_mode}" ] }
        self.send_command_to_server(command)

    async def change_color(self, r: float, g: float, b: float, a: float = 1):
        ''' Color must be normalized between [0, 1]'''
        color = { 'r': r, 'g': g, 'b': b, 'a': a }
        color_string = json.dumps(color)
        command = { 'commandName': 'color', 'data': [ color_string ] }
        self.send_command_to_server(command)
