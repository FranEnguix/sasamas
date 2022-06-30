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
        print(f"Agente iniciado")
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect(self.agent.socket_info)
        print(f"Agente se ha conectado al servidor {self.agent.socket_info}.")

    async def run(self):
        #msg = "Hola, esto es una prueba"
        #data = self.send_msg_to_server(msg)
        #print(f"{data}")
        pass

    async def on_end(self):
        print(f"Agente se ha ido a dormir")
        await self.agent.stop()

class AgentState(State):
    async def on_start(self):
        behaviour = self.agent.behaviours[0]
        self.sock = behaviour.sock

    def send_msg_to_server(self, msg:str):
        """
        Send a message and waits for a response
        """
        encoded_msg = (msg + "\n").encode()
        self.sock.sendall(bytearray(encoded_msg))

    async def send_msg_to_server_and_wait(self, msg:str) -> str:
        """
        Send a message and waits for a response
        """
        encoded_msg = (msg + "\n").encode()
        self.sock.sendall(bytearray(encoded_msg))
        rcv = 0
        return self.sock.recv(32)

    async def send_command_to_server_and_wait(self, msg:dict) -> str:
        """
        Send a message and waits for a response
        """
        encoded_msg = json.dumps(msg).encode()
        self.sock.sendall(bytearray(encoded_msg))
        rcv = 0
        return self.sock.recv(32)

class StateInit(AgentState):
    async def run(self):
        print(f"Agente en estado {STATE_INIT}")
        print(f"Mandando mensaje de instanciacion en el simulador")
        # TODO self.agent.name para coger el nombre
        command = { 'commandName': 'create', 'gameObjects': [self.agent.name], 'data': None }
        msg = (await self.send_command_to_server_and_wait(command)).decode('utf-8')
        print(f"{msg}")
        self.agent.position = [float(x) for x in (msg.split())[1:]]
        # self.send_msg_to_server_and_wait(STATE_INIT)
        # self.agent.position = [0, 0, 0]
        self.set_next_state(STATE_PERCEPTION)

class StatePerception(AgentState):
    async def run(self):
        print(f"Agente en estado {STATE_PERCEPTION}")
        # self.send_msg_to_server(STATE_PERCEPTION)
        self.set_next_state(STATE_COGNITION)

class StateCognition(AgentState):
    async def run(self):
        print(f"Agente en estado {STATE_COGNITION}")
        # Decide la direccion que va a tomar
        # self.send_msg_to_server(STATE_COGNITION)
        time.sleep(1)
        self.set_next_state(STATE_ACTION)

class StateAction(AgentState):
    async def run(self):
        print(f"Agente en estado {STATE_ACTION}")
        print(f"Mandando mensaje de accion en el simulador")
        position = self.agent.position.copy()
        position[0] = position[0] + 0.2;
        new_position = f"({position[0]} {position[1]} {position[2]})"
        command = { 'commandName': 'moveTo', 'gameObjects': [self.agent.name], 'data': [new_position] }
        msg = (await self.send_command_to_server_and_wait(command)).decode('utf-8')
        print(f"{msg}")
        self.agent.position = [float(x) for x in (msg.split())[1:]]
        self.set_next_state(STATE_PERCEPTION)

# --------------------------------------------- #
# AGENT                                         #
# --------------------------------------------- #
class AnimalAgent(Agent):
    async def setup(self):
        self.socket_info = ('127.0.0.1', 60444)
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
        print(f"Agente se acaba de desperpertar.")

# --------------------------------------------- #
# MAIN                                          #
# --------------------------------------------- #
if __name__ == "__main__":
    animal = AnimalAgent("fran@localhost", "xmppserver")
    future = animal.start()
    future.result()
    while animal.is_alive():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    animal.stop()
