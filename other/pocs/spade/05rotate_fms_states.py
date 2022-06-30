import time
import asyncio
import socket
import sys

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
    def send_msg_to_server(self, msg:str) -> str:
        """
        Send a message and waits for a response
        """
        encoded_msg = (msg + "\n").encode()
        self.sock.sendall(bytearray(encoded_msg))
        rcv = 0
        return self.sock.recv(16)

    async def on_start(self):
        print(f"Agente iniciado")
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect(self.agent.socket_info)
        print(f"Agente se ha conectado al servidor {self.agent.socket_info}.")

    async def run(self):
        msg = "Hola, esto es una prueba"
        data = self.send_msg_to_server(msg)
        print(f"{data}")

    async def on_end(self):
        print(f"Agente se ha ido a dormir")
        await self.agent.stop()

class StateInit(State):
    async def run(self):
        behaviour = self.agent.behaviours[0]
        print(f"Agente en estado {STATE_INIT}")
        print(f"Mandando mensaje de instanciacion en el simulador")
        behaviour.send_msg_to_server(STATE_INIT)
        self.set_next_state(STATE_PERCEPTION)

class StatePerception(State):
    async def run(self):
        behaviour = self.agent.behaviours[0]
        # msg = await self.receive(timeout=5)
        print(f"Agente en estado {STATE_PERCEPTION}")
        behaviour.send_msg_to_server(STATE_PERCEPTION)
        self.set_next_state(STATE_COGNITION)

class StateCognition(State):
    async def run(self):
        behaviour = self.agent.behaviours[0]
        print(f"Agente en estado {STATE_COGNITION}")
        behaviour.send_msg_to_server(STATE_COGNITION)
        self.set_next_state(STATE_ACTION)

class StateAction(State):
    async def run(self):
        behaviour = self.agent.behaviours[0]
        print(f"Agente en estado {STATE_ACTION}")
        print(f"Mandando mensaje de accion en el simulador")
        behaviour.send_msg_to_server(STATE_ACTION)
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
