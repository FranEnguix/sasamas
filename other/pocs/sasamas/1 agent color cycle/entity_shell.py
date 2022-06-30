import time
import os

from commander import Commander, Axis, ImageMode
from spade.agent import Agent

from image_data import ImageData

class EntityShell:

    async def init(agent: Agent, commander: Commander):
        await commander.change_color(0.5, 0.5, 0.5, 0.8)
        time.sleep(4)

    async def perception(agent: Agent, commander: Commander, data: ImageData):
        await commander.change_color(1, 0, 0, 0.8)
        time.sleep(4)

    async def cognition(agent: Agent, commander: Commander):
        await commander.change_color(0, 1, 0, 0.8)
        time.sleep(4)

    async def action(agent: Agent, commander: Commander):
        await commander.change_color(0, 0, 1, 0.8)
        time.sleep(4)
