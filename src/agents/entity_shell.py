import time
import os

from commander import Commander, Axis, ImageMode
from spade.agent import Agent

from image_data import ImageData
from orange_network import OrangeNetwork

class EntityShell:

    async def init(agent: Agent, commander: Commander):
        pass

    async def perception(agent: Agent, commander: Commander, data: ImageData):
        pass

    async def cognition(agent: Agent, commander: Commander):
        pass

    async def action(agent: Agent, commander: Commander):
        pass
