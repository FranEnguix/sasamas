import asyncio
import json
import socket
import sys
import time

from spade.agent import Agent
from spade.behaviour import FSMBehaviour, State
from spade.message import Message

# --------------------------------------------- #
# BEHAVIOUR                                     #
# --------------------------------------------- #
STATE_INIT = "STATE_INITIALIZATION"
STATE_PERCEPTION = "STATE_PERCEPTION"
STATE_COGNITION = "STATE_COGNITION"
STATE_ACTION = "STATE_ACTION"
    
class AgentBehaviour(FSMBehaviour):
    async def on_start(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect(self.agent.socket_info)

    async def run(self):
        pass

    async def on_end(self):
        await self.agent.stop()

class AgentState(State):
    async def on_start(self):
        behaviour = self.agent.behaviours[0]
        self.sock = behaviour.sock

    def send_msg_to_server(self, msg:str):
        encoded_msg = (msg + "\n").encode()
        self.sock.sendall(bytearray(encoded_msg))

    async def send_msg_to_server_and_wait(self, msg:str) -> str:
        encoded_msg = (msg + "\n").encode()
        self.sock.sendall(bytearray(encoded_msg))
        return self.sock.recv(2048)

    async def send_command_to_server_and_wait(self, msg:dict) -> str:
        encoded_msg = json.dumps(msg).encode()
        self.sock.sendall(bytearray(encoded_msg))
        return self.sock.recv(2048)

    async def send_command_to_server(self, msg:dict):
        encoded_msg = json.dumps(msg).encode()
        self.sock.sendall(bytearray(encoded_msg))

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

    async def change_color(self, r: float, g: float, b: float, a: float = 1):
        ''' Color must be normalized between [0, 1]'''
        color = { 'r': r, 'g': g, 'b': b, 'a': a }
        color_string = json.dumps(color)
        command = { 'commandName': 'color', 'data': [ color_string ] }
        self.send_command_to_server(command)


class StateInit(AgentState):
    async def run(self):
        self.agent.starter_position = "Spawner 1"
        self.agent.name = "agente1"
        self.agent.prefab_name = "Tractor"
        self.agent.position = await self.create_agent(self.agent)
        await self.change_color(0.5, 0.5, 0.5, 0.8)
        time.sleep(4)
        self.set_next_state(STATE_PERCEPTION)

class StatePerception(AgentState):
    async def run(self):
        await self.change_color(1, 0, 0, 0.8)
        time.sleep(4)
        self.set_next_state(STATE_COGNITION)

class StateCognition(AgentState):
    async def run(self):
        await self.change_color(0, 1, 0, 0.8)
        time.sleep(4)
        self.set_next_state(STATE_ACTION)

class StateAction(AgentState):
    async def run(self):
        await self.change_color(0, 0, 1, 0.8)
        time.sleep(4)
        self.set_next_state(STATE_PERCEPTION)

# --------------------------------------------- #
# AGENT                                         #
# --------------------------------------------- #
class AnimalAgent(Agent):
    async def setup(self):
        self.socket_info = ('127.0.0.1', 6066)
        behaviour = AgentBehaviour()

        # STATES
        behaviour.add_state(name=STATE_INIT, state=StateInit(), initial=True)
        behaviour.add_state(name=STATE_PERCEPTION, state=StatePerception())
        behaviour.add_state(name=STATE_COGNITION, state=StateCognition())
        behaviour.add_state(name=STATE_ACTION, state=StateAction())

        # TRANSITIONS
        behaviour.add_transition(source=STATE_INIT, dest=STATE_PERCEPTION)
        behaviour.add_transition(source=STATE_PERCEPTION, dest=STATE_COGNITION)
        behaviour.add_transition(source=STATE_COGNITION, dest=STATE_ACTION)
        behaviour.add_transition(source=STATE_ACTION, dest=STATE_PERCEPTION)

        self.add_behaviour(behaviour)

# --------------------------------------------- #
# MAIN                                          #
# --------------------------------------------- #
if __name__ == "__main__":
    animal = AnimalAgent("agente1@localhost", "xmppserver")
    future = animal.start()
    future.result()
    while animal.is_alive():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    animal.stop()
